using System.Collections.Generic;
using UnityEngine;

public sealed class CombatStatusHandler : MonoBehaviour
{
    public static CombatStatusHandler Instance { get; private set; }

    [Header("Optional Playtest Tools")]
    [Tooltip("Assign an enemy here to test damage reduction while enemy AI is not implemented.")]
    [SerializeField] private UnitController debugGuardTarget;
    [SerializeField, Range(0f, 100f)] private float debugGuardReductionPercent = 50f;

    private readonly Dictionary<UnitController, float> guardReductions = new();
    private readonly List<UnitController> expiredGuards = new();
    private TurnManager subscribedTurnManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;

    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one CombatStatusHandler should exist in the scene.", this);
            enabled = false;

            return;

        }

        Instance = this;

    }

    private void Start()
    {
        subscribedTurnManager = TurnManager.Instance;
        if (subscribedTurnManager != null)
        {
            subscribedTurnManager.TurnStarted += OnTurnStarted;

        }
        else
        {
            Debug.LogWarning("CombatStatusHandler could not find TurnManager; Guard expiry will not run.", this);

        }

        CombatActionExecutor.UnitDefeated += OnUnitDefeated;

    }

    public bool ApplyGuard(UnitController owner, float damageReductionPercent)
    {
        if (owner == null || !owner.isActiveAndEnabled ||
            owner.data == null || owner.data.currentHealth <= 0)
        {
            return false;

        }

        float reduction = Mathf.Clamp(damageReductionPercent, 0f, 100f);
        guardReductions[owner] = reduction;

        Debug.Log($"{owner.data.characterName} is Guarding: {reduction:0.#}% incoming damage reduction.", owner);

        return true;

    }

    public int FilterIncomingDamage(UnitController target, int incomingDamage)
    {
        int damage = Mathf.Max(0, incomingDamage);
        if (target == null || damage == 0 ||
            !guardReductions.TryGetValue(target, out float reduction))
        {
            return damage;

        }

        int reducedDamage = Mathf.CeilToInt(damage * (1f - reduction / 100f));
        reducedDamage = Mathf.Max(0, reducedDamage);

        Debug.Log($"{target.data.characterName}'s Guard reduced incoming damage from " +
                  $"{damage} to {reducedDamage}.", target);

        return reducedDamage;

    }

    private void OnTurnStarted(FactionData faction)
    {
        expiredGuards.Clear();

        foreach (KeyValuePair<UnitController, float> active in guardReductions)
        {
            UnitController unit = active.Key;
            if (unit == null || !unit.isActiveAndEnabled ||
                unit.data == null || unit.data.currentHealth <= 0)
            {
                expiredGuards.Add(unit);

                continue;

            }

            UnitFaction member = unit.GetComponent<UnitFaction>();
            if (member != null && member.Faction == faction)
            {
                expiredGuards.Add(unit);

            }

        }

        foreach (UnitController unit in expiredGuards)
        {
            if (unit != null)
            {
                Debug.Log($"{unit.data.characterName}'s Guard expired at the start of its faction's turn.", unit);

            }

            guardReductions.Remove(unit);

        }

        expiredGuards.Clear();

    }

    private void OnUnitDefeated(UnitController attacker, UnitController target)
    {
        if (target != null)
        {
            guardReductions.Remove(target);

        }

    }

    [ContextMenu("Test / Apply Guard to Debug Target")]
    private void DebugApplyGuardToTarget()
    {
        if (debugGuardTarget == null)
        {
            Debug.LogWarning("Assign Debug Guard Target in the Inspector first.", this);

            return;

        }

        ApplyGuard(debugGuardTarget, debugGuardReductionPercent);

    }

    private void OnDestroy()
    {
        if (subscribedTurnManager != null)
        {
            subscribedTurnManager.TurnStarted -= OnTurnStarted;

        }

        CombatActionExecutor.UnitDefeated -= OnUnitDefeated;

        if (Instance == this)
        {
            Instance = null;

        }

    }

}
