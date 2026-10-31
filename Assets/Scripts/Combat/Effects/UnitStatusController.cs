using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    // One component per unit. No runtime state is stored on shared SO assets.
    // The owner faction's turn is the clock, regardless of who applied a status.
    [RequireComponent(typeof(UnitController))]
    public sealed class UnitStatusController : MonoBehaviour
    {
        [Header("Status Test (Play Mode)")]
        [SerializeField] private StatusDefinition testStatus;
        [SerializeField] private bool logStatusEvents = true;

        private sealed class ActiveStatus
        {
            public readonly StatusDefinition Definition;
            public readonly UnitController Source;
            public int Stacks;
            public int RemainingTurns;
            public bool SkipNextOwnerTurnEnd;
            public int AppliedOwnerRound;

            public ActiveStatus(StatusDefinition definition, UnitController source, bool skipTurnEnd)
            {
                Definition = definition;
                Source = source;
                Stacks = 1;
                RemainingTurns = definition.DurationOwnerTurns;
                SkipNextOwnerTurnEnd = skipTurnEnd;
                AppliedOwnerRound = -1;

            }

        }

        private readonly List<ActiveStatus> active = new List<ActiveStatus>();
        private UnitController owner;
        private TurnManager turns;
        private int lastStartRound = -1;
        private int lastEndRound = -1;
        private int statusRevision;

        public int ActiveStatusCount => active.Count;
        public int StatusRevision => statusRevision;

        public string GetStatusSummary(int maxVisible = 5)
        {
            if (active.Count == 0)
            {
                return string.Empty;

            }

            int limit = Mathf.Max(1, maxVisible);
            StringBuilder summary = new StringBuilder();
            summary.Append(active.Count).Append(" STATUS");
            if (active.Count != 1)
            {
                summary.Append('S');

            }

            summary.Append("  |  ");
            int shown = 0;

            // Invisible stays visible in the compact readout even when the
            // unit also has many other temporary buffs/debuffs.
            foreach (ActiveStatus status in active)
            {
                if ((status.Definition.Controls & StatusControlFlags.Invisible) != 0)
                {
                    AppendStatusLabel(summary, status);
                    shown++;

                    break;

                }

            }

            foreach (ActiveStatus status in active)
            {
                if (shown >= limit)
                {
                    break;

                }

                if ((status.Definition.Controls & StatusControlFlags.Invisible) != 0)
                {
                    continue;

                }

                if (shown > 0)
                {
                    summary.Append("  ·  ");

                }

                AppendStatusLabel(summary, status);
                shown++;

            }

            if (active.Count > shown)
            {
                summary.Append("  +").Append(active.Count - shown).Append(" more");

            }

            return summary.ToString();

        }

        // Full overhead readout. Unlike the compact summary, every effect gets its own line.
        public string GetStatusLines()
        {
            if (active.Count == 0)
            {
                return string.Empty;

            }

            StringBuilder lines = new StringBuilder();

            // Show invisibility first so it remains easy to spot even on a busy unit.
            foreach (ActiveStatus status in active)
            {
                if ((status.Definition.Controls & StatusControlFlags.Invisible) == 0)
                {
                    continue;

                }

                AppendStatusLine(lines, status);

            }

            foreach (ActiveStatus status in active)
            {
                if ((status.Definition.Controls & StatusControlFlags.Invisible) != 0)
                {
                    continue;

                }

                AppendStatusLine(lines, status);

            }

            return lines.ToString();

        }

        private static void AppendStatusLine(StringBuilder lines, ActiveStatus status)
        {
            if (lines.Length > 0)
            {
                lines.Append('\n');

            }

            lines.Append(status.Definition.DisplayName).Append(" x").Append(status.Stacks);

        }


        private static void AppendStatusLabel(StringBuilder summary, ActiveStatus status)
        {
            summary.Append(status.Definition.DisplayName);
            if (status.Stacks > 1)
            {
                summary.Append(" x").Append(status.Stacks);

            }

        }


        private void Awake()
        {
            owner = GetComponent<UnitController>();

        }

        private void OnEnable()
        {
            TryBindTurns();

        }

        private void Update()
        {
            if (turns == null)
            {
                TryBindTurns();

            }

        }

        private void OnDisable()
        {
            UnbindTurns();
            ClearAll();

        }

        public bool TryApply(StatusDefinition definition, UnitController source, int stacks = 1)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id) ||
                owner == null || owner.data == null || owner.data.currentHealth <= 0 || stacks < 1)
            {
                return false;

            }

            if (turns == null)
            {
                TryBindTurns();

            }

            ActiveStatus existing = Find(definition.Id);
            if (existing != null)
            {
                if (definition.Stacking == StatusStackingPolicy.AddStacks)
                {
                    existing.Stacks = Mathf.Min(definition.MaximumStacks, existing.Stacks + stacks);

                }

                existing.RemainingTurns = definition.DurationOwnerTurns;
                // Refreshing an existing effect must not restart its first-turn grace.
                RefreshStatModifier(existing);
                statusRevision++;
                Log($"Status '{definition.DisplayName}' refreshed on {owner.data.characterName}: " +
                    $"{existing.Stacks} stack(s), {existing.RemainingTurns} owner turns remaining.");
                CombatFloatingFeedback.ReportStatusApplied(owner, definition.IsDebuff);

                return true;

            }

            bool appliedDuringOwnTurn = turns != null && IsOwnFaction(turns.ActiveFaction);
            ActiveStatus status = new ActiveStatus(definition, source, appliedDuringOwnTurn);
            status.Stacks = Mathf.Min(definition.MaximumStacks, stacks);
            if (appliedDuringOwnTurn && turns != null)
            {
                status.AppliedOwnerRound = turns.Round;

            }

            active.Add(status);
            RefreshStatModifier(status);
            statusRevision++;
            Log($"Status '{definition.DisplayName}' applied to {owner.data.characterName}: " +
                $"{status.Stacks} stack(s), {status.RemainingTurns} owner turns remaining.");
            CombatFloatingFeedback.ReportStatusApplied(owner, definition.IsDebuff);

            return true;

        }

        public bool HasStatus(string statusId)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                return false;

            }

            foreach (ActiveStatus status in active)
            {
                if (string.Equals(status.Definition.Id, statusId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;

                }

            }

            return false;

        }

        public bool HasControl(StatusControlFlags restriction)
        {
            foreach (ActiveStatus status in active)
            {
                if ((status.Definition.Controls & restriction) != 0)
                {
                    return true;

                }

            }

            return false;

        }

        public bool CanMoveVoluntarily => !HasControl(StatusControlFlags.Stun | StatusControlFlags.Root);

        public bool CanUseAction(ActionDefinition action)
        {
            if (action == null || HasControl(StatusControlFlags.Stun))
            {
                return false;

            }

            if (HasControl(StatusControlFlags.Silence) &&
                ((action.ControlTags & ActionControlTags.MagicOrMutation) != 0 ||
                 action.DamageStat == CombatDamageStat.Magic))
            {
                return false;

            }

            if (HasControl(StatusControlFlags.Disarm) &&
                (action.ControlTags & ActionControlTags.RequiresWeapon) != 0)
            {
                return false;

            }

            return true;

        }

        public float DodgeChancePercent
        {
            get
            {
                float chance = 0f;
                foreach (ActiveStatus status in active)
                {
                    chance += status.Definition.DodgeChancePercentPerStack * status.Stacks;

                }

                return Mathf.Clamp(chance, 0f, 95f);

            }

        }

        public float IncomingOrdinaryDamageMultiplier
        {
            get
            {
                float modifierPercent = 0f;
                foreach (ActiveStatus status in active)
                {
                    modifierPercent += status.Definition.IncomingDamagePercentPerStack * status.Stacks;

                }

                return Mathf.Clamp(1f + modifierPercent / 100f, 0f, 10f);

            }

        }

        // With no ID, remove debuffs only: the Cleanse operation.
        public int Remove(string statusId, bool debuffsOnly = false)
        {
            int removed = 0;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                ActiveStatus status = active[i];
                if ((string.IsNullOrWhiteSpace(statusId) ||
                        string.Equals(status.Definition.Id, statusId, StringComparison.OrdinalIgnoreCase)) &&
                    (!debuffsOnly || status.Definition.IsDebuff))
                {
                    RemoveAt(i, "removed");
                    removed++;

                }

            }

            return removed;

        }

        private ActiveStatus Find(string id)
        {
            foreach (ActiveStatus status in active)
            {
                if (string.Equals(status.Definition.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return status;

                }

            }

            return null;

        }

        private void RefreshStatModifier(ActiveStatus status)
        {
            if (owner == null || owner.data == null)
            {
                return;

            }

            List<UnitData.StatModifierEntry> modifiers = new List<UnitData.StatModifierEntry>();

            // Preserve the original single-stat field for previously created statuses.
            if (status.Definition.ModifiedStat != StatIdentifier.None &&
                status.Definition.ModifierPerStack != 0f)
            {
                modifiers.Add(new UnitData.StatModifierEntry(status.Definition.ModifiedStat,
                    status.Definition.ModifierMode, (double)status.Definition.ModifierPerStack * status.Stacks));

            }

            if (status.Definition.StatModifiers != null)
            {
                foreach (StatusStatModifier entry in status.Definition.StatModifiers)
                {
                    if (entry == null || entry.Stat == StatIdentifier.None || entry.AmountPerStack == 0f)
                    {
                        continue;

                    }

                    modifiers.Add(new UnitData.StatModifierEntry(entry.Stat, entry.Mode,
                        (double)entry.AmountPerStack * status.Stacks));

                }

            }

            owner.data.TryReplaceStatModifiers(status, modifiers);

        }

        private void OnTurnStarted(FactionData faction)
        {
            if (!IsOwnFaction(faction) || turns == null || lastStartRound == turns.Round)
            {
                return;

            }

            lastStartRound = turns.Round;
            Tick(StatusTickTiming.OwnerTurnStart);

        }

        private void OnTurnEnded(FactionData faction)
        {
            if (!IsOwnFaction(faction) || turns == null || lastEndRound == turns.Round)
            {
                return;

            }

            lastEndRound = turns.Round;
            Tick(StatusTickTiming.OwnerTurnEnd);
            for (int i = active.Count - 1; i >= 0; i--)
            {
                ActiveStatus status = active[i];
                if (status.SkipNextOwnerTurnEnd)
                {
                    status.SkipNextOwnerTurnEnd = false;

                    continue;

                }

                status.RemainingTurns--;
                if (status.RemainingTurns <= 0)
                {
                    RemoveAt(i, "expired");

                }

            }

        }

        private void Tick(StatusTickTiming timing)
        {
            // Snapshot prevents collection changes by damage, death, or listeners.
            ActiveStatus[] snapshot = active.ToArray();
            foreach (ActiveStatus status in snapshot)
            {
                if (!active.Contains(status) || owner == null || owner.data == null ||
                    owner.data.currentHealth <= 0 || status.Definition.TickTiming != timing ||
                    (timing == StatusTickTiming.OwnerTurnStart && turns != null &&
                    status.AppliedOwnerRound == turns.Round))
                {
                    continue;

                }

                long amountLong = (long)status.Definition.AmountPerStack * status.Stacks;
                int amount = (int)Math.Min(int.MaxValue, amountLong);
                if (amount <= 0)
                {
                    continue;

                }

                if (status.Definition.PeriodicOperation == StatusPeriodicOperation.Heal)
                {
                    if (ResourceOperations.TryApply(owner.data, ResourceIdentifier.Health, amount,
                        out ResourceChangeResult healing))
                    {
                        Log($"Status '{status.Definition.DisplayName}' healed {owner.data.characterName} " +
                            $"{healing.AppliedAmount} HP ({healing.Before} -> {healing.After}).");

                    }

                }
                else if (status.Definition.PeriodicOperation == StatusPeriodicOperation.Damage)
                {
                    DamageRequest request = new DamageRequest(status.Source, owner, amount,
                        1f, 0, 0, null, status.Definition.DamageType, DamageDeliveryType.Environmental);
                    if (DamageResolver.TryResolve(request, out DamageResult damage))
                    {
                        Log($"Status '{status.Definition.DisplayName}' dealt " +
                            $"{damage.ActualDamage} {status.Definition.DamageType} damage to " +
                            $"{owner.data.characterName} ({damage.HealthBefore} -> {damage.HealthAfter}).");
                        CombatActionExecutor.PublishResolvedEffectDamage(damage);

                    }

                }

            }

        }

        private void RemoveAt(int index, string reason)
        {
            ActiveStatus status = active[index];
            if (owner != null && owner.data != null)
            {
                owner.data.RemoveStatModifier(status);

            }

            active.RemoveAt(index);
            statusRevision++;
            Log($"Status '{status.Definition.DisplayName}' {reason} on " +
                $"{(owner != null && owner.data != null ? owner.data.characterName : gameObject.name)}.");

        }

        private void ClearAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                RemoveAt(i, "cleared");

            }

        }

        private void TryBindTurns()
        {
            TurnManager current = TurnManager.Instance;
            if (turns == current)
            {
                return;

            }

            UnbindTurns();
            if (current != null)
            {
                turns = current;
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

        private bool IsOwnFaction(FactionData faction)
        {
            if (owner == null || owner.data == null || faction == null)
            {
                return false;

            }

            UnitFaction membership = owner.GetComponent<UnitFaction>();

            return membership != null && membership.Faction == faction;

        }

        private void Log(string message)
        {
            if (logStatusEvents)
            {
                Debug.Log(message, this);

            }

        }

        [ContextMenu("Test / Apply Configured Status")]
        private void TestApplyStatus()
        {
            if (!Application.isPlaying || !TryApply(testStatus, owner))
            {
                Debug.LogWarning("Enter Play Mode, assign Test Status, and use an initialized living unit.", this);

            }

        }

        [ContextMenu("Test / Remove Configured Status")]
        private void TestRemoveStatus()
        {
            if (Application.isPlaying && testStatus != null)
            {
                Remove(testStatus.Id);

            }

        }

        [ContextMenu("Test / Cleanse Debuffs")]
        private void TestCleanse()
        {
            if (Application.isPlaying)
            {
                Remove(null, true);

            }

        }

        [ContextMenu("Test / Log Active Statuses")]
        private void TestLogStatuses()
        {
            Debug.Log($"Active statuses on {gameObject.name}: {active.Count}", this);
            foreach (ActiveStatus status in active)
            {
                Debug.Log($"{status.Definition.DisplayName}: stacks={status.Stacks}, " +
                    $"remaining={status.RemainingTurns}, debuff={status.Definition.IsDebuff}.", this);

            }

        }

    }
}
