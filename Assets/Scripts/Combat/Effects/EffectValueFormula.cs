using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    // Which number an individual term reads at resolution time.
    // These values are resolved by the future processor from the event and targets.
    public enum EffectValueSource
    {
        Constant = 0,
        EventActualDamage = 1,
        EventAttemptedDamage = 2,
        EventAppliedHealing = 3,
        EventAffectedTargetCount = 4,

        OwnerStat = 10,
        RecipientStat = 11,
        OwnerResourceCurrent = 12,
        OwnerResourceMaximum = 13,
        OwnerResourceMissing = 14,
        RecipientResourceCurrent = 15,
        RecipientResourceMaximum = 16,
        RecipientResourceMissing = 17

    }

    // One term contributes (resolved value * multiplier) to the result.
    // For Constant, the resolved value is literalValue.
    [Serializable]
    public sealed class EffectValueTerm
    {
        [SerializeField] private EffectValueSource source = EffectValueSource.Constant;
        [SerializeField] private float literalValue;
        [SerializeField] private StatIdentifier stat = StatIdentifier.Strength;
        [SerializeField] private ResourceIdentifier resource = ResourceIdentifier.Health;
        [SerializeField] private float multiplier = 1f;

        public EffectValueSource Source => source;
        public float LiteralValue => literalValue;
        public StatIdentifier Stat => stat;
        public ResourceIdentifier Resource => resource;
        public float Multiplier => multiplier;

    }

    // Data-only weighted combination: constant + sum(each term * its multiplier).
    // Supports flat amounts and mixtures of stats, damage, and resources.
    // More advanced arithmetic can be added to the calculator later without
    // changing healing/damage operations or individual source implementations.
    [Serializable]
    public sealed class EffectValueFormula
    {
        [SerializeField] private float constant;
        [SerializeField] private List<EffectValueTerm> terms = new List<EffectValueTerm>();
        [SerializeField] private CalculationRounding rounding = CalculationRounding.Ceiling;

        public float Constant => constant;
        public IReadOnlyList<EffectValueTerm> Terms => terms;
        public CalculationRounding Rounding => rounding;

    }
}
