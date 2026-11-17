using UnityEngine;

public enum MissionObjectiveType
{
    EliminateUnits,
    InteractWithTargets,
    ExtractPlayerUnits,
    DestroyObjects,
    ReachArea,
    CaptureUnits,
    ProtectUnits

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

    [Header("World Marker (Optional Override)")]
    [Tooltip("Override the automatic mission-type icon for world markers. Leave empty to use its default icon.")]
    [SerializeField] private Sprite worldMarkerIcon;

    [Header("Objective Rules")]
    [SerializeField] private MissionObjectiveType objectiveType;
    [SerializeField] private MissionTargetFilter targetFilter;
    [Tooltip("Used by Specific Faction; ignored for other target filters.")]
    [SerializeField] private FactionData targetFaction;
    [Tooltip("Used by Target ID, Prefab Type ID, or Category ID filters.")]
    [SerializeField] private string targetKey;

    [Header("Interaction Rules")]
    [Tooltip("For Interact With Targets: match the Interaction ID on any WorldInteractable (chests, switches, NPCs, etc.).")]
    [SerializeField] private string interactionId = "help";

    [Header("Count Rules")]
    [Tooltip("For unit/object/interaction objectives: require every matching target. Reach Area always uses Required Count.")]
    [SerializeField] private bool countAllMatchingTargets = true;
    [Tooltip("Number of targets, or number of allies needed in a Reach Area.")]
    [SerializeField, Min(1)] private int requiredCount = 1;

    public string Title => title;
    public string Description => description;
    public Sprite WorldMarkerIcon => worldMarkerIcon;
    public MissionObjectiveType ObjectiveType => objectiveType;
    public MissionTargetFilter TargetFilter => targetFilter;
    public FactionData TargetFaction => targetFaction;
    public string TargetKey => targetKey;
    public string InteractionId => interactionId;
    public bool CountAllMatchingTargets => countAllMatchingTargets;
    public int RequiredCount => requiredCount;

}
