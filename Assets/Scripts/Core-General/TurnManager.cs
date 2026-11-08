using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;



public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }


    [Header("References")]
    [SerializeField] private FactionManager factionManager;
    [SerializeField] private FactionData playerFaction;

    [Header("Turn Initiative")]
    [SerializeField, Min(2)] private int initiativeDieSides = 20;

    [Header("Turn Presentation")]
    [SerializeField, Min(0f)] private float aiAutoSkipDelaySeconds = 1.5f;

    [Header("Testing")]
    [SerializeField] private bool enterEndsPlayerTurn = true;


    private readonly List<FactionData> turnOrder = new List<FactionData>();
    // Initiative is rolled once per AI faction per battle, not every round.
    private readonly Dictionary<FactionData, int> aiInitiativeRolls =
        new Dictionary<FactionData, int>();
    private int turnIndex = -1;
    private Coroutine aiAutoSkipCoroutine;

    public int Round { get; private set; }
    public FactionData ActiveFaction { get; private set; }
    public FactionData PlayerFaction => playerFaction;
    public bool IsPlayerTurn => ActiveFaction != null && ActiveFaction == playerFaction;

    // Other systems can subscribe later (e.g. stamina refresh, HUD, AI).
    public event Action<int> RoundStarted;
    public event Action<FactionData> TurnStarted;
    public event Action<FactionData> TurnEnded;
    public event Action TurnStateRestored;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one TurnManager exists in the loaded scenes.", this);
            enabled = false;

            return;

        }

        Instance = this;

        if (factionManager == null)
        {
            factionManager = FindAnyObjectByType<FactionManager>();

        }

    }

    private void Start()
    {
        if (factionManager == null || playerFaction == null)
        {
            Debug.LogError("TurnManager requires a FactionManager and a Player Faction asset.", this);
            enabled = false;

            return;

        }

        if (!factionManager.IsRegistered(playerFaction))
        {
            Debug.LogError(
                $"Player faction '{playerFaction.name}' is not registered. " +
                "Check that its asset is under a Resources/Factions folder.", this);

            enabled = false;

            return;

        }

        RestartTurns();

    }

    private void Update()
    {
        if (enterEndsPlayerTurn && IsPlayerTurn &&
            Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            EndTurn();

        }

    }


    public void RestartTurns()
    {
        if (aiAutoSkipCoroutine != null)
        {
            StopCoroutine(aiAutoSkipCoroutine);
            aiAutoSkipCoroutine = null;

        }

        Round = 0;
        ActiveFaction = null;
        turnIndex = -1;
        turnOrder.Clear();
        aiInitiativeRolls.Clear();
        AdvanceTurn();

    }



    public void EndTurn()
    {
        if (!IsPlayerTurn)
        {
            return;

        }

        AdvanceTurn();

    }

    // Called by SimpleFactionAI after it finishes controlling a non-player faction.
    public bool CompleteNonPlayerTurn(FactionData faction)
    {
        if (faction == null || ActiveFaction != faction || faction == playerFaction)
        {
            return false;

        }

        AdvanceTurn();

        return true;

    }

    public bool IsFactionTurn(FactionData faction)
    {
        return faction != null && ActiveFaction == faction;

    }

    public TurnSaveData CaptureTurnState()
    {
        TurnSaveData saved = new TurnSaveData
        {
            currentRound = Round, activeFactionId = ActiveFaction != null ? ActiveFaction.Id : "", activeTurnIndex = turnIndex
        };

        // Save faction turn order
        foreach (FactionData faction in turnOrder)
        {
            if (faction != null)
            {
                saved.factionTurnOrder.Add(faction.Id);
            }
        }

        // Save initiative rolls
        foreach (KeyValuePair<FactionData, int> entry in aiInitiativeRolls)
        {
            if (entry.Key == null)
                continue;

            saved.initiativeRolls.Add(new FactionInitiativeSaveData
            { factionId = entry.Key.Id, initiativeRoll = entry.Value 
            });
        }

        return saved;
    }

    public bool TryRestoreTurnState(TurnSaveData saved)
    {
        if (saved == null || factionManager == null || saved.factionTurnOrder == null || saved.initiativeRolls == null || saved.currentRound < 1)
        {
            Debug.LogError("Invalid turn save data.");
            return false;
        }

        // Initially support restoring during the player's turn.
        if (!IsPlayerTurn || !string.Equals(saved.activeFactionId, playerFaction.Id, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("Turn restoration currently supports player turns only.");
            return false;
        }

        List<FactionData> restoredOrder = new List<FactionData>();
        Dictionary<FactionData, int> restoredRolls = new Dictionary<FactionData, int>();

        HashSet<FactionData> seenFactions = new HashSet<FactionData>();

        // Rebuild the saved turn order.
        foreach (string factionId in saved.factionTurnOrder)
        {
            if (!factionManager.TryGetFaction(factionId, out FactionData faction) || !seenFactions.Add(faction))
            {
                Debug.LogError($"Invalid or duplicate faction: {factionId}");
                return false;
            }

            restoredOrder.Add(faction);
        }

        // Validate the saved turn index.
        if (saved.activeTurnIndex < 0 || saved.activeTurnIndex >= restoredOrder.Count || restoredOrder[saved.activeTurnIndex] != playerFaction)
        {
            Debug.LogError("Saved turn index does not match the active faction.");
            return false;
        }

        // Restore initiative rolls.
        foreach (FactionInitiativeSaveData roll in saved.initiativeRolls)
        {
            if (roll == null || !factionManager.TryGetFaction(roll.factionId, out FactionData faction) || faction == playerFaction || roll.initiativeRoll < 1 || restoredRolls.ContainsKey(faction))
            {
                Debug.LogError("Invalid saved initiative roll.");
                return false;
            }

            restoredRolls.Add(faction, roll.initiativeRoll);
        }

        // All validation passed. Apply the saved state.
        if (aiAutoSkipCoroutine != null)
        {
            StopCoroutine(aiAutoSkipCoroutine);
            aiAutoSkipCoroutine = null;
        }

        turnOrder.Clear();
        turnOrder.AddRange(restoredOrder);

        aiInitiativeRolls.Clear();

        foreach (KeyValuePair<FactionData, int> roll in restoredRolls)
        {
            aiInitiativeRolls.Add(roll.Key, roll.Value);
        }

        Round = saved.currentRound;
        turnIndex = saved.activeTurnIndex;
        ActiveFaction = playerFaction;

        Debug.Log($"Restored Round {Round} - {ActiveFaction.DisplayName}'s turn.");

        TurnStateRestored?.Invoke();

        return true;
    }

    public bool HasActiveUnits(FactionData faction)
    {
        if (faction == null)
        {
            return false;

        }

        UnitFaction[] units = FindObjectsByType<UnitFaction>();

        foreach (UnitFaction unit in units)
        {
            if (unit.isActiveAndEnabled && unit.Faction == faction)
            {
                return true;

            }

        }

        return false;

    }

    private void RebuildTurnOrder()
    {
        turnOrder.Clear();
        HashSet<FactionData> activeFactions = new HashSet<FactionData>();

        // Scan current loaded scenes for participating units, not for all assets.
        UnitFaction[] units = FindObjectsByType<UnitFaction>();

        foreach (UnitFaction unit in units)
        {
            if (!unit.isActiveAndEnabled)
            {
                continue;

            }

            FactionData faction = unit.Faction;
            if (faction != null && factionManager.IsRegistered(faction))
            {
                activeFactions.Add(faction);

            }

        }

        foreach (FactionData faction in activeFactions)
        {
            if (faction == playerFaction || aiInitiativeRolls.ContainsKey(faction))
            {
                continue;

            }

            int sides = Mathf.Max(2, initiativeDieSides);
            int roll = UnityEngine.Random.Range(1, sides + 1);

            aiInitiativeRolls.Add(faction, roll);
            Debug.Log($"Initiative: {faction.DisplayName} rolled {roll} (d{sides}).");

        }

        turnOrder.AddRange(activeFactions);
        turnOrder.Sort((a, b) =>
        {
            if (a == b) return 0;

            // The player always acts first.
            if (a == playerFaction)
            {
                return -1;

            }
            if (b == playerFaction)
            {
                return 1;

            }

            int byRoll = aiInitiativeRolls[b].CompareTo(aiInitiativeRolls[a]);
            if (byRoll != 0)
            {
                return byRoll;

            }

            return string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);

        });

        Debug.Log("Faction turn order: " + string.Join(" -> ",
            turnOrder.ConvertAll(faction => faction.DisplayName)));

    }

    private void RefillActionPointsForFaction(FactionData faction)
    {
        if (faction == null)
        {

            return;

        }

        int refilledUnits = 0;

        foreach (UnitFaction member in FindObjectsByType<UnitFaction>())
        {
            if (member == null || !member.isActiveAndEnabled || member.Faction != faction)
            {
                continue;

            }

            UnitController unit = member.GetComponent<UnitController>();
            if (unit == null || unit.data == null || unit.data.currentHealth <= 0)
            {
                continue;

            }

            int missingAP = unit.data.maxActionPoints - unit.data.currentActionPoints;
            if (missingAP > 0)
            {
                unit.data.ModifyActionPoints(missingAP);
                refilledUnits++;

            }

        }

        if (refilledUnits > 0)
        {
            Debug.Log($"Restored AP for {refilledUnits} unit(s) in {faction.DisplayName}.", this);

        }

    }

    private void AdvanceTurn()
    {
        if (ActiveFaction != null)
        {
            FactionData endedFaction = ActiveFaction;
            ActiveFaction = null;
            if (endedFaction == playerFaction)
            {
                CombatActionExecutor.Instance?.CancelTargeting();

            }

            TurnEnded?.Invoke(endedFaction);

        }

        // A loop (rather than recursion) allows skipping multiple AI factions safely.
        while (true)
        {
            if (turnIndex + 1 >= turnOrder.Count)
            {
                RebuildTurnOrder();
                turnIndex = -1;

                if (turnOrder.Count == 0)
                {
                    Debug.LogError("[TurnManager] No active factions are deployed. " +
                        "Check mission crew spawning before starting turns.", this);

                    return;

                }

                Round++;
                Debug.Log($"=== ROUND {Round}: {turnOrder.Count} participating factions ===");
                RoundStarted?.Invoke(Round);

            }

            FactionData candidate = turnOrder[++turnIndex];

            // A faction may have lost its last unit since the round began.
            if (!HasActiveUnits(candidate))
            {
                continue;

            }

            ActiveFaction = candidate;
            RefillActionPointsForFaction(candidate);
            Debug.Log($"Round {Round} - {candidate.DisplayName}'s turn.");
            TurnStarted?.Invoke(candidate);

            if (candidate == playerFaction)
            {
                return;

            }

            // Optional prototype AI controls non-player faction turns.
            SimpleFactionAI simpleAI = FindAnyObjectByType<SimpleFactionAI>();
            if (simpleAI != null && simpleAI.isActiveAndEnabled && simpleAI.TryBeginTurn(candidate))
            {
                return;

            }

            Debug.Log($"Auto-skipping non-player faction: {candidate.DisplayName} in {aiAutoSkipDelaySeconds:0.##}s.");
            aiAutoSkipCoroutine = StartCoroutine(AutoSkipAiTurn(candidate));

            return;

        }

    }

    private IEnumerator AutoSkipAiTurn(FactionData faction)
    {
        // Even at zero delay, wait one frame to avoid recursive turn advances.
        if (aiAutoSkipDelaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(aiAutoSkipDelaySeconds);

        }
        else
        {
            yield return null;

        }

        aiAutoSkipCoroutine = null;
        if (ActiveFaction == faction)
        {
            AdvanceTurn();

        }

    }

    private void OnDisable()
    {
        if (aiAutoSkipCoroutine != null)
        {
            StopCoroutine(aiAutoSkipCoroutine);
            aiAutoSkipCoroutine = null;

        }

    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;

        }

    }

}
