using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    // Which actor's actions/events are eligible to activate a granted effect.
    // SourceMatch additionally restricts events to the exact granting item/action.
    public enum EffectEventActorFilter
    {
        OwnerOnly = 0,
        OwnerOrAllies = 1,
        HostilesOnly = 2,
        Anyone = 3

    }

    [Serializable]
    public sealed class EffectTriggerRules
    {
        [SerializeField] private EffectTrigger trigger = EffectTrigger.OnActionUsed;
        [SerializeField] private EffectResolutionPhase phase = EffectResolutionPhase.Primary;
        [SerializeField] private EffectProcFrequency frequency = EffectProcFrequency.OncePerResolution;
        [SerializeField] private EffectEventActorFilter eventActor = EffectEventActorFilter.OwnerOnly;

        [Tooltip("For weapon/item-specific reactions: require the event to identify this exact granting source. The Step 5 collection binder cannot attribute damage to a specific equipped item yet. Matching grants remain inactive until source identity is wired.")]
        [SerializeField] private bool requireGrantingSourceMatch;
        [Tooltip("Requires a real combat action; excludes damage-over-time and environmental damage events.")]
        [SerializeField] private bool requireAction;

        public EffectTrigger Trigger => trigger;
        public EffectResolutionPhase Phase => phase;
        public EffectProcFrequency Frequency => frequency;
        public EffectEventActorFilter EventActor => eventActor;
        public bool RequireGrantingSourceMatch => requireGrantingSourceMatch;
        public bool RequireAction => requireAction;

    }

    [Serializable]
    public sealed class EffectTargetRules
    {
        [SerializeField] private EffectTargetReference reference = EffectTargetReference.Owner;
        [SerializeField] private EffectTargetSelection selection = EffectTargetSelection.Single;
        [SerializeField] private EffectFactionMask factionMask = EffectFactionMask.Any;
        [SerializeField] private bool includeOwner = true;
        [Min(0)]
        [SerializeField] private int radiusInTiles;
        [SerializeField] private bool sameFloorOnly = true;

        public EffectTargetReference Reference => reference;
        public EffectTargetSelection Selection => selection;
        public EffectFactionMask FactionMask => factionMask;
        public bool IncludeOwner => includeOwner;
        public int RadiusInTiles => radiusInTiles;
        public bool SameFloorOnly => sameFloorOnly;

    }

    [Serializable]
    public sealed class EffectLifetimeRules
    {
        [SerializeField] private EffectLifetime lifetime = EffectLifetime.Instant;
        [Min(1)]
        [SerializeField] private int durationTurns = 1;
        [Min(1)]
        [SerializeField] private int maximumUses = 1;

        public EffectLifetime Lifetime => lifetime;
        public int DurationTurns => durationTurns;
        public int MaximumUses => maximumUses;

    }

    // A value of 20 in Percent mode means +20% to the final stat.
    public enum StatModifierMode
    {
        Flat = 0,
        Percent = 1

    }

    // Parameters for the operation selected by EffectDefinition.
    // Only parameters relevant to that operation will be read by the processor.
    [Serializable]
    public sealed class EffectOperationParameters
    {
        [SerializeField] private ResourceIdentifier resource = ResourceIdentifier.None;
        [SerializeField] private StatIdentifier stat = StatIdentifier.None;
        [SerializeField] private StatModifierMode statModifierMode = StatModifierMode.Flat;
        [SerializeField] private DamageType damageType = DamageType.Unspecified;
        [SerializeField] private DamageDeliveryType damageDelivery = DamageDeliveryType.Unspecified;
        [SerializeField] private StatIdentifier defenseStat = StatIdentifier.None;

        [Tooltip("Optional status ID for removing a named status. Leave blank to cleanse debuffs.")]
        [SerializeField] private string statusId;
        [SerializeField] private StatusDefinition statusDefinition;

        public ResourceIdentifier Resource => resource;
        public StatIdentifier Stat => stat;
        public StatModifierMode ModifierMode => statModifierMode;
        public DamageType DamageKind => damageType;
        public DamageDeliveryType DamageDelivery => damageDelivery;
        public StatIdentifier DefenseStat => defenseStat;
        public string StatusId => statusId;
        public StatusDefinition StatusDefinition => statusDefinition;

    }

    // Optional comparisons evaluated against the same resolution snapshot.
    public enum EffectComparison
    {
        Equal = 0,
        NotEqual = 1,
        Greater = 2,
        GreaterOrEqual = 3,
        Less = 4,
        LessOrEqual = 5

    }

    [Serializable]
    public sealed class EffectCondition
    {
        [SerializeField] private EffectValueFormula left = new EffectValueFormula();
        [SerializeField] private EffectComparison comparison = EffectComparison.Greater;
        [SerializeField] private EffectValueFormula right = new EffectValueFormula();

        public EffectValueFormula Left => left;
        public EffectComparison Comparison => comparison;
        public EffectValueFormula Right => right;

    }

    // Serializable *configuration*, not active state. Every grant owns its own
    // values, so a sword, mutation stage, and consumable can reuse one definition.
    [Serializable]
    public sealed class EffectGrant : ISerializationCallbackReceiver
    {
        [SerializeField] private EffectDefinition definition;

        [Header("Application Chance")]
        [Range(0f, 100f)]
        [Tooltip("Chance this effect applies each time its trigger, targeting and conditions qualify. 100 = guaranteed; 0 = never. Failed rolls do not consume limited uses.")]
        [SerializeField] private float procChancePercent = 100f;

        // Old grants were guaranteed. Upgrade missing serialized fields safely
        // without turning previously authored effects into 0%-chance effects.
        [SerializeField, HideInInspector] private int procChanceSchemaVersion = 1;
        [SerializeField] private EffectTriggerRules activation = new EffectTriggerRules();
        [SerializeField] private EffectTargetRules targeting = new EffectTargetRules();
        [SerializeField] private EffectLifetimeRules lifetime = new EffectLifetimeRules();
        [SerializeField] private EffectOperationParameters operationParameters = new EffectOperationParameters();
        [SerializeField] private EffectValueFormula value = new EffectValueFormula();

        [Tooltip("All conditions must pass. Leave empty to apply without extra numeric conditions.")]
        [SerializeField] private List<EffectCondition> conditions = new List<EffectCondition>();

        public EffectDefinition Definition => definition;
        public float ProcChancePercent => procChanceSchemaVersion < 1
            ? 100f : Mathf.Clamp(procChancePercent, 0f, 100f);

        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {
            if (procChanceSchemaVersion < 1)
            {
                procChancePercent = 100f;
                procChanceSchemaVersion = 1;

            }

        }
        public EffectTriggerRules Activation => activation;
        public EffectTargetRules Targeting => targeting;
        public EffectLifetimeRules Lifetime => lifetime;
        public EffectOperationParameters OperationParameters => operationParameters;
        public EffectValueFormula Value => value;
        public IReadOnlyList<EffectCondition> Conditions => conditions;

    }
}
