using System.Collections.Generic;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    // Opt-in bridge: add this to a UnitController GameObject and assign
    // Effect Collections to exercise the processor. Source integration with
    // equipment, mutations, and actions happens after the proof of concept.
    [RequireComponent(typeof(UnitController))]
    public sealed class UnitEffectSource : MonoBehaviour
    {
        [Header("Temporary Step 5 Effect Sources")]
        [SerializeField] private List<EffectCollection> collections = new List<EffectCollection>();
        [SerializeField] private bool logEffectResults = true;

        private readonly List<EffectRuntimeInstance> runtime = new List<EffectRuntimeInstance>();
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

        // Can be called after a future inventory/mutation loadout update.
        // It deliberately restarts runtime durations: don't call every frame.
        [ContextMenu("Rebuild Runtime Effect Grants")]
        public void RefreshEffects()
        {
            runtime.Clear();
            if (collections != null)
            {
                foreach (EffectCollection collection in collections)
                {
                    if (collection == null || collection.Effects == null)
                    {
                        continue;

                    }

                    foreach (EffectGrant grant in collection.Effects)
                    {
                        if (grant != null)
                        {
                            runtime.Add(new EffectRuntimeInstance(collection, grant));

                        }

                    }

                }

            }

            initialized = true;

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
                report.Action, report.ActualDamage, true, 1));
            ProcessEvent(new EffectEventContext(EffectTrigger.OnDamageReceived, resolutionId,
                report.Target, report.Attacker, report.Target, report.Target,
                report.Action, report.ActualDamage, true, 1));

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