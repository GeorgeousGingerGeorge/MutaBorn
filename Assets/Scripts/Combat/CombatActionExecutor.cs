using System;
using MutaBorn.Gameplay;
using UnityEngine;

public readonly struct CombatDamageEvent
{
    public UnitController Attacker { get; }
    public UnitController Target { get; }
    public ActionDefinition Action { get; }
    public int ActualDamage { get; }
    public int HealthBefore { get; }
    public int HealthAfter { get; }
    public int SourceEquipmentIndex { get; }
    public DamageType DamageType { get; }
    public System.Collections.Generic.IReadOnlyList<DamageTypeAmount> DamageByType { get; }

    public bool ReducedHealthToZero => HealthBefore > 0 && HealthAfter <= 0;

    public CombatDamageEvent(
        UnitController attacker,
        UnitController target,
        ActionDefinition action,
        int actualDamage,
        int healthBefore,
        int healthAfter,
        int sourceEquipmentIndex = -1,
        DamageType damageType = DamageType.Unspecified,
        System.Collections.Generic.IReadOnlyList<DamageTypeAmount> damageByType = null)
    {
        Attacker = attacker;
        Target = target;
        Action = action;
        ActualDamage = actualDamage;
        HealthBefore = healthBefore;
        HealthAfter = healthAfter;
        SourceEquipmentIndex = sourceEquipmentIndex;
        DamageType = damageType;
        DamageByType = damageByType ?? Array.Empty<DamageTypeAmount>();

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

    private static int effectDamagePublishDepth;
    public static bool CanPublishEffectDamage => effectDamagePublishDepth < 4;

    // Avoid stale subscriptions when Unity's Enter Play Mode uses disabled domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        DamageApplied = null;
        UnitDefeated = null;
        effectDamagePublishDepth = 0;

    }

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
            action.CombatBehavior != CombatActionBehavior.ApplyGuard)
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

        if (!granted)
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
                OnTargetingCancelled, () => CanCommand(source)))
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

        // The resolver owns calculations, Guard, and actual health changes.
        // Player and AI attacks share this path; action/event handling stays here.
        int sourceIndex = FindUniqueEquipmentSourceIndex(source, action);
        if (!DamageResolver.TryResolveAction(source, target, action, out DamageResult damageResult, sourceIndex))
        {
            return false;

        }

        int apCost = Mathf.CeilToInt(action.ApCost);
        int apBefore = source.data.currentActionPoints;
        source.data.ModifyActionPoints(-apCost);
        source.GetComponent<UnitActionLoadout>()?.RecordSuccessfulUse(action);

        string damageTypes = DescribeDamageTypes(damageResult.Request, target.data);
        Debug.Log($"[Combat] {source.data.characterName} used {action.DisplayName} on " +
                  $"{target.data.characterName} for {damageResult.ActualDamage} damage.\n" +
                  $"Types: {damageTypes} | Armor: {target.data.armor} | Ward: {target.data.ward}\n" +
                  $"Damage: {damageResult.DamageBeforeArmor} raw -> " +
                  $"{damageResult.DamageAfterArmor} after Armor/Ward -> " +
                  $"{damageResult.DamageAfterResistances} after resistance/minimum -> " +
                  $"{damageResult.DamageAfterGuard} after Guard -> " +
                  $"{damageResult.ActualDamage} actual\n" +
                  $"HP: {damageResult.HealthBefore} -> {damageResult.HealthAfter}/{target.data.maxHealth} | " +
                  $"Attacker AP: {apBefore} -> {source.data.currentActionPoints}/{source.data.maxActionPoints}", this);

        if (clearPlayerTargeting)
        {
            CancelTargeting();

        }

        FinalizeResolvedDamage(damageResult, sourceIndex);

        return true;

    }

    // Status ticks and direct effect damage use the same damage events,
    // objectives and defeat cleanup as ordinary attacks, without spending AP.
    public static void PublishResolvedEffectDamage(DamageResult result)
    {
        if (!CanPublishEffectDamage)
        {
            Debug.LogWarning("Effect damage reaction limit reached; nested reactions suppressed.");

            return;

        }

        effectDamagePublishDepth++;
        try
        {
            FinalizeResolvedDamage(result, -1);

        }
        finally
        {
            effectDamagePublishDepth--;

        }

    }

    private static void FinalizeResolvedDamage(DamageResult result, int sourceIndex)
    {
        UnitController target = result.Request.Target;
        if (target == null || target.data == null)
        {
            return;

        }

        if (result.ActualDamage > 0)
        {
            PublishDamageApplied(new CombatDamageEvent(result.Request.Attacker, target,
                result.Request.Action, result.ActualDamage, result.HealthBefore,
                result.HealthAfter, sourceIndex, result.Request.DamageType,
                result.DamageByType));

        }

        if (result.ReducedHealthToZero && target.gameObject.activeSelf)
        {
            if (target.occupancy != null && target.currentNode != null)
            {
                target.occupancy.UnregisterUnit(target.currentNode, target);

            }

            target.gameObject.SetActive(false);
            Debug.Log($"{target.data.characterName} was defeated!", target);
            PublishUnitDefeated(result.Request.Attacker, target);

        }

    }

    // Action ownership is the only current attribution signal. If two items
    // grant the same action, there is no selected weapon in combat yet, so
    // fail closed rather than credit both weapons for the same hit.
    private static int FindUniqueEquipmentSourceIndex(UnitController attacker, ActionDefinition action)
    {
        if (attacker == null || attacker.data == null || action == null ||
            attacker.data.equippedItems == null)
        {
            return -1;

        }

        int sourceIndex = -1;
        for (int i = 0; i < attacker.data.equippedItems.Count; i++)
        {
            EquipmentInstance equipment = attacker.data.equippedItems[i];
            bool hasAction = equipment.grantedActionSet != null &&
                equipment.grantedActionSet.ContainsAction(action.Id);
            if (!hasAction && equipment.grantedActions != null)
            {
                foreach (ActionDefinition granted in equipment.grantedActions)
                {
                    if (granted != null && string.Equals(granted.Id, action.Id,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        hasAction = true;

                        break;

                    }

                }

            }

            if (!hasAction)
            {
                continue;

            }

            if (sourceIndex >= 0)
            {
                return -1;

            }

            sourceIndex = i;

        }

        return sourceIndex;

    }

    private static string DescribeDamageTypes(DamageRequest request, UnitData target)
    {
        System.Collections.Generic.List<DamageType> types = new System.Collections.Generic.List<DamageType>();
        types.Add(request.DamageType);

        if (request.AdditionalComponents != null)
        {
            foreach (DamageComponent component in request.AdditionalComponents)
            {
                if (component.Amount > 0 && !types.Contains(component.Type))
                {
                    types.Add(component.Type);

                }

            }

        }

        System.Collections.Generic.List<string> descriptions = new System.Collections.Generic.List<string>();
        foreach (DamageType type in types)
        {
            if (type == DamageType.True)
            {
                descriptions.Add("True (ignores Armor, Ward, resistance, Guard)");

                continue;

            }

            float resistance = target.GetDamageResistancePercent(type);
            descriptions.Add($"{type} ({resistance:+0.##;-0.##;0}% resist)");

        }

        return string.Join(" + ", descriptions);

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
