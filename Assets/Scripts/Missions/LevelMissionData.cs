using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LevelObjectiveSetup
{
    [Tooltip("Reusable objective rules. The level controls activation, visibility, and requirement.")]
    [SerializeField] private ObjectiveDefinition template;

    [Header("Level-specific Identity")]
    [Tooltip("Unique within this mission. Prerequisite IDs refer to this ID.")]
    [SerializeField] private string objectiveId;

    [Header("Level-specific State")]
    [SerializeField] private bool optional;
    [Header("Objective World Marker")]
    [Tooltip("Higher number wins when several objectives mark the same unit/object. " +
             "At equal values, specific Target ID objectives beat broad elimination objectives.")]
    [SerializeField] private int worldMarkerPriority;

    [SerializeField] private bool initiallyVisible = true;
    [SerializeField] private bool initiallyActive = true;
    [Tooltip("If true, eliminations occurring before activation still count.")]
    [SerializeField] private bool countWhileLocked = true;
    [SerializeField] private List<string> prerequisiteObjectiveIds = new();

    public ObjectiveDefinition Template => template;
    public string ObjectiveId => objectiveId;
    public bool Optional => optional;
    public int WorldMarkerPriority => worldMarkerPriority;
    public bool InitiallyVisible => initiallyVisible;
    public bool InitiallyActive => initiallyActive;
    public bool CountWhileLocked => countWhileLocked;
    public IReadOnlyList<string> PrerequisiteObjectiveIds => prerequisiteObjectiveIds;

}

[CreateAssetMenu(fileName = "NewLevelMission", menuName = "Game/Missions/Level Mission Data")]
public sealed class LevelMissionData : ScriptableObject
{
    [Header("This level's mission information")]
    [SerializeField] private string missionId = "test-mission";
    [SerializeField] private string missionName = "Test Mission";
    [SerializeField] private FactionData playerFaction;
    [SerializeField] private List<LevelObjectiveSetup> objectives = new();
    [SerializeField] private MissionLootTable lootTable;

    public string MissionId => missionId;
    public string MissionName => missionName;
    public FactionData PlayerFaction => playerFaction;
    public IReadOnlyList<LevelObjectiveSetup> Objectives => objectives;
    public MissionLootTable LootTable => lootTable;

}
