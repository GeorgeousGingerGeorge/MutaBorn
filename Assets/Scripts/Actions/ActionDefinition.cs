using UnityEngine;

// Main buttons in the current MutaBorn Action Menu.
public enum ActionMenuCategory
{
    BasicAttacks,
    Defense,
    Abilities,
    Interact

}

// Whether the player deliberately activates this, or it operates automatically.
// Overwatch is Active (the player prepares it); Vampirism is Passive.
public enum ActionActivationType
{
    Active,
    Passive

}

// Combat behavior is separate from the menu category and Active/Passive classification.
public enum CombatActionBehavior
{
    None,
    DamageUnit,
    ApplyGuard,
    InspectUnit

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

    [Header("Combat Rules")]

    [Tooltip("Select the gameplay behavior. None is for passive or not-yet-implemented actions.")]
    [SerializeField] private CombatActionBehavior combatBehavior = CombatActionBehavior.None;

    [Tooltip("Which calculated UnitData damage stat this attack uses. Includes relevant equipment and mutation bonuses.")]
    [SerializeField] private CombatDamageStat damageStat = CombatDamageStat.Melee;

    [SerializeField, Min(0.1f)] private float combatRange = 2.5f;
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;
    [SerializeField] private int flatDamageBonus = 0;
    [SerializeField, Min(0)] private int minimumDamage = 5;

    [Header("Defensive Rules")]

    [Tooltip("For ApplyGuard: incoming damage reduction percentage until the unit's next faction turn begins.")]
    [SerializeField, Range(0f, 100f)] private float guardDamageReductionPercent = 50f;

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
    public CombatActionBehavior CombatBehavior => combatBehavior;
    public CombatDamageStat DamageStat => damageStat;
    public float CombatRange => combatRange;
    public float DamageMultiplier => damageMultiplier;
    public int FlatDamageBonus => flatDamageBonus;
    public int MinimumDamage => minimumDamage;
    public float GuardDamageReductionPercent => guardDamageReductionPercent;
}
