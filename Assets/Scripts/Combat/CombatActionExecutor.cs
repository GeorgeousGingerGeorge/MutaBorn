using System;
using UnityEngine;

public readonly struct CombatDamageEvent
{
    public UnitController Attacker { get; }
    public UnitController Target { get; }
    public ActionDefinition Action { get; }
    public int ActualDamage { get; }
    public int HealthBefore { get; }
    public int HealthAfter { get; }

    public bool ReducedHealthToZero => HealthBefore > 0 && HealthAfter <= 0;

    public CombatDamageEvent(
        UnitController attacker,
        UnitController target,
        ActionDefinition action,
        int actualDamage,
        int healthBefore,
        int healthAfter)
    {
        Attacker = attacker;
        Target = target;
        Action = action;
        ActualDamage = actualDamage;
        HealthBefore = healthBefore;
        HealthAfter = healthAfter;

    }

}

public sealed class CombatActionExecutor : MonoBehaviour
{
    public static CombatActionExecutor Instance { get; private set; }
    public static bool IsTargeting => Instance != null && Instance.pendingAction != null;

    // One event per successful damage application. Subscribers inspect
    // Attacker/Target to react to damage dealt or received.
    public static event Action<CombatDamageEvent> DamageApplied;

    // Existing event signature preserved for future elimination objectives.
    // Fired once after a unit is removed from active combat.
    public static event Action<UnitController, UnitController> UnitDefeated;

    // Avoid stale subscriptions when Unity's Enter Play Mode uses disabled domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        DamageApplied = null;
        UnitDefeated = null;

    }

    [Header("Unit Inspection")]
    [SerializeField] private UnitInspectionUI unitInspectionUI;

    private UnitController attacker;
    private ActionDefinition pendingAction;
    private TargetingModeController targetingController;
    private FactionManager factionManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one CombatActionExecutor should exist in a scene.", this);
            enabled = false;
            return;

        }

        Instance = this;
        factionManager = FindAnyObjectByType<FactionManager>();

    }

    public bool TryBeginAction(UnitController source, ActionDefinition action)
    {
        if (source == null || source.data == null || action == null || action.IsPassive)
        {
            return false;

        }

        // Executable combat actions are selected by behavior, not by Action ID.
        if (action.CombatBehavior != CombatActionBehavior.DamageUnit &&
            action.CombatBehavior != CombatActionBehavior.ApplyGuard &&
            action.CombatBehavior != CombatActionBehavior.InspectUnit)
        {
            Debug.LogWarning($"'{action.DisplayName}' does not have an executable combat behavior yet.", this);

            return false;

        }

        if (!CanCommand(source))
        {
            return false;

        }

        UnitActionLoadout loadout = source.GetComponent<UnitActionLoadout>();
        bool granted = false;
        if (loadout != null)
        {
            foreach (ActionDefinition available in loadout.Actions)
            {
                if (available != null && string.Equals(available.Id?.Trim(), action.Id?.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    granted = true;
                    break;

                }

            }

        }

        // Inspect Unit is a universal UI command, not a granted combat ability.
        if (!granted && action.CombatBehavior != CombatActionBehavior.InspectUnit)
        {
            Debug.LogWarning("Unit does not possess the requested action.", source);
            return false;

        }

        if (source.data.currentActionPoints < Mathf.CeilToInt(action.ApCost))
        {
            Debug.Log($"Not enough AP to use {action.DisplayName}.", source);

            return false;

        }

        // Defensive self-actions activate immediately; they do not enter targeting mode.
        if (action.CombatBehavior == CombatActionBehavior.ApplyGuard)
        {
            if (IsTargeting)
            {
                Debug.Log("Finish or cancel the current targeting action before guarding.", this);

                return false;

            }

            CombatStatusHandler statuses = CombatStatusHandler.Instance;
            if (statuses == null)
            {
                Debug.LogError("CombatStatusHandler is missing. Add it to the Managers GameObject.", this);

                return false;

            }

            if (!statuses.ApplyGuard(source, action.GuardDamageReductionPercent))
            {
                return false;

            }

            int cost = Mathf.CeilToInt(action.ApCost);
            source.data.ModifyActionPoints(-cost);
            loadout.RecordSuccessfulUse(action);

            Debug.Log($"{source.data.characterName} used {action.DisplayName}. " +
                      $"AP: {source.data.currentActionPoints}/{source.data.maxActionPoints}", this);

            return true;

        }

        // TargetingModeController handles input, range visuals, and cancellation.
        if (targetingController == null)
        {
            targetingController = TargetingModeController.Instance;

        }

        if (targetingController == null)
        {
            targetingController = FindAnyObjectByType<TargetingModeController>();

        }

        if (targetingController == null)
        {
            Debug.LogError("No TargetingModeController in scene. Add one to an always-active GameObject.", this);
            return false;

        }

        if (!targetingController.TryBeginTargeting(
                this, source, action.DisplayName, action.CombatRange,
                OnTargetPicked,
                hit => hit.collider != null && hit.collider.GetComponentInParent<UnitController>() != null,
                OnTargetingCancelled, () => CanCommand(source),
                showRangeIndicator: action.CombatBehavior != CombatActionBehavior.InspectUnit))
        {
            return false;

        }

        attacker = source;
        pendingAction = action;
        return true;

    }

    private void OnTargetPicked(RaycastHit hit)
    {
        if (hit.collider != null)
        {
            TryExecuteAgainst(hit.collider.GetComponentInParent<UnitController>());

        }

    }

    private bool CanCommand(UnitController source)
    {
        TurnManager turns = TurnManager.Instance;
        if (source == null || !source.isActiveAndEnabled || source.data == null ||
            source.data.currentHealth <= 0 || turns == null || !turns.IsPlayerTurn)
        {
            return false;

        }

        UnitFaction member = source.GetComponent<UnitFaction>();
        return member != null && member.Faction != null && turns.IsFactionTurn(member.Faction);

    }

    private void TryExecuteAgainst(UnitController target)
    {
        // Inspection is informational: any active unit can be selected, including allies and self.
        // It must bypass hostile-target and attack-range validation.
        if (pendingAction != null && pendingAction.CombatBehavior == CombatActionBehavior.InspectUnit)
        {
            if (target == null || !target.isActiveAndEnabled || target.data == null)
            {
                Debug.Log("Select an active unit to inspect.", this);

                return;

            }

            if (!CanCommand(attacker))
            {
                CancelTargeting();

                return;

            }

            if (unitInspectionUI == null)
            {
                unitInspectionUI = FindAnyObjectByType<UnitInspectionUI>();

            }

            if (unitInspectionUI == null)
            {
                Debug.LogWarning("CombatActionExecutor: UnitInspectionUI is missing from the scene.", this);
                CancelTargeting();

                return;

            }

            UnitController inspectionSource = attacker;
            ActionDefinition inspectionAction = pendingAction;
            int cost = Mathf.CeilToInt(inspectionAction.ApCost);

            if (inspectionSource.data.currentActionPoints < cost)
            {
                CancelTargeting();

                return;

            }

            // End targeting first so the targeting HUD and world input are released.
            CancelTargeting();
            unitInspectionUI.Open(target);

            if (!unitInspectionUI.IsOpen)
            {
                return;

            }

            if (cost > 0)
            {
                inspectionSource.data.ModifyActionPoints(-cost);

            }

            inspectionSource.GetComponent<UnitActionLoadout>()?.RecordSuccessfulUse(inspectionAction);

            return;

        }

        if (target == null || target == attacker || target.data == null ||
            !target.isActiveAndEnabled || target.data.currentHealth <= 0)
        {
            Debug.Log("Invalid attack target.", this);
            return;

        }

        UnitFaction sourceFaction = attacker.GetComponent<UnitFaction>();
        UnitFaction targetFaction = target.GetComponent<UnitFaction>();
        if (sourceFaction == null || targetFaction == null || factionManager == null ||
            !factionManager.AreHostile(sourceFaction.Faction, targetFaction.Faction))
        {
            Debug.Log($"{pendingAction.DisplayName} requires a hostile target.", this);

            return;

        }

        float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
        if (distance > pendingAction.CombatRange)
        {
            Debug.Log($"Target out of range for {pendingAction.DisplayName} ({distance:0.0} > {pendingAction.CombatRange:0.0}).", this);

            return;

        }

        int apCost = Mathf.CeilToInt(pendingAction.ApCost);
        if (!CanCommand(attacker) || attacker.data.currentActionPoints < apCost)
        {
            CancelTargeting();
            return;

        }

        ApplyDamage(attacker, target, pendingAction, true);

    }

    // Direct entry point for non-player factions: no player selection or targeting HUD.
    public bool TryExecuteAiAttack(UnitController source, UnitController target, ActionDefinition action)
    {
        TurnManager turns = TurnManager.Instance;
        if (turns == null || turns.IsPlayerTurn || source == null || target == null ||
            action == null || action.IsPassive ||
            action.CombatBehavior != CombatActionBehavior.DamageUnit ||
            !source.isActiveAndEnabled || !target.isActiveAndEnabled ||
            source.data == null || target.data == null ||
            source.data.currentHealth <= 0 || target.data.currentHealth <= 0)
        {
            return false;

        }

        UnitFaction sourceFaction = source.GetComponent<UnitFaction>();
        UnitFaction targetFaction = target.GetComponent<UnitFaction>();
        if (sourceFaction == null || targetFaction == null || factionManager == null ||
            !turns.IsFactionTurn(sourceFaction.Faction) ||
            !factionManager.AreHostile(sourceFaction.Faction, targetFaction.Faction))
        {
            return false;

        }

        UnitActionLoadout loadout = source.GetComponent<UnitActionLoadout>();
        bool ownsAction = false;
        if (loadout != null)
        {
            foreach (ActionDefinition available in loadout.Actions)
            {
                if (available == action)
                {
                    ownsAction = true;

                    break;

                }

            }

        }

        int apCost = Mathf.CeilToInt(action.ApCost);
        if (!ownsAction || source.data.currentActionPoints < apCost ||
            Vector3.Distance(source.transform.position, target.transform.position) > action.CombatRange)
        {
            return false;

        }

        return ApplyDamage(source, target, action, false);

    }

    private bool ApplyDamage(UnitController source, UnitController target, ActionDefinition action,
        bool clearPlayerTargeting)
    {
        if (source == null || target == null || action == null ||
            source.data == null || target.data == null)
        {
            return false;

        }

        // Both player and AI attacks use the exact same damage and event pipeline.
        int damage = CalculateDamage(source.data, target.data, action);
        if (CombatStatusHandler.Instance != null)
        {
            damage = CombatStatusHandler.Instance.FilterIncomingDamage(target, damage);

        }

        int apCost = Mathf.CeilToInt(action.ApCost);
        int healthBefore = target.data.currentHealth;

        source.data.ModifyActionPoints(-apCost);
        target.data.ModifyHealth(-damage);
        int actualDamage = Mathf.Max(0, healthBefore - target.data.currentHealth);
        source.GetComponent<UnitActionLoadout>()?.RecordSuccessfulUse(action);

        Debug.Log($"{source.data.characterName} used {action.DisplayName} on {target.data.characterName} " +
                  $"for {actualDamage}. HP: {target.data.currentHealth}/{target.data.maxHealth}; " +
                  $"attacker AP: {source.data.currentActionPoints}/{source.data.maxActionPoints}", this);

        if (clearPlayerTargeting)
        {
            CancelTargeting();

        }

        CombatDamageEvent damageEvent = new CombatDamageEvent(
            source, target, action, actualDamage, healthBefore, target.data.currentHealth);
        PublishDamageApplied(damageEvent);

        // Keep mutation reactions, objective events, and defeat handling identical.
        if (target != null && target.data != null && target.data.currentHealth <= 0)
        {
            if (target.occupancy != null && target.currentNode != null)
            {
                target.occupancy.UnregisterUnit(target.currentNode, target);

            }

            target.gameObject.SetActive(false);
            Debug.Log($"{target.data.characterName} was defeated!", this);
            PublishUnitDefeated(source, target);

        }

        return true;

    }

    private static int CalculateDamage(UnitData sourceData, UnitData targetData, ActionDefinition action)
    {
        int baseDamage;

        switch (action.DamageStat)
        {
            case CombatDamageStat.Ranged:
            {
                baseDamage = sourceData.rangeDamage;

                break;

            }
            case CombatDamageStat.Magic:
            {
                baseDamage = sourceData.magicDamage;

                break;

            }
            default:
            {
                baseDamage = sourceData.meleeDamage;

                break;

            }

        }

        int scaledDamage = Mathf.CeilToInt(Mathf.Max(0, baseDamage) * action.DamageMultiplier);
        int damageBeforeArmor = scaledDamage + action.FlatDamageBonus;
        int damageAfterArmor = damageBeforeArmor - Mathf.Max(0, targetData.armor);

        return Mathf.Max(action.MinimumDamage, damageAfterArmor);

    }

    // Isolate listeners: a broken objective/UI listener should not interrupt combat
    // or prevent another subscriber from receiving its notification.
    private static void PublishDamageApplied(CombatDamageEvent report)
    {
        Action<CombatDamageEvent> callbacks = DamageApplied;
        if (callbacks == null)
        {
            return;

        }

        foreach (Delegate subscriber in callbacks.GetInvocationList())
        {
            try
            {
                ((Action<CombatDamageEvent>)subscriber)(report);

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

            }

        }

    }

    private static void PublishUnitDefeated(UnitController source, UnitController target)
    {
        Action<UnitController, UnitController> callbacks = UnitDefeated;
        if (callbacks == null)
        {
            return;

        }

        foreach (Delegate subscriber in callbacks.GetInvocationList())
        {
            try
            {
                ((Action<UnitController, UnitController>)subscriber)(source, target);

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

            }

        }

    }

    private void OnTargetingCancelled()
    {
        attacker = null;
        pendingAction = null;

    }

    public void CancelTargeting()
    {
        attacker = null;
        pendingAction = null;

        if (targetingController != null)
        {
            targetingController.CompleteTargeting(this);

        }

    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            CancelTargeting();
            Instance = null;

        }

    }

}
