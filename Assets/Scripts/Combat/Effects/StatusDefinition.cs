using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    public enum StatusStackingPolicy
    {
        RefreshDuration = 0,
        AddStacks = 1

    }

    public enum StatusTickTiming
    {
        None = 0,
        OwnerTurnStart = 1,
        OwnerTurnEnd = 2

    }

    public enum StatusPeriodicOperation
    {
        None = 0,
        Damage = 1,
        Heal = 2

    }

    [Serializable]
    public sealed class StatusStatModifier
    {
        [SerializeField] private StatIdentifier stat = StatIdentifier.None;
        [SerializeField] private StatModifierMode mode = StatModifierMode.Percent;
        [SerializeField] private float amountPerStack;

        public StatIdentifier Stat => stat;
        public StatModifierMode Mode => mode;
        public float AmountPerStack => amountPerStack;

    }

    // Shared configuration only. Active stacks, duration and stat changes
    // belong to each unit's UnitStatusController, never this SO.
    [CreateAssetMenu(fileName = "NewStatus", menuName = "MutaBorn/Effects/Status Definition")]
    public sealed class StatusDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string statusId = "new-status";
        [SerializeField] private string displayName = "New Status";
        [SerializeField] private bool isDebuff = true;

        [Header("Duration and Stacking")]
        [Min(1)] [SerializeField] private int durationOwnerTurns = 3;
        [Min(1)] [SerializeField] private int maximumStacks = 1;
        [SerializeField] private StatusStackingPolicy stacking = StatusStackingPolicy.RefreshDuration;

        [Header("Periodic Operation")]
        [SerializeField] private StatusTickTiming tickTiming = StatusTickTiming.OwnerTurnStart;
        [SerializeField] private StatusPeriodicOperation periodicOperation = StatusPeriodicOperation.None;
        [SerializeField] private DamageType damageType = DamageType.Fire;
        [Min(0)] [SerializeField] private int amountPerStack = 5;

        [Header("Optional Persistent Stat Modifier")]
        [SerializeField] private StatIdentifier modifiedStat = StatIdentifier.None;
        [SerializeField] private StatModifierMode modifierMode = StatModifierMode.Flat;
        [SerializeField] private float modifierPerStack;

        [Header("Additional Persistent Stat Modifiers")]
        [Tooltip("All entries scale with the current stack count and expire with this status. Percent -2 means -2% per stack.")]
        [SerializeField] private List<StatusStatModifier> statModifiers = new List<StatusStatModifier>();

        public string Id => statusId;
        public string DisplayName => displayName;
        public bool IsDebuff => isDebuff;
        public int DurationOwnerTurns => Mathf.Max(1, durationOwnerTurns);
        public int MaximumStacks => Mathf.Max(1, maximumStacks);
        public StatusStackingPolicy Stacking => stacking;
        public StatusTickTiming TickTiming => tickTiming;
        public StatusPeriodicOperation PeriodicOperation => periodicOperation;
        public DamageType DamageType => damageType;
        public int AmountPerStack => Mathf.Max(0, amountPerStack);
        public StatIdentifier ModifiedStat => modifiedStat;
        public StatModifierMode ModifierMode => modifierMode;
        public float ModifierPerStack => modifierPerStack;
        public IReadOnlyList<StatusStatModifier> StatModifiers => statModifiers;

    }
}
