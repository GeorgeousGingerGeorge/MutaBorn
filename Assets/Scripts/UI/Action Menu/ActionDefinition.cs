using UnityEngine;

public enum ActionMenuCategory
{
    BasicAttacks,
    Defense,
    Abilities
}

/// <summary>
/// Reusable MENU METADATA for an action. This is not the action executor.
/// Do not store a unit's remaining AP, cooldown, or usage count on this asset.
/// </summary>
[CreateAssetMenu(fileName = "NewAction", menuName = "Game/Actions/Action Definition")]
public class ActionDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string actionId = "new-action";
    [SerializeField] private string displayName = "New Action";
    [TextArea(2, 5)] [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Menu")]
    [SerializeField] private ActionMenuCategory category = ActionMenuCategory.Abilities;
    [SerializeField, Min(0)] private float apCost = 5f;

    public string Id => actionId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public ActionMenuCategory Category => category;
    public float ApCost => apCost;
}
