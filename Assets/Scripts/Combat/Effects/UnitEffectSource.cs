using System.Collections.Generic;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    // Resolves runtime effect grants from mutations and equipped items.
    // Inspector collections remain available for existing effect tests.
    [RequireComponent(typeof(UnitController))]
    public sealed class UnitEffectSource : MonoBehaviour
    {
        [Header("Additional Effect Collections (Testing)")]
        [SerializeField] private List<EffectCollection> collections = new List<EffectCollection>();
        [SerializeField] private bool logEffectResults = true;

        [Header("Temporary Effect Test (Play Mode)")]
        [SerializeField] private EffectGrant temporaryTestGrant = new EffectGrant();

        private readonly List<EffectRuntimeInstance> runtime = new List<EffectRuntimeInstance>();
        private readonly List<EffectRuntimeInstance> temporary = new List<EffectRuntimeInstance>();
        private readonly HashSet<MutationID> boundMutationEffects = new HashSet<MutationID>();
        private UnitController owner;
        private TurnManager turns;
        private FactionManager factions;
        private bool initialized;
        private int lastStartRound = -1;
        private static long nextResolutionId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeIds()
        {
            nextResolutionId = 0;

        }

        private void Awake()
        {
            owner = GetComponent<UnitController>();
            if (GetComponent<UnitStatusController>() == null)
            {
                gameObject.AddComponent<UnitStatusController>();

            }

            factions = FindAnyObjectByType<FactionManager>();
            RefreshEffects();

        }

        private void OnEnable()
        {
            CombatActionExecutor.DamageApplied += OnDamageApplied;
            TryBindTurns();

        }

        private void Start()
        {
            TryBindTurns();
            CatchUpFirstTurn();

        }

        private void Update()
        {
            if (turns == null)
            {
                TryBindTurns();

            }

            // UnitController.Start and TurnManager.Start have no guaranteed
            // order. Recover the first turn event if unit data wasn't ready.
            CatchUpFirstTurn();

        }

        private void OnDisable()
        {
            CombatActionExecutor.DamageApplied -= OnDamageApplied;
            UnbindTurns();

        }

        // Rebuild after changing a unit's equipped sources; doing this resets
        // runtime durations, so do not call it every frame.
        [ContextMenu("Rebuild Runtime Effect Grants")]
        public void RefreshEffects()
        {
            // Temporary grants survive refreshes; permanent grants are rebound.
            if (owner != null && owner.data != null)
            {
                foreach (EffectRuntimeInstance previous in runtime)
                {
                    if (!temporary.Contains(previous))
                    {
                        owner.data.RemoveStatModifier(previous);

                    }

                }

            }

            runtime.Clear();
            boundMutationEffects.Clear();

            HashSet<EffectCollection> boundCollections = new HashSet<EffectCollection>();
            if (collections != null)
            {
                foreach (EffectCollection collection in collections)
                {
                    AddCollection(collection, boundCollections);

                }

            }

            if (owner == null)
            {
                owner = GetComponent<UnitController>();

            }

            if (owner != null && owner.data != null && owner.data.activeMutations != null)
            {
                GameDataBase database = GameDataBaseLoader.Instance;
                MutationDataBase mutationDatabase = database != null ? database.mutationData : null;
                if (mutationDatabase != null)
                {
                    foreach (BaseMutation mutation in owner.data.activeMutations.Values)
                    {
                        if (mutation == null)
                        {
                            continue;

                        }

                        EffectCollection collection = mutationDatabase.GetGrantedEffectCollection(mutation.ID);
                        if (AddCollection(collection, boundCollections))
                        {
                            boundMutationEffects.Add(mutation.ID);

                        }

                    }

                }

            }

            // Each fixed mutation node owns its effect grants. Two different families
            // may reference the same definition without losing their independent effects.
            if (owner != null && owner.data != null && owner.data.activeMutationNodes != null)
            {
                GameDataBase gameData = GameDataBaseLoader.Instance;
                MutationDataBase database = gameData != null ? gameData.mutationData : null;
                if (database != null)
                {
                    foreach (ActiveMutationRecord active in owner.data.activeMutationNodes)
                    {
                        MutationNodeDefinition node = database.GetNode(active.MutationId);
                        if (node == null)
                        {
                            continue;

                        }

                        bool added = false;
                        if (node.EffectCollection != null && node.EffectCollection.Effects != null)
                        {
                            foreach (EffectGrant grant in node.EffectCollection.Effects)
                            {
                                if (grant == null || grant.Definition == null)
                                {
                                    continue;

                                }

                                runtime.Add(new EffectRuntimeInstance(node.EffectCollection, grant));
                                added = true;

                            }

                        }

                        foreach (EffectGrant grant in node.GrantedEffects)
                        {
                            if (grant == null || grant.Definition == null)
                            {
                                continue;

                            }

                            runtime.Add(new EffectRuntimeInstance(null, grant));
                            added = true;

                        }

                        if (added)
                        {
                            boundMutationEffects.Add(node.Id);

                        }

                    }

                }

            }

            // One runtime grant per equipped item, even when multiple items
            // reference the same reusable EffectCollection.
            if (owner != null && owner.data != null && owner.data.equippedItems != null)
            {
                for (int i = 0; i < owner.data.equippedItems.Count; i++)
                {
                    EquipmentInstance item = owner.data.equippedItems[i];
                    AddEquipmentCollection(item.grantedEffectCollection, i);
                    if (item.grantedEffects != null)
                    {
                        foreach (EffectGrant grant in item.grantedEffects)
                        {
                            if (grant != null && grant.Definition != null)
                            {
                                runtime.Add(new EffectRuntimeInstance(null, grant, i));

                            }

                        }

                    }

                }

            }

            // Rebuilding equipment and mutation grants must not restart or
            // erase temporary effects that were already applied to this unit.
            for (int i = temporary.Count - 1; i >= 0; i--)
            {
                EffectRuntimeInstance instance = temporary[i];
                if (instance.Expired)
                {
                    temporary.RemoveAt(i);

                    continue;

                }

                runtime.Add(instance);

            }

            initialized = true;

            // Passive mutation/equipment stat grants activate on binding.
            if (owner != null && owner.data != null)
            {
                List<EffectRuntimeInstance> sources = new List<EffectRuntimeInstance>();
                foreach (EffectRuntimeInstance instance in runtime)
                {
                    if (!temporary.Contains(instance))
                    {
                        sources.Add(instance);

                    }

                }

                ProcessSourceActivated(sources);

            }

        }

        // Temporary effects are runtime grants, not mutations or equipment.
        // Each application gets independent turn counters and proc tracking.
        public bool ApplyTemporaryEffect(EffectGrant grant)
        {
            if (!Application.isPlaying || grant == null || grant.Definition == null ||
                grant.Activation == null || grant.Targeting == null || grant.Lifetime == null ||
                grant.Lifetime.Lifetime != EffectLifetime.TurnCount ||
                grant.Lifetime.DurationTurns < 1 || grant.Activation.RequireGrantingSourceMatch)
            {
                return false;

            }

            if (owner == null)
            {
                owner = GetComponent<UnitController>();

            }

            if (owner == null || owner.data == null)
            {
                return false;

            }

            if (!initialized)
            {
                RefreshEffects();

            }

            EffectRuntimeInstance instance = new EffectRuntimeInstance(null, grant);
            temporary.Add(instance);
            runtime.Add(instance);
            ProcessSourceActivated(new List<EffectRuntimeInstance> { instance });

            return true;

        }

        [ContextMenu("Test / Apply Temporary Effect")]
        private void TestApplyTemporaryEffect()
        {
            if (!ApplyTemporaryEffect(temporaryTestGrant))
            {
                Debug.LogWarning("Temporary test grant requires Play Mode, a ready unit, " +
                    "a valid effect definition, Turn Count lifetime, at least 1 turn, " +
                    "and Granting Source Match disabled.", this);

                return;

            }

            Debug.Log($"Applied temporary effect '{temporaryTestGrant.Definition.DisplayName}' " +
                $"for {temporaryTestGrant.Lifetime.DurationTurns} owner turn ends.", this);

        }

        [ContextMenu("Test / Log Temporary Effects")]
        private void TestLogTemporaryEffects()
        {
            Debug.Log($"Active temporary effects: {temporary.Count}", this);
            foreach (EffectRuntimeInstance instance in temporary)
            {
                Debug.Log($"Temporary '{instance.Grant.Definition.DisplayName}': " +
                    $"{instance.RemainingTurns} owner turn ends remaining, expired={instance.Expired}.", this);

            }

        }

        private void AddEquipmentCollection(EffectCollection collection, int equipmentIndex)
        {
            if (collection == null || collection.Effects == null)
            {
                return;

            }

            foreach (EffectGrant grant in collection.Effects)
            {
                if (grant == null || grant.Definition == null)
                {
                    continue;

                }

                runtime.Add(new EffectRuntimeInstance(collection, grant, equipmentIndex));

            }

        }

        public bool HasMutationEffects(MutationID mutationId)
        {
            return initialized && boundMutationEffects.Contains(mutationId);

        }

        private bool AddCollection(EffectCollection collection, HashSet<EffectCollection> boundCollections)
        {
            if (collection == null || collection.Effects == null)
            {
                return false;

            }

            bool hasGrant = false;
            bool shouldAdd = boundCollections.Add(collection);
            foreach (EffectGrant grant in collection.Effects)
            {
                if (grant == null || grant.Definition == null)
                {
                    continue;

                }

                hasGrant = true;
                if (shouldAdd)
                {
                    runtime.Add(new EffectRuntimeInstance(collection, grant));

                }

            }

            return hasGrant;

        }

        private void TryBindTurns()
        {
            TurnManager available = TurnManager.Instance;
            if (turns == available)
            {
                return;

            }

            UnbindTurns();
            if (available != null)
            {
                turns = available;
                turns.TurnStarted += OnTurnStarted;
                turns.TurnEnded += OnTurnEnded;

            }

        }

        private void UnbindTurns()
        {
            if (turns != null)
            {
                turns.TurnStarted -= OnTurnStarted;
                turns.TurnEnded -= OnTurnEnded;
                turns = null;

            }

        }

        private void OnDamageApplied(CombatDamageEvent report)
        {
            if (!initialized)
            {
                RefreshEffects();

            }

            if (owner == null || owner.data == null || report.ActualDamage <= 0)
            {
                return;

            }

            long resolutionId = ++nextResolutionId;
            ProcessEvent(new EffectEventContext(EffectTrigger.OnDamageDealt, resolutionId,
                report.Attacker, report.Attacker, report.Target, report.Target,
                report.Action, report.ActualDamage, true, 1, report.SourceEquipmentIndex));
            ProcessEvent(new EffectEventContext(EffectTrigger.OnDamageReceived, resolutionId,
                report.Target, report.Attacker, report.Target, report.Target,
                report.Action, report.ActualDamage, true, 1, report.SourceEquipmentIndex));

            // Action-specific grants only fire for this action's attacker on
            // the original hit, never on a recursive damage-over-time tick.
            if (report.Attacker == owner && report.Action != null &&
                !CombatActionExecutor.IsPublishingEffectDamage)
            {
                ProcessActionDamageEffects(report, resolutionId);

            }

        }

        private void ProcessActionDamageEffects(CombatDamageEvent report, long resolutionId)
        {
            EffectCollection collection = report.Action.OnDamageEffects;
            if (collection == null || collection.Effects == null)
            {
                return;

            }

            List<EffectRuntimeInstance> grants = new List<EffectRuntimeInstance>();
            foreach (EffectGrant grant in collection.Effects)
            {
                if (grant != null && grant.Definition != null)
                {
                    grants.Add(new EffectRuntimeInstance(collection, grant));

                }

            }

            if (grants.Count == 0)
            {
                return;

            }

            if (factions == null)
            {
                factions = FindAnyObjectByType<FactionManager>();

            }

            EffectEventContext context = new EffectEventContext(
                EffectTrigger.OnDamageDealt, resolutionId,
                owner, owner, report.Target, report.Target,
                report.Action, report.ActualDamage, true, 1, report.SourceEquipmentIndex);

            int applied = EffectProcessor.Process(owner, grants, context, factions, logEffectResults);
            if (logEffectResults)
            {
                Debug.Log($"[Action Effects] {report.Action.DisplayName}: " +
                    $"{applied}/{grants.Count} effect grant(s) applied to " +
                    $"{report.Target.data.characterName} / {owner.data.characterName}.", owner);

            }

        }

        private void OnTurnStarted(FactionData faction)
        {
            if (!IsOwnFaction(faction) || turns == null || lastStartRound == turns.Round)
            {
                return;

            }

            lastStartRound = turns.Round;
            ProcessTurn(EffectTrigger.OnTurnStart);

        }

        private void OnTurnEnded(FactionData faction)
        {
            if (!IsOwnFaction(faction))
            {
                return;

            }

            ProcessTurn(EffectTrigger.OnTurnEnd);
            foreach (EffectRuntimeInstance instance in runtime)
            {
                instance.OnOwnerTurnEnded();

            }

            foreach (EffectRuntimeInstance instance in runtime)
            {
                if (instance.Expired && owner != null && owner.data != null)
                {
                    owner.data.RemoveStatModifier(instance);

                }

            }

            for (int i = temporary.Count - 1; i >= 0; i--)
            {
                EffectRuntimeInstance instance = temporary[i];
                if (!instance.Expired)
                {
                    continue;

                }

                Debug.Log($"Temporary effect '{instance.Grant.Definition.DisplayName}' expired.", this);
                runtime.Remove(instance);
                temporary.RemoveAt(i);

            }

        }

        private void CatchUpFirstTurn()
        {
            if (turns != null && turns.Round > 0 && IsOwnFaction(turns.ActiveFaction))
            {
                OnTurnStarted(turns.ActiveFaction);

            }

        }

        private bool IsOwnFaction(FactionData faction)
        {
            if (owner == null || owner.data == null || faction == null)
            {
                return false;

            }

            UnitFaction member = owner.GetComponent<UnitFaction>();

            return member != null && member.Faction == faction;

        }

        private void ProcessTurn(EffectTrigger trigger)
        {
            long resolutionId = ++nextResolutionId;
            ProcessEvent(new EffectEventContext(trigger, resolutionId,
                owner, owner, owner, owner, null, 0, false, 0));

        }

        private void ProcessSourceActivated(IReadOnlyList<EffectRuntimeInstance> sources)
        {
            if (owner == null || owner.data == null || sources.Count == 0)
            {
                return;

            }

            if (factions == null)
            {
                factions = FindAnyObjectByType<FactionManager>();

            }

            long resolutionId = ++nextResolutionId;
            EffectProcessor.Process(owner, sources,
                new EffectEventContext(EffectTrigger.OnSourceActivated, resolutionId,
                    owner, owner, owner, owner, null, 0, false, 0), factions, logEffectResults);

        }

        private void ProcessEvent(EffectEventContext context)
        {
            if (factions == null)
            {
                factions = FindAnyObjectByType<FactionManager>();

            }

            EffectProcessor.Process(owner, runtime, context, factions, logEffectResults);

        }

    }
}