using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MissionObjectiveStatus
{
    Locked,
    Active,
    Completed,
    Failed

}

public enum MissionStatus
{
    NotStarted,
    InProgress,
    Victory,
    Defeat

}

// Runtime state: never stored on the reusable ObjectiveDefinition asset.
public sealed class MissionObjectiveEntry
{
    private readonly HashSet<Component> targets = new();
    private readonly HashSet<Component> completedTargets = new();

    public string Id { get; }
    public string Title => Template.Title;
    public string Description => Template.Description;
    public MissionObjectiveType Type => Template.ObjectiveType;
    public ObjectiveDefinition Template { get; }
    public bool Optional { get; }
    public bool StartsActive { get; }
    public bool CountWhileLocked { get; }
    public bool Visible { get; private set; }
    public MissionObjectiveStatus Status { get; private set; }
    public int Progress => completedTargets.Count;
    public int Total { get; private set; }
    public int TargetCount => targets.Count;
    public int Remaining => Mathf.Max(0, Total - Progress);
    public IReadOnlyList<string> Prerequisites { get; }

    public MissionObjectiveEntry(LevelObjectiveSetup setup)
    {
        Template = setup.Template;
        Id = setup.ObjectiveId.Trim();
        Optional = setup.Optional;
        StartsActive = setup.InitiallyActive;
        CountWhileLocked = setup.CountWhileLocked;
        Visible = setup.InitiallyVisible;
        Prerequisites = setup.PrerequisiteObjectiveIds;
        Status = MissionObjectiveStatus.Locked;

    }

    internal void AddTarget(Component target)
    {
        if (target != null)
        {
            targets.Add(target);

        }

    }

    internal void FinishTargetCount()
    {
        Total = Template.CountAllMatchingTargets ? targets.Count : Template.RequiredCount;

    }

    internal bool HasUncompletedTarget(Component target)
    {
        return target != null && targets.Contains(target) && !completedTargets.Contains(target);

    }

    internal bool RecordTarget(Component target)
    {
        if (!HasUncompletedTarget(target) ||
            Status == MissionObjectiveStatus.Completed || Status == MissionObjectiveStatus.Failed ||
            Status == MissionObjectiveStatus.Locked && !CountWhileLocked)
        {
            return false;

        }

        return completedTargets.Add(target);

    }

    internal void SetVisible(bool visible)
    {
        Visible = visible;

    }

    internal void SetStatus(MissionObjectiveStatus status)
    {
        Status = status;

    }

}

public sealed class MissionObjectiveManager : MonoBehaviour
{
    public static MissionObjectiveManager Instance { get; private set; }

    [Header("Level Mission Data")]
    [Tooltip("Assign the per-level mission asset. This owns the objectives and their initial state.")]
    [SerializeField] private LevelMissionData levelMission;
    [SerializeField] private FactionManager factionManager;
    [SerializeField] private bool logProgress = true;

    private readonly List<MissionObjectiveEntry> objectives = new();
    private readonly HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<MissionObjectiveEntry> Objectives => objectives;
    public LevelMissionData LevelMission => levelMission;
    public MissionStatus Status { get; private set; } = MissionStatus.NotStarted;
    public event Action ObjectivesChanged;
    public event Action<MissionStatus> MissionFinished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;

    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one MissionObjectiveManager should exist in the scene.", this);
            enabled = false;

            return;

        }

        Instance = this;

        if (factionManager == null)
        {
            factionManager = FindAnyObjectByType<FactionManager>();

        }

    }

    private void OnEnable()
    {
        CombatActionExecutor.UnitDefeated += OnUnitDefeated;
        ObjectiveInteractable.InteractionCompleted += OnInteractionCompleted;

    }

    private IEnumerator Start()
    {
        // UnitController initializes UnitData and grid nodes in Start.
        yield return null;
        InitializeMission();

    }

    private void InitializeMission()
    {
        if (levelMission == null || levelMission.PlayerFaction == null || factionManager == null)
        {
            Debug.LogError("Assign Level Mission Data (with Player Faction) and FactionManager.", this);
            enabled = false;

            return;

        }

        objectives.Clear();
        ids.Clear();

        foreach (LevelObjectiveSetup setup in levelMission.Objectives)
        {
            if (setup == null || setup.Template == null || string.IsNullOrWhiteSpace(setup.ObjectiveId) ||
                !ids.Add(setup.ObjectiveId.Trim()))
            {
                Debug.LogError("Each level objective needs a template and a unique, nonempty Objective ID.", this);
                enabled = false;

                return;

            }

            objectives.Add(new MissionObjectiveEntry(setup));

        }

        foreach (MissionObjectiveEntry objective in objectives)
        {
            foreach (string requiredId in objective.Prerequisites)
            {
                if (string.IsNullOrWhiteSpace(requiredId) || !ids.Contains(requiredId.Trim()) ||
                    string.Equals(requiredId.Trim(), objective.Id, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError($"Objective '{objective.Id}' has an invalid prerequisite '{requiredId}'.", this);
                    enabled = false;

                    return;

                }

            }

        }

        UnitController[] units = FindObjectsByType<UnitController>();
        ObjectiveInteractable[] interactables = FindObjectsByType<ObjectiveInteractable>();

        foreach (MissionObjectiveEntry objective in objectives)
        {
            if (objective.Type == MissionObjectiveType.EliminateUnits)
            {
                foreach (UnitController unit in units)
                {
                    if (IsLivingUnit(unit) && MatchesTarget(objective.Template, unit))
                    {
                        objective.AddTarget(unit);

                    }

                }

            }
            else if (objective.Type == MissionObjectiveType.InteractWithTargets)
            {
                foreach (ObjectiveInteractable interactable in interactables)
                {
                    if (interactable != null && interactable.isActiveAndEnabled &&
                        !interactable.Used &&
                        string.Equals(interactable.InteractionId?.Trim(), objective.Template.InteractionId?.Trim(),
                            StringComparison.OrdinalIgnoreCase) &&
                        MatchesTarget(objective.Template, interactable))
                    {
                        UnitController targetUnit = interactable.GetComponentInParent<UnitController>();
                        if (targetUnit == null || IsLivingUnit(targetUnit))
                        {
                            objective.AddTarget(interactable);

                        }

                    }

                }

            }

            objective.FinishTargetCount();

            if (objective.TargetCount == 0)
            {
                Debug.LogWarning($"Objective '{objective.Id}' found no matching targets. " +
                                 "Check unit factions, target filters, and ObjectiveInteractable components. " +
                                 "Count-all objectives with zero targets complete automatically.", this);

            }

            if (!objective.Template.CountAllMatchingTargets && objective.Total > objective.TargetCount)
            {
                Debug.LogWarning($"Objective '{objective.Id}' needs {objective.Total} targets, " +
                                 $"but only {objective.TargetCount} were found at mission start.", this);

            }

        }

        Status = MissionStatus.InProgress;
        RefreshDependencies();
        PublishChange();
        CheckMissionCompletion();

        if (logProgress)
        {
            foreach (MissionObjectiveEntry objective in objectives)
            {
                Debug.Log($"Objective '{objective.Title}': {objective.Progress}/{objective.Total}, " +
                          $"{objective.Status}, optional={objective.Optional}.", this);

            }

        }

    }

    private bool MatchesTarget(ObjectiveDefinition template, Component target)
    {
        if (template == null || target == null)
        {
            return false;

        }

        UnitFaction member = target.GetComponentInParent<UnitFaction>();
        FactionData faction = member != null ? member.Faction : null;
        MissionTargetIdentity identity = target.GetComponentInParent<MissionTargetIdentity>();

        switch (template.TargetFilter)
        {
            case MissionTargetFilter.Any:
                {
                    return true;

                }
            case MissionTargetFilter.HostileToPlayer:
                {
                    return faction != null &&
                           factionManager.AreHostile(levelMission.PlayerFaction, faction);

                }
            case MissionTargetFilter.SpecificFaction:
                {
                    return template.TargetFaction != null && faction == template.TargetFaction;

                }
            case MissionTargetFilter.TargetId:
                {
                    return identity != null && !string.IsNullOrWhiteSpace(template.TargetKey) &&
                           string.Equals(identity.TargetId?.Trim(), template.TargetKey.Trim(),
                               StringComparison.OrdinalIgnoreCase);

                }
            case MissionTargetFilter.PrefabTypeId:
                {
                    return identity != null && !string.IsNullOrWhiteSpace(template.TargetKey) &&
                           string.Equals(identity.PrefabTypeId?.Trim(), template.TargetKey.Trim(),
                               StringComparison.OrdinalIgnoreCase);

                }
            case MissionTargetFilter.CategoryId:
                {
                    return identity != null && !string.IsNullOrWhiteSpace(template.TargetKey) &&
                           string.Equals(identity.CategoryId?.Trim(), template.TargetKey.Trim(),
                               StringComparison.OrdinalIgnoreCase);

                }
            default:
                {
                    return false;

                }

        }

    }

    public bool CanReceiveInteraction(UnitController actor, ObjectiveInteractable target)
    {
        if (Status != MissionStatus.InProgress || !IsLivingUnit(actor) || target == null ||
            levelMission == null)
        {
            return false;

        }

        UnitFaction actorFaction = actor.GetComponent<UnitFaction>();
        if (actorFaction == null || actorFaction.Faction != levelMission.PlayerFaction)
        {
            return false;

        }

        foreach (MissionObjectiveEntry objective in objectives)
        {
            if (objective.Type == MissionObjectiveType.InteractWithTargets &&
                objective.Status == MissionObjectiveStatus.Active &&
                objective.HasUncompletedTarget(target))
            {
                return true;

            }

        }

        return false;

    }

    private void OnInteractionCompleted(UnitController actor, ObjectiveInteractable target)
    {
        if (!CanReceiveInteraction(actor, target))
        {
            return;

        }

        RecordMatchingTarget(MissionObjectiveType.InteractWithTargets, target);

    }

    private void OnUnitDefeated(UnitController attacker, UnitController defeated)
    {
        if (Status != MissionStatus.InProgress || defeated == null)
        {
            return;

        }

        RecordMatchingTarget(MissionObjectiveType.EliminateUnits, defeated);

        if (Status == MissionStatus.InProgress && !HasLivingPlayerUnit())
        {
            FinishMission(MissionStatus.Defeat);

        }

    }

    private void RecordMatchingTarget(MissionObjectiveType type, Component target)
    {
        bool changed = false;

        foreach (MissionObjectiveEntry objective in objectives)
        {
            if (objective.Type != type || !objective.RecordTarget(target))
            {
                continue;

            }

            changed = true;

            if (logProgress)
            {
                Debug.Log($"{objective.Title}: {objective.Progress}/{objective.Total}.", this);

            }

        }

        if (!changed)
        {
            return;

        }

        RefreshDependencies();
        PublishChange();
        CheckMissionCompletion();

    }

    private void RefreshDependencies()
    {
        // Repeat to resolve chains where completing one objective unlocks another.
        bool changed;

        do
        {
            changed = false;

            foreach (MissionObjectiveEntry objective in objectives)
            {
                if (objective.Status == MissionObjectiveStatus.Locked && objective.StartsActive &&
                    HasCompletedPrerequisites(objective))
                {
                    objective.SetStatus(MissionObjectiveStatus.Active);
                    changed = true;

                }

                if (objective.Status == MissionObjectiveStatus.Active &&
                    objective.Progress >= objective.Total)
                {
                    objective.SetStatus(MissionObjectiveStatus.Completed);
                    changed = true;

                }

            }

        } while (changed);

    }

    private bool HasCompletedPrerequisites(MissionObjectiveEntry objective)
    {
        foreach (string prerequisiteId in objective.Prerequisites)
        {
            MissionObjectiveEntry prerequisite = FindObjective(prerequisiteId);
            if (prerequisite == null || prerequisite.Status != MissionObjectiveStatus.Completed)
            {
                return false;

            }

        }

        return true;

    }

    public MissionObjectiveEntry FindObjective(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;

        }

        foreach (MissionObjectiveEntry objective in objectives)
        {
            if (string.Equals(objective.Id, id.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return objective;

            }

        }

        return null;

    }

    public void SetObjectiveVisible(string id, bool visible)
    {
        MissionObjectiveEntry objective = FindObjective(id);
        if (objective == null || objective.Visible == visible)
        {
            return;

        }

        objective.SetVisible(visible);
        PublishChange();

    }

    public bool ActivateObjective(string id)
    {
        MissionObjectiveEntry objective = FindObjective(id);
        if (Status != MissionStatus.InProgress || objective == null ||
            objective.Status != MissionObjectiveStatus.Locked || !HasCompletedPrerequisites(objective))
        {
            return false;

        }

        objective.SetStatus(MissionObjectiveStatus.Active);
        RefreshDependencies();
        PublishChange();
        CheckMissionCompletion();

        return true;

    }

    public void FailObjective(string id)
    {
        MissionObjectiveEntry objective = FindObjective(id);
        if (objective == null || objective.Status == MissionObjectiveStatus.Failed ||
            objective.Status == MissionObjectiveStatus.Completed)
        {
            return;

        }

        objective.SetStatus(MissionObjectiveStatus.Failed);
        PublishChange();
        CheckMissionCompletion();

    }

    private void CheckMissionCompletion()
    {
        if (Status != MissionStatus.InProgress)
        {
            return;

        }

        bool anyRequired = false;

        foreach (MissionObjectiveEntry objective in objectives)
        {
            if (objective.Optional)
            {
                continue;

            }

            anyRequired = true;

            if (objective.Status == MissionObjectiveStatus.Failed)
            {
                FinishMission(MissionStatus.Defeat);

                return;

            }

            if (objective.Status != MissionObjectiveStatus.Completed)
            {
                return;

            }

        }

        if (anyRequired)
        {
            FinishMission(MissionStatus.Victory);

        }

    }

    private bool HasLivingPlayerUnit()
    {
        foreach (UnitFaction member in FindObjectsByType<UnitFaction>())
        {
            if (member != null && member.Faction == levelMission.PlayerFaction &&
                IsLivingUnit(member.GetComponent<UnitController>()))
            {
                return true;

            }

        }

        return false;

    }

    private static bool IsLivingUnit(UnitController unit)
    {
        return unit != null && unit.isActiveAndEnabled &&
               unit.data != null && unit.data.currentHealth > 0;

    }

    private void FinishMission(MissionStatus result)
    {
        if (Status != MissionStatus.InProgress)
        {
            return;

        }

        Status = result;
        Debug.Log(result == MissionStatus.Victory ? "MISSION COMPLETE — VICTORY!" : "MISSION FAILED — DEFEAT!", this);
        PublishChange();
        MissionFinished?.Invoke(result);

    }

    private void PublishChange()
    {
        ObjectivesChanged?.Invoke();

    }

    // Compatibility with the earlier CivilianRescueInteractable.cs while you migrate.
    // The generic ObjectiveInteractable is still required on the civilian.
    public bool ReportCivilianRescued(UnitController rescuer, UnitController civilian)
    {
        ObjectiveInteractable target = civilian != null ? civilian.GetComponent<ObjectiveInteractable>() : null;
        if (target == null || !CanReceiveInteraction(rescuer, target))
        {
            return false;

        }

        OnInteractionCompleted(rescuer, target);

        return true;

    }

    public static bool AreAdjacentTiles(UnitController a, UnitController b)
    {
        if (a == null || b == null || a.currentNode == null || b.currentNode == null ||
            a.currentNode.floorID != b.currentNode.floorID)
        {
            return false;

        }

        Vector2Int offset = a.currentNode.coords - b.currentNode.coords;

        return Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) == 1;

    }

    private void OnDisable()
    {
        CombatActionExecutor.UnitDefeated -= OnUnitDefeated;
        ObjectiveInteractable.InteractionCompleted -= OnInteractionCompleted;

        if (Instance == this)
        {
            Instance = null;

        }

    }

}
