using UnityEngine;

// Main buttons in the current MutaBorn Action Menu.
public enum ActionMenuCategory
{
    BasicAttacks,
    Defense,
    Abilities

}

// Whether the player deliberately activates this, or it operates automatically.
// Overwatch is Active (the player prepares it); Vampirism is Passive.
public enum ActionActivationType
{
    Active,
    Passive

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
}
