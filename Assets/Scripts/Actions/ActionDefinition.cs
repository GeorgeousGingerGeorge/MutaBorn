using UnityEngine;
using MutaBorn.Gameplay;

// Main buttons in the current MutaBorn Action Menu.
public enum ActionMenuCategory
{
    Attacks,
    Defense,
    Abilities,
    Interact,
    Items

}

// Whether the player deliberately activates this, or it operates automatically.
// Overwatch is Active (the player prepares it); Vampirism is Passive.
public enum ActionActivationType
{
    Active,
    Passive

}

[System.Flags]
public enum ActionControlTags
{
    None = 0,
    MagicOrMutation = 1,
    RequiresWeapon = 2

}

// Combat behavior is separate from the menu category and Active/Passive classification.
public enum CombatActionBehavior
{
    None,
    DamageUnit,
    ApplyGuard,
    InspectUnit,
    DamageArea,
    ApplySelfStatus,
    HealArea,
    RestoreMana,
    MutationScan = 8,
    MutationDeployCover = 9,
    MutationDeployTrap = 10

}

public enum CombatDamageStat
{
    Melee,
    Ranged,
    Magic

}

[CreateAssetMenu(fileName = "NewAction", menuName = "Game/Actions/Action Definition")]
public class ActionDefinition : ScriptableObject
{
    [Header("Identity")]

    [SerializeField] private string actionId = "new-action";
    [SerializeField] private string displayName = "New Action";
    [TextArea(2, 5)][SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Behavior")]

    [SerializeField] private ActionActivationType activationType = ActionActivationType.Active;

    [Header("Menu Organization")]

    [SerializeField] private ActionMenuCategory category = ActionMenuCategory.Abilities;
    [Tooltip("Future collapsible group, such as Melee, Ranged, Mutations, or Equipment.")]
    [SerializeField] private string submenuGroup = "General";
    [Tooltip("Lower values appear first within a group. Use 10, 20, 30 to leave gaps.")]
    [SerializeField] private int displayOrder = 100;

    [Header("Active Action Cost")]

    [Tooltip("Only applies to active actions. Passive abilities do not spend AP to activate.")]
    [SerializeField, Min(0)] private float apCost = 5f;
    [Tooltip("Zero means unlimited uses per faction turn. Used by Arcane Replenish.")]
    [SerializeField, Min(0)] private int maxUsesPerTurn;

    [Header("Status Interaction")]
    [Tooltip("Silence blocks MagicOrMutation actions. Disarm blocks RequiresWeapon actions. Unarmed attacks should not have RequiresWeapon.")]
    [SerializeField] private ActionControlTags controlTags = ActionControlTags.None;

    [Header("Combat Rules")]

    [Tooltip("Select the gameplay behavior. None is for passive or not-yet-implemented actions.")]
    [SerializeField] private CombatActionBehavior combatBehavior = CombatActionBehavior.None;

    [Tooltip("Which calculated UnitData damage stat this attack uses. Includes relevant equipment and mutation bonuses.")]
    [SerializeField] private CombatDamageStat damageStat = CombatDamageStat.Melee;

    [SerializeField, Min(0.1f)] private float combatRange = 2.5f;
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;
    [SerializeField] private int flatDamageBonus = 0;
    [SerializeField, Min(0)] private int minimumDamage = 5;
    [Tooltip("Number of separate direct attacks. AP is paid once; each hit resolves independently.")]
    [SerializeField, Range(1, 5)] private int hitCount = 1;

    [Header("Area Explosion (DamageArea only)")]
    [Tooltip("How far the explosion can be thrown, measured in grid tiles.")]
    [SerializeField, Min(0)] private int areaThrowRangeInTiles = 6;
    [Tooltip("Blast radius in grid tiles. All units in the circle receive the same raw attack.")]
    [SerializeField, Min(0)] private int areaRadiusInTiles = 2;
    [Tooltip("First version: throw and explosion stay on the actor's floor.")]
    [SerializeField] private bool areaSameFloorOnly = true;
    [Tooltip("Explosions damage allies, civilians and the caster when they are inside the blast.")]
    [SerializeField] private bool areaFriendlyFire = true;

    [Header("Area Healing (HealArea only)")]
    [Tooltip("Health restored to each friendly unit in the blast circle. Cannot heal past maximum HP.")]
    [SerializeField, Min(0)] private int areaHealAmount = 15;

    [Header("Mana Recovery (RestoreMana only)")]
    [SerializeField, Min(0)] private int manaRestoreAmount;

    [Header("Self Status (ApplySelfStatus only)")]
    [SerializeField] private StatusDefinition selfStatus;

    [Header("Damage Types")]
    [Tooltip("Unspecified uses Slash for Melee, Pierce for Ranged, or Arcane for Magic. Existing actions do not need reconfiguration.")]
    [SerializeField] private DamageType damageType = DamageType.Unspecified;
    [Tooltip("Additional fixed-damage components, each using its own Armor/Ward and damage resistance. Optional.")]
    [SerializeField] private DamageComponent[] additionalDamage = new DamageComponent[0];

    [Header("Optional Crit and Penetration")]
    [Tooltip("0 by default. Applied only to this attack, before defenses.")]
    [SerializeField, Range(0f, 100f)] private float criticalChancePercent;
    [SerializeField, Min(1f)] private float criticalDamageMultiplier = 1.5f;
    [SerializeField, Min(0)] private int flatArmorPenetration;
    [SerializeField, Min(0)] private int flatWardPenetration;

    [Header("Action-Specific Effects (Optional)")]
    [Tooltip("Effects to trigger when this particular action successfully damages a unit. Does not apply to other actions.")]
    [SerializeField] private EffectCollection onDamageEffects;

    [Header("Biological Mutation Combat Rules")]
    [SerializeField] private bool mutationRushToTarget;
    [SerializeField, Min(0)] private int mutationKnockbackTiles;
    [SerializeField, Range(0f, 100f)] private float mutationNegateChancePercent;
    [SerializeField, Min(1)] private int mutationGuardOwnerTurns = 1;
    [SerializeField] private GameObject mutationDeployPrefab;
    [SerializeField, Min(1)] private int mutationDeployDurationTurns = 2;
    [SerializeField, Min(0)] private int mutationStructureHealth = 30;

    [Header("Defensive Rules")]

    [Tooltip("For ApplyGuard: percentage of ordinary damage prevented until the unit's next faction turn begins. Zero for flat-only Guard.")]
    [SerializeField, Range(0f, 100f)] private float guardDamageReductionPercent = 0f;
    [Tooltip("For ApplyGuard: flat ordinary damage prevented per hit, after Armor, Ward, and resistances. Guard cannot reduce positive damage below 1.")]
    [SerializeField, Min(0)] private int guardFlatDamageReduction = 3;

    public string Id => actionId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public ActionActivationType ActivationType => activationType;
    public bool IsPassive => activationType == ActionActivationType.Passive;
    public ActionMenuCategory Category => category;
    public string SubmenuGroup => submenuGroup;
    public int DisplayOrder => displayOrder;
    public float ApCost => apCost;
    public int MaxUsesPerTurn => maxUsesPerTurn;
    public int ManaRestoreAmount => Mathf.Max(0, manaRestoreAmount);
    public CombatActionBehavior CombatBehavior => combatBehavior;
    public ActionControlTags ControlTags => controlTags;
    public CombatDamageStat DamageStat => damageStat;
    public float CombatRange => combatRange;
    public float DamageMultiplier => damageMultiplier;
    public int FlatDamageBonus => flatDamageBonus;
    public int MinimumDamage => minimumDamage;
    public int HitCount => Mathf.Clamp(hitCount, 1, 5);
    public int AreaThrowRangeInTiles => Mathf.Max(0, areaThrowRangeInTiles);
    public int AreaRadiusInTiles => Mathf.Max(0, areaRadiusInTiles);
    public bool AreaSameFloorOnly => areaSameFloorOnly;
    public bool AreaFriendlyFire => areaFriendlyFire;
    public int AreaHealAmount => Mathf.Max(0, areaHealAmount);
    public StatusDefinition SelfStatus => selfStatus;
    public DamageType DamageType => damageType;
    public DamageComponent[] AdditionalDamage => additionalDamage;
    public EffectCollection OnDamageEffects => onDamageEffects;
    public float GuardDamageReductionPercent => guardDamageReductionPercent;
    public int GuardFlatDamageReduction => Mathf.Max(0, guardFlatDamageReduction);
    public bool MutationRushToTarget => mutationRushToTarget;
    public int MutationKnockbackTiles => Mathf.Max(0, mutationKnockbackTiles);
    public float MutationNegateChancePercent => mutationNegateChancePercent;
    public int MutationGuardOwnerTurns => Mathf.Max(1, mutationGuardOwnerTurns);
    public GameObject MutationDeployPrefab => mutationDeployPrefab;
    public int MutationDeployDurationTurns => Mathf.Max(1, mutationDeployDurationTurns);
    public int MutationStructureHealth => Mathf.Max(0, mutationStructureHealth);
    public float CriticalChancePercent => criticalChancePercent;
    public float CriticalDamageMultiplier => Mathf.Max(1f, criticalDamageMultiplier);
    public int FlatArmorPenetration => Mathf.Max(0, flatArmorPenetration);
    public int FlatWardPenetration => Mathf.Max(0, flatWardPenetration);
}
