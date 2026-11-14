using System.Collections.Generic;
using UnityEngine;

public sealed class CombatStatusHandler : MonoBehaviour
{
    public static CombatStatusHandler Instance { get; private set; }

    [Header("Optional Playtest Tools")]
    [Tooltip("Assign an enemy here to test damage reduction while enemy AI is not implemented.")]
    [SerializeField] private UnitController debugGuardTarget;
    [SerializeField, Range(0f, 100f)] private float debugGuardReductionPercent = 0f;
    [SerializeField, Min(0)] private int debugGuardFlatReduction = 3;

    private struct GuardProtection
    {
        public readonly float Percent;
        public readonly int Flat;
        public readonly float NegateChance;
        public int RemainingOwnerTurns;

        public GuardProtection(float percent, int flat, float negateChance, int duration)
        {
            Percent = percent;
            Flat = flat;
            NegateChance = negateChance;
            RemainingOwnerTurns = duration;

        }

    }

    private readonly Dictionary<UnitController, GuardProtection> guardReductions = new();
    private readonly List<UnitController> expiredGuards = new();
    private readonly Dictionary<UnitController, GuardProtection> ongoingGuards = new();
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
        return ApplyGuard(owner, damageReductionPercent, 0);

    }

    public bool ApplyGuard(UnitController owner, float damageReductionPercent, int flatReduction)
    {
        return ApplyGuard(owner, damageReductionPercent, flatReduction, 0f, 1);

    }

    public bool ApplyGuard(UnitController owner, float damageReductionPercent, int flatReduction,
        float negateChancePercent, int durationOwnerTurns)
    {
        if (owner == null || !owner.isActiveAndEnabled ||
            owner.data == null || owner.data.currentHealth <= 0)
        {
            return false;

        }

        float percent = Mathf.Clamp(damageReductionPercent, 0f, 100f);
        int flat = Mathf.Max(0, flatReduction);
        guardReductions[owner] = new GuardProtection(percent, flat,
            Mathf.Clamp(negateChancePercent, 0f, 100f), Mathf.Max(1, durationOwnerTurns));

        Debug.Log($"{owner.data.characterName} is Guarding: -{flat} damage per ordinary hit" +
                  (percent > 0f ? $", then {percent:0.#}% reduction" : "") + ".", owner);
        MutaBorn.Gameplay.CombatFloatingFeedback.ReportGuardActivated(owner);

        return true;

    }

    // Deterministic outcome if Guard's random full-negation does not trigger.
    // Only the normal damage path rolls chance and shows floating feedback.
    public bool TryPreviewGuard(UnitController target, int incomingDamage,
        out int reducedDamage, out float negateChance)
    {
        reducedDamage = Mathf.Max(0, incomingDamage);
        negateChance = 0f;
        if (target == null || !guardReductions.TryGetValue(target, out GuardProtection protection))
        {
            return false;

        }

        negateChance = protection.NegateChance;
        if (reducedDamage > 0)
        {
            reducedDamage = Mathf.Max(1, reducedDamage - protection.Flat);
            if (protection.Percent > 0f)
            {
                reducedDamage = Mathf.Max(1, Mathf.CeilToInt(reducedDamage *
                    (1f - protection.Percent / 100f)));

            }

        }

        return true;

    }

    public int FilterIncomingDamage(UnitController target, int incomingDamage)
    {
        int damage = Mathf.Max(0, incomingDamage);
        if (target == null || damage == 0 ||
            !guardReductions.TryGetValue(target, out GuardProtection protection))
        {
            return damage;

        }

        if (protection.NegateChance > 0f && UnityEngine.Random.value * 100f < protection.NegateChance)
        {
            MutaBorn.Gameplay.CombatFloatingFeedback.ReportGuardBlocked(target, damage);

            return 0;

        }

        int reducedDamage = Mathf.Max(1, damage - protection.Flat);
        if (protection.Percent > 0f)
        {
            reducedDamage = Mathf.Max(1, Mathf.CeilToInt(reducedDamage *
                (1f - protection.Percent / 100f)));

        }

        if (reducedDamage < damage)
        {
            MutaBorn.Gameplay.CombatFloatingFeedback.ReportGuardBlocked(target, damage - reducedDamage);

        }

        Debug.Log($"{target.data.characterName}'s Guard reduced incoming ordinary damage from " +
                  $"{damage} to {reducedDamage} (flat {protection.Flat}, " +
                  $"percent {protection.Percent:0.#}).", target);

        return reducedDamage;

    }

    private void OnTurnStarted(FactionData faction)
    {
        expiredGuards.Clear();
        ongoingGuards.Clear();

        foreach (KeyValuePair<UnitController, GuardProtection> active in guardReductions)
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
                GuardProtection protection = active.Value;
                protection.RemainingOwnerTurns--;
                if (protection.RemainingOwnerTurns <= 0)
                {
                    expiredGuards.Add(unit);

                }
                else
                {
                    ongoingGuards[unit] = protection;

                }

            }

        }

        foreach (KeyValuePair<UnitController, GuardProtection> ongoing in ongoingGuards)
        {
            guardReductions[ongoing.Key] = ongoing.Value;

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

        ApplyGuard(debugGuardTarget, debugGuardReductionPercent, debugGuardFlatReduction);

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
