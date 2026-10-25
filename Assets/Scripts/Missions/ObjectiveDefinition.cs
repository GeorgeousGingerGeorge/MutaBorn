using UnityEngine;

public enum MissionObjectiveType
{
    EliminateUnits,
    InteractWithTargets

}

public enum MissionTargetFilter
{
    Any,
    HostileToPlayer,
    SpecificFaction,
    TargetId,
    PrefabTypeId,
    CategoryId

}

[CreateAssetMenu(fileName = "NewObjective", menuName = "Game/Missions/Objective Template")]
public sealed class ObjectiveDefinition : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string title = "New Objective";
    [TextArea(2, 4)]
    [SerializeField] private string description;

    [Header("Objective Rules")]
    [SerializeField] private MissionObjectiveType objectiveType;
    [SerializeField] private MissionTargetFilter targetFilter;
    [Tooltip("Used by Specific Faction; ignored for other target filters.")]
    [SerializeField] private FactionData targetFaction;
    [Tooltip("Used by Target ID, Prefab Type ID, or Category ID filters.")]
    [SerializeField] private string targetKey;

    [Header("Interaction Rules")]
    [Tooltip("Only for Interact With Targets. Must match ObjectiveInteractable.Interaction ID.")]
    [SerializeField] private string interactionId = "help";

    [Header("Count Rules")]
    [Tooltip("If true, the objective requires every matching target found when the mission starts.")]
    [SerializeField] private bool countAllMatchingTargets = true;
    [Tooltip("Used when Count All Matching Targets is false.")]
    [SerializeField, Min(1)] private int requiredCount = 1;

    public string Title => title;
    public string Description => description;
    public MissionObjectiveType ObjectiveType => objectiveType;
    public MissionTargetFilter TargetFilter => targetFilter;
    public FactionData TargetFaction => targetFaction;
    public string TargetKey => targetKey;
    public string InteractionId => interactionId;
    public bool CountAllMatchingTargets => countAllMatchingTargets;
    public int RequiredCount => requiredCount;

}
