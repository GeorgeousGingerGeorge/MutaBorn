using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Faction-based turns for the tactical scene.
/// Only registered factions with active UnitFaction components receive turns.
/// AI factions roll initiative once when first participating in a battle.
/// Non-player turns are skipped until AI is implemented.
/// </summary>
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
    public bool IsPlayerTurn => ActiveFaction != null && ActiveFaction == playerFaction;

    // Other systems can subscribe later (e.g. stamina refresh, HUD, AI).
    public event Action<int> RoundStarted;
    public event Action<FactionData> TurnStarted;
    public event Action<FactionData> TurnEnded;

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
            factionManager = FindFirstObjectByType<FactionManager>();
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

    /// <summary>
    /// Call this after spawning units if no battlefield units existed at Start().
    /// Resets the round counter and all initiative rolls, then starts the first eligible faction's turn.
    /// </summary>
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

    /// <summary>Ends the player-controlled faction's current turn.</summary>
    public void EndTurn()
    {
        if (!IsPlayerTurn)
            return;

        AdvanceTurn();
    }

    public bool IsFactionTurn(FactionData faction)
    {
        return faction != null && ActiveFaction == faction;
    }

    /// <summary>
    /// "Active" currently means an enabled UnitFaction on an active GameObject.
    /// Hook this into your unit death/alive state when that system is ready.
    /// </summary>
    public bool HasActiveUnits(FactionData faction)
    {
        if (faction == null)
            return false;

        UnitFaction[] units = FindObjectsByType<UnitFaction>();

        foreach (UnitFaction unit in units)
        {
            if (unit.isActiveAndEnabled && unit.Faction == faction)
                return true;
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
                continue;

            FactionData faction = unit.Faction;
            if (faction != null && factionManager.IsRegistered(faction))
                activeFactions.Add(faction);
        }

        // New AI factions roll once the first time they have active units.
        // Rolls persist for the entire battle, including later rounds.
        foreach (FactionData faction in activeFactions)
        {
            if (faction == playerFaction || aiInitiativeRolls.ContainsKey(faction))
                continue;

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
            if (a == playerFaction) return -1;
            if (b == playerFaction) return 1;

            // Highest AI die roll takes its turn first.
            int byRoll = aiInitiativeRolls[b].CompareTo(aiInitiativeRolls[a]);
            if (byRoll != 0) return byRoll;

            // Ties use faction ID so the order is unambiguous and stable.
            return string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);
        });

        Debug.Log("Faction turn order: " + string.Join(" -> ",
            turnOrder.ConvertAll(faction => faction.DisplayName)));
    }

    private void AdvanceTurn()
    {
        if (ActiveFaction != null)
        {
            FactionData endedFaction = ActiveFaction;
            ActiveFaction = null;
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
                    Debug.Log("TurnManager: No factions currently have active units.");
                    return;
                }

                // Until AI and defeat handling exist, don't endlessly skip rounds
                // if the player has no participating units.
                if (!turnOrder.Contains(playerFaction))
                {
                    Debug.Log("TurnManager: No player units remain on the battlefield. Turns paused.");
                    return;
                }

                Round++;
                Debug.Log($"=== ROUND {Round}: {turnOrder.Count} participating factions ===");
                RoundStarted?.Invoke(Round);
            }

            FactionData candidate = turnOrder[++turnIndex];

            // A faction may have lost its last unit since the round began.
            if (!HasActiveUnits(candidate))
                continue;

            ActiveFaction = candidate;
            Debug.Log($"Round {Round} - {candidate.DisplayName}'s turn.");
            TurnStarted?.Invoke(candidate);

            if (candidate == playerFaction)
                return; // Wait for Enter or EndTurn() from UI/input code.

            // Leave this turn active briefly so the turn announcement can be seen.
            // AI behavior can replace this coroutine later.
            Debug.Log($"Auto-skipping non-player faction: {candidate.DisplayName} in {aiAutoSkipDelaySeconds:0.##}s.");
            aiAutoSkipCoroutine = StartCoroutine(AutoSkipAiTurn(candidate));
            return;
        }
    }

    private IEnumerator AutoSkipAiTurn(FactionData faction)
    {
        // Even at zero delay, wait one frame to avoid recursive turn advances.
        if (aiAutoSkipDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(aiAutoSkipDelaySeconds);
        else
            yield return null;

        aiAutoSkipCoroutine = null;
        if (ActiveFaction == faction)
            AdvanceTurn();
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
            Instance = null;
    }
}
