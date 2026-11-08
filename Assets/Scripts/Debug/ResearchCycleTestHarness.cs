using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class ResearchCycleTestHarness : MonoBehaviour
{
    [Header("Used only when starting this scene directly (without the hub)")]
    [SerializeField] private UnitRoster starterRosterPrefab;
    [SerializeField] private ItemStorage itemStoragePrefab;
    [SerializeField] private ResourceStockpile resourceStockpilePrefab;

    private static int victoriesThisPlaySession;
    private MissionObjectiveManager mission;
    private MissionStatus result = MissionStatus.NotStarted;

    private void Awake()
    {
        if (ItemStorage.Instance == null && itemStoragePrefab != null)
        {
            Instantiate(itemStoragePrefab);

        }

        if (ResourceStockpile.Instance == null && resourceStockpilePrefab != null)
        {
            Instantiate(resourceStockpilePrefab);

        }

        if (UnitRoster.Instance == null && starterRosterPrefab != null)
        {
            Instantiate(starterRosterPrefab);

        }

    }

    private void Start()
    {
        mission = MissionObjectiveManager.Instance;
        if (mission != null)
        {
            mission.MissionFinished += OnMissionFinished;

        }

        EnsureCrewDeployed();
        StartCoroutine(FinishStartup());

    }

    private void EnsureCrewDeployed()
    {
        FactionData player = mission != null && mission.LevelMission != null
            ? mission.LevelMission.PlayerFaction : null;
        TurnManager turns = TurnManager.Instance;
        if (player != null && turns != null && turns.HasActiveUnits(player))
        {

            return;

        }

        UnitRoster roster = UnitRoster.Instance;
        GridManager grid = FindAnyObjectByType<GridManager>();
        MissionCrewSpawnArea crewArea = FindAnyObjectByType<MissionCrewSpawnArea>();
        if (roster == null || grid == null || crewArea == null)
        {
            Debug.LogError("[ResearchCycleTest] Cannot deploy crew. Check for an active UnitRoster, " +
                "GridManager and MissionCrewSpawnArea in the test scene.", this);

            return;

        }

        int readyCrew = 0;
        for (int slot = 0; slot < roster.UnlockedCrewSlots; slot++)
        {
            if (roster.TryGetDeploymentUnit(slot, out UnitData unit) && unit.currentHealth > 0)
            {
                readyCrew++;

            }

        }

        if (readyCrew == 0)
        {
            Debug.LogError("[ResearchCycleTest] Roster exists, but no living units are assigned " +
                "to unlocked deployment slots. Select crew in the Hub.", this);

            return;

        }

        Debug.LogWarning($"[ResearchCycleTest] No active player units found after GridManager Awake. " +
            $"Retrying crew deployment ({readyCrew} ready).", this);
        crewArea.PrepareDeployment(grid);
        grid.InitializeUnits();

        if (player == null || turns == null || !turns.HasActiveUnits(player))
        {
            Debug.LogError("[ResearchCycleTest] Crew deployment did not produce active player units. " +
                "Inspect the [CrewSpawn] messages for the reason.", this);

            return;

        }

        Debug.Log("[ResearchCycleTest] Player crew deployed; turn order can begin.", this);

    }

    private IEnumerator FinishStartup()
    {

        yield return null;

        VisibilityManager visibility = FindAnyObjectByType<VisibilityManager>();
        if (visibility != null)
        {
            visibility.UpdateVisibility();

        }

        TurnManager turns = TurnManager.Instance;
        FactionData player = mission != null && mission.LevelMission != null
            ? mission.LevelMission.PlayerFaction : null;

        if (turns != null && turns.ActiveFaction == null && player != null && turns.HasActiveUnits(player) &&
            mission.Status != MissionStatus.Victory && mission.Status != MissionStatus.Defeat)
        {
            Debug.LogWarning("[ResearchCycleTest] Crew is deployed but no faction turn started. " +
                "Rebuilding the initial turn order.", this);
            turns.RestartTurns();

        }

    }

    private void OnDestroy()
    {
        if (mission != null)
        {
            mission.MissionFinished -= OnMissionFinished;

        }

    }

    private void OnMissionFinished(MissionStatus status)
    {
        result = status;
        if (status == MissionStatus.Victory)
        {
            victoriesThisPlaySession++;
            Debug.Log($"[ResearchCycleTest] Victory #{victoriesThisPlaySession} this Play session. " +
                "MissionFinished fired; research progression must subscribe separately.", this);

        }

    }

}
