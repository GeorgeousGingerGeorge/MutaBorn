using System;
using System.Collections;
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
    public static event Action<UnitController, UnitController> UnitCaptured;

    private static int effectDamagePublishDepth;
    public static bool CanPublishEffectDamage => effectDamagePublishDepth < 4;
    public static bool IsPublishingEffectDamage => effectDamagePublishDepth > 0;

    // Avoid stale subscriptions when Unity's Enter Play Mode uses disabled domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        DamageApplied = null;
        UnitDefeated = null;
        UnitCaptured = null;
        effectDamagePublishDepth = 0;

    }

    private UnitController attacker;
    private ActionDefinition pendingAction;
    private TargetingModeController targetingController;
    private TargetingSystem targetSystem; 
    private FactionManager factionManager;
    private CoverSystem coverSystem;
    private LineOfSight lineOfSight;
    private SoundDetectionSystem soundDetectionSystem;
    private AccuracySystem accuracySystem;
    private int pendingConsumableIndex = -1;
    private bool lastAreaKilledTarget;

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
        targetSystem = FindAnyObjectByType<TargetingSystem>();
        coverSystem = FindAnyObjectByType<CoverSystem>();
        lineOfSight = FindAnyObjectByType<LineOfSight>();
        soundDetectionSystem = FindAnyObjectByType<SoundDetectionSystem>();
        accuracySystem = FindAnyObjectByType<AccuracySystem>();
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
            action.CombatBehavior != CombatActionBehavior.DamageArea &&
            action.CombatBehavior != CombatActionBehavior.HealArea &&
            action.CombatBehavior != CombatActionBehavior.ApplySelfStatus &&
            action.CombatBehavior != CombatActionBehavior.RestoreMana &&
            action.CombatBehavior != CombatActionBehavior.MutationScan &&
            action.CombatBehavior != CombatActionBehavior.MutationDeployCover &&
            action.CombatBehavior != CombatActionBehavior.MutationDeployTrap &&
            action.CombatBehavior != CombatActionBehavior.CaptureUnit)
        {
            Debug.LogWarning($"'{action.DisplayName}' does not have an executable combat behavior yet.", this);

            return false;

        }

        UnitStatusController actionStatuses = source.GetComponent<UnitStatusController>();
        if (actionStatuses != null && !actionStatuses.CanUseAction(action))
        {
            Debug.Log($"{source.data.characterName} cannot use {action.DisplayName} while affected by a disabling status.", source);

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

        if (action.MaxUsesPerTurn > 0 && loadout.GetUsesThisTurn(action) >= action.MaxUsesPerTurn)
        {
            Debug.Log($"{action.DisplayName} has reached its per-turn use limit.", source);

            return false;

        }

        // Capture actions come from consumable slots, never equipped accessories.
        if (action.CombatBehavior == CombatActionBehavior.CaptureUnit)
        {
            return false;

        }

        if (action.CombatBehavior == CombatActionBehavior.MutationScan)
        {
            if (IsTargeting || !MutationTactics.TryScan(source, action))
            {
                return false;

            }

            source.data.ModifyActionPoints(-Mathf.CeilToInt(action.ApCost));
            loadout.RecordSuccessfulUse(action);

            return true;

        }

        if (action.CombatBehavior == CombatActionBehavior.RestoreMana)
        {
            if (IsTargeting || action.ManaRestoreAmount <= 0 ||
                source.data.currentMana >= source.data.maxMana)
            {
                return false;

            }

            source.data.ModifyActionPoints(-Mathf.CeilToInt(action.ApCost));
            source.data.ModifyMana(action.ManaRestoreAmount);
            loadout.RecordSuccessfulUse(action);
            Debug.Log($"{source.data.characterName} used {action.DisplayName}: " +
                $"MP {source.data.currentMana}/{source.data.maxMana}.", source);

            return true;

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

            if (!statuses.ApplyGuard(source, action.GuardDamageReductionPercent,
                action.GuardFlatDamageReduction, action.MutationNegateChancePercent,
                action.MutationGuardOwnerTurns))
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

        // Self status actions are immediate and share the same AP and loadout rules.
        if (action.CombatBehavior == CombatActionBehavior.ApplySelfStatus)
        {
            if (IsTargeting || action.SelfStatus == null)
            {
                Debug.LogWarning("Finish targeting first, or assign a Self Status to this action.", source);

                return false;

            }

            UnitStatusController statuses = source.GetComponent<UnitStatusController>();
            if (statuses == null)
            {
                statuses = source.gameObject.AddComponent<UnitStatusController>();

            }

            if (!statuses.TryApply(action.SelfStatus, source))
            {
                return false;

            }

            source.data.ModifyActionPoints(-Mathf.CeilToInt(action.ApCost));
            loadout.RecordSuccessfulUse(action);
            Debug.Log($"[Status Action] {source.data.characterName} used {action.DisplayName}. " +
                $"AP {source.data.currentActionPoints}/{source.data.maxActionPoints}.", source);

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

        bool isArea = action.CombatBehavior == CombatActionBehavior.DamageArea ||
            action.CombatBehavior == CombatActionBehavior.HealArea ||
            action.CombatBehavior == CombatActionBehavior.MutationDeployCover ||
            action.CombatBehavior == CombatActionBehavior.MutationDeployTrap;
        GridManager grid = isArea ? FindAnyObjectByType<GridManager>() : null;
        if (isArea && (source.currentNode == null || grid == null))
        {
            Debug.LogWarning("Area targeting requires a grid and a unit on a grid tile.", source);

            return false;

        }

        float cellSize = grid != null ? grid.GridCellSize : 1f;
        float targetRange = action.AreaThrowRangeInTiles * cellSize;

        int targetFloorID = action.AreaSameFloorOnly ? source.currentNode.floorID : -1;
        Func<RaycastHit, bool> targetFilter;
        if (isArea)
        {


            targetFilter = hit => hit.collider != null &&
                hit.collider.GetComponentInParent<TileController>() != null;

        }
        else
        {

            targetFilter = hit => IsValidUnitTarget(hit, source, action);
            //targetFilter = hit => hit.collider != null &&
            //    hit.collider.GetComponentInParent<UnitController>() != null;

        }

        if (!targetingController.TryBeginTargeting(
                this, source, action.DisplayName,
                isArea ? action.AreaThrowRangeInTiles * cellSize : action.CombatRange,
                OnTargetPicked, targetFilter,
                OnTargetingCancelled, () => CanCommand(source),
                true, isArea, isArea ? action.AreaRadiusInTiles * cellSize : 0f,
                isArea && action.AreaSameFloorOnly ? source.currentNode.floorID : -1,
                isArea ? OnAreaPointPicked : null))
        {
            return false;

        }

        attacker = source;
        pendingAction = action;
        pendingConsumableIndex = -1;
        return true;

    }

    // Consumables are carried in two mission slots, not granted by ActionSets.
    // Targeting is shared with ordinary actions; the item use is spent only on a successful resolution.
    public bool TryBeginConsumable(UnitController source, int index)
    {
        if (!CanCommand(source) || source.data.carriedConsumables == null ||
            index < 0 || index >= source.data.carriedConsumables.Count)
        {
            return false;

        }

        ConsumableInstance item = source.data.carriedConsumables[index];
        ConsumableData definition = item.definition;
        ActionDefinition action = definition != null ? definition.UseAction : null;
        if (definition == null || action == null || action.IsPassive ||
            action.Category != ActionMenuCategory.Items ||
            (action.CombatBehavior != CombatActionBehavior.DamageUnit &&
             action.CombatBehavior != CombatActionBehavior.DamageArea &&
             action.CombatBehavior != CombatActionBehavior.CaptureUnit) ||
            (action.CombatBehavior == CombatActionBehavior.CaptureUnit &&
             (definition.UseKind != ConsumableUseKind.CaptureUnit ||
              BrigManager.Instance == null || !BrigManager.Instance.HasSpace)) ||
            (!definition.UsesActionPointsOnly && item.remainingUses <= 0) ||
            source.data.currentActionPoints < Mathf.CeilToInt(action.ApCost))
        {
            return false;

        }

        UnitStatusController statuses = source.GetComponent<UnitStatusController>();
        if (statuses != null && !statuses.CanUseAction(action))
        {
            return false;

        }

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
            return false;

        }

        bool isArea = action.CombatBehavior == CombatActionBehavior.DamageArea;
        GridManager grid = isArea ? FindAnyObjectByType<GridManager>() : null;
        if (isArea && (source.currentNode == null || grid == null))
        {
            return false;

        }

        float cellSize = grid != null ? grid.GridCellSize : 1f;
        float targetRange = action.AreaThrowRangeInTiles * cellSize;
        int targetFloorId = action.AreaSameFloorOnly ? source.currentNode.floorID : -1; 
        Func<RaycastHit, bool> filter = isArea
            ? hit => hit.collider != null && hit.collider.GetComponentInParent<TileController>() != null
            : hit => IsValidUnitTarget(hit, source, action);
                                                                                                /* hit => hit.collider != null && hit.collider.GetComponentInParent<UnitController>() != null;*/

        if (!targetingController.TryBeginTargeting(
                this, source, action.DisplayName,
                isArea ? action.AreaThrowRangeInTiles * cellSize : action.CombatRange,
                OnTargetPicked, filter, OnTargetingCancelled, () => CanCommand(source),
                true, isArea, isArea ? action.AreaRadiusInTiles * cellSize : 0f,
                isArea && action.AreaSameFloorOnly ? source.currentNode.floorID : -1,
                isArea ? OnAreaPointPicked : null,
                isArea ? (Vector3 point, int floorId) => IsValidGroundTarget(point,floorId,source,action) : null))
        {
            return false;

        }

        attacker = source;
        pendingAction = action;
        pendingConsumableIndex = index;
        return true;

    }

    private void CompleteConsumableUse(UnitController source, int index, bool killedTarget)
    {
        if (source == null || source.data == null || index < 0 ||
            index >= source.data.carriedConsumables.Count)
        {
            return;

        }

        ConsumableInstance item = source.data.carriedConsumables[index];
        if (item.definition == null)
        {
            return;

        }

        if (!item.definition.UsesActionPointsOnly)
        {
            source.data.TrySpendConsumableUse(index);
            if (killedTarget && item.definition.RefillOnKill)
            {
                source.data.TryRefillConsumableUses(index);

            }

        }

        int remaining = source.data.carriedConsumables[index].remainingUses;
        Debug.Log($"[Consumable] {source.data.characterName} used {item.definition.itemName}: " +
            $"{remaining}/{item.definition.MaxUses} uses remaining.", source);

    }

    private void OnTargetPicked(RaycastHit hit)
    {
        if (hit.collider == null)
        {
            return;

        }

        TryExecuteAgainst(hit.collider.GetComponentInParent<UnitController>());

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

        UnitStatusController targetStatuses = target.GetComponent<UnitStatusController>();
        if (targetStatuses != null && targetStatuses.HasControl(StatusControlFlags.Invisible))
        {
            Debug.Log($"Cannot directly target {target.data.characterName}: Invisible.", target);

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

        UnitController source = attacker;
        int index = pendingConsumableIndex;
        if (pendingAction.MutationRushToTarget &&
            !MutationTactics.TryRushAdjacent(source, target))
        {
            Debug.Log("Charge path is blocked; no AP spent.", source);

            return;

        }

        if (pendingAction.CombatBehavior == CombatActionBehavior.CaptureUnit)
        {
            TryCaptureTarget(source, target, pendingAction);

            return;

        }

        if (ApplyDamage(source, target, pendingAction, true) && index >= 0)
        {
            CompleteConsumableUse(source, index, target.data.currentHealth <= 0);

        }

    }

    private void TryCaptureTarget(UnitController source, UnitController target, ActionDefinition action)
    {
        int index = pendingConsumableIndex;
        if (source == null || source.data == null || index < 0 ||
            index >= source.data.carriedConsumables.Count ||
            BrigManager.Instance == null || !BrigManager.Instance.HasSpace)
        {
            Debug.Log("No unused capture consumable or Brig slot available.", source);
            CancelTargeting();

            return;

        }

        ConsumableInstance item = source.data.carriedConsumables[index];
        if (item.definition == null || item.definition.UseAction != action ||
            item.definition.UseKind != ConsumableUseKind.CaptureUnit || item.remainingUses <= 0)
        {
            CancelTargeting();

            return;

        }

        source.data.ModifyActionPoints(-Mathf.CeilToInt(action.ApCost));
        CompleteConsumableUse(source, index, false);
        float missingHealthPercent = 100f * (1f -
            Mathf.Clamp01((float)target.data.currentHealth / Mathf.Max(1, target.data.maxHealth)));
        float chance = Mathf.Clamp(missingHealthPercent + action.CaptureBonusPercent, 0f, 95f);
        bool succeeded = UnityEngine.Random.Range(0f, 100f) < chance &&
            BrigManager.Instance.TryCapture(target.data);
        CombatFloatingFeedback.ReportCaptureResolved(target, succeeded);
        if (succeeded)
        {
            target.enabled = false;
            StartCoroutine(PlayCaptureBox(source, target));

        }

        Debug.Log($"[Capture] {source.data.characterName} used {item.definition.itemName} on " +
            $"{target.data.characterName}: {chance:0.#}% chance, " +
            (succeeded ? "CAPTURED" : "FAILED") + ". Consumable use spent.", source);
        CancelTargeting();

    }

    private IEnumerator PlayCaptureBox(UnitController source, UnitController target)
    {
        GridManager grid = FindAnyObjectByType<GridManager>();
        float tileSize = grid != null ? grid.GridCellSize : 2f;
        float width = tileSize * 0.9f;
        float height = tileSize * 2.5f;
        Vector3 ground = target.transform.position;
        Vector3 landed = ground + Vector3.up * (height * 0.5f);
        Vector3 start = landed + Vector3.up * (height + tileSize);

        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Capture Box";
        box.transform.localScale = new Vector3(width, height, width);
        box.transform.position = start;
        Collider boxCollider = box.GetComponent<Collider>();
        boxCollider.enabled = false;
        Destroy(boxCollider);

        Renderer boxRenderer = box.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");

        }

        if (shader != null)
        {
            boxRenderer.material = new Material(shader);

        }

        Material material = boxRenderer.material;
        material.color = new Color(0.28f, 0.47f, 0.58f, 1f);

        const float fallDuration = 0.38f;
        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);
            box.transform.position = Vector3.Lerp(start, landed, t * t);
            yield return null;

        }

        box.transform.position = landed;
        if (target != null)
        {
            if (target.occupancy != null && target.currentNode != null)
            {
                target.occupancy.UnregisterUnit(target.currentNode, target);

            }

            target.gameObject.SetActive(false);

        }

        const float shrinkDuration = 0.3f;
        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = 1f - Mathf.SmoothStep(0f, 1f, elapsed / shrinkDuration);
            box.transform.localScale = new Vector3(width, height, width) * scale;
            box.transform.position = ground + Vector3.up * (height * scale * 0.5f);
            yield return null;

        }

        Destroy(material);
        Destroy(box);
        if (target != null)
        {
            UnitCaptured?.Invoke(source, target);

        }

    }

    private void OnAreaPointPicked(Vector3 point, int floorId)
    {
        if (pendingAction != null &&
            (pendingAction.CombatBehavior == CombatActionBehavior.MutationDeployCover ||
             pendingAction.CombatBehavior == CombatActionBehavior.MutationDeployTrap))
        {
            if (attacker != null && CanCommand(attacker) &&
                attacker.data.currentActionPoints >= Mathf.CeilToInt(pendingAction.ApCost) &&
                MutationTactics.TryDeploy(attacker, pendingAction, point, floorId))
            {
                attacker.data.ModifyActionPoints(-Mathf.CeilToInt(pendingAction.ApCost));
                attacker.GetComponent<UnitActionLoadout>()?.RecordSuccessfulUse(pendingAction);
                CancelTargeting();
            
            }

            return;

        }

        TryExecuteArea(point, floorId);

    }

    private void TryExecuteArea(Vector3 impactPoint, int impactFloor)
    {
        if (attacker == null || pendingAction == null || !CanCommand(attacker))
        {
            return;

        }

        UnitController source = attacker;
        ActionDefinition action = pendingAction;
        int index = pendingConsumableIndex;

        if (targetSystem == null) 
        {
            return;
        }

        GridManager grid = FindAnyObjectByType<GridManager>();

        if (grid == null) 
        {
            return;
        }

        float targetRange = action.AreaThrowRangeInTiles * grid.GridCellSize;

        if (action.AreaSameFloorOnly && impactFloor != source.currentNode.floorID) 
        {
            return;
        }

        if (!targetSystem.CanTargetGround(source, impactPoint, impactFloor, targetRange, false)) 
        {
            return;
        }

        if (ResolveAreaAction(source, action, impactPoint, impactFloor))
        {
            // The callback is only reached on successful targeting; area kill-refills
            // are tracked by the resolver rather than guessing from the scene afterwards.
            if (index >= 0)
            {
                CompleteConsumableUse(source, index, lastAreaKilledTarget);

            }

            CancelTargeting();

        }

    }

    // Non-player area attacks and team healing use the same rules as player targeting.
    public bool TryExecuteAiArea(UnitController source, UnitController aimAt, ActionDefinition action)
    {
        TurnManager turns = TurnManager.Instance;
        if (turns == null || turns.IsPlayerTurn || source == null || aimAt == null ||
            action == null || action.IsPassive || source.data == null || aimAt.data == null ||
            source.data.currentHealth <= 0 || aimAt.data.currentHealth <= 0 ||
            !source.isActiveAndEnabled || !aimAt.isActiveAndEnabled ||
            source.currentNode == null || aimAt.currentNode == null ||
            (action.CombatBehavior != CombatActionBehavior.DamageArea &&
            action.CombatBehavior != CombatActionBehavior.HealArea))
        {
            return false;

        }

        UnitFaction sourceFaction = source.GetComponent<UnitFaction>();
        UnitFaction aimFaction = aimAt.GetComponent<UnitFaction>();
        if (sourceFaction == null || aimFaction == null || sourceFaction.Faction == null ||
            aimFaction.Faction == null || !turns.IsFactionTurn(sourceFaction.Faction))
        {
            return false;

        }

        if (action.CombatBehavior == CombatActionBehavior.DamageArea &&
            (factionManager == null ||
            !factionManager.AreHostile(sourceFaction.Faction, aimFaction.Faction)))
        {
            return false;

        }

        if (action.CombatBehavior == CombatActionBehavior.HealArea &&
            sourceFaction.Faction != aimFaction.Faction &&
            (factionManager == null ||
            !factionManager.AreAllied(sourceFaction.Faction, aimFaction.Faction)))
        {
            return false;

        }

        return ResolveAreaAction(source, action, aimAt.transform.position,
            aimAt.currentNode.floorID);

    }

    public bool TryExecuteAiSelfStatus(UnitController source, ActionDefinition action)
    {
        TurnManager turns = TurnManager.Instance;
        if (turns == null || turns.IsPlayerTurn || source == null || source.data == null ||
            action == null || action.IsPassive ||
            action.CombatBehavior != CombatActionBehavior.ApplySelfStatus ||
            action.SelfStatus == null || source.data.currentHealth <= 0)
        {
            return false;

        }

        UnitFaction member = source.GetComponent<UnitFaction>();
        if (member == null || member.Faction == null || !turns.IsFactionTurn(member.Faction) ||
            !HasGrantedAction(source, action) ||
            source.data.currentActionPoints < Mathf.CeilToInt(action.ApCost))
        {
            return false;

        }

        UnitStatusController statuses = source.GetComponent<UnitStatusController>();
        if (statuses != null && (!statuses.CanUseAction(action) ||
            statuses.HasStatus(action.SelfStatus.Id)))
        {
            return false;

        }

        if (statuses == null)
        {
            statuses = source.gameObject.AddComponent<UnitStatusController>();

        }

        if (!statuses.TryApply(action.SelfStatus, source))
        {
            return false;

        }

        source.data.ModifyActionPoints(-Mathf.CeilToInt(action.ApCost));
        source.GetComponent<UnitActionLoadout>()?.RecordSuccessfulUse(action);
        Debug.Log($"[AI Mutation] {source.data.characterName} used {action.DisplayName}.", source);

        return true;

    }

    private bool HasGrantedAction(UnitController source, ActionDefinition action)
    {
        UnitActionLoadout loadout = source.GetComponent<UnitActionLoadout>();
        if (loadout == null)
        {
            return false;

        }

        if (loadout.Version == 0)
        {
            loadout.InitializeForCombat();

        }

        foreach (ActionDefinition granted in loadout.Actions)
        {
            if (granted == action)
            {
                return true;

            }

        }

        return false;

    }

    // Consumables are intentionally carried outside UnitActionLoadout. Allow the
    // currently targeted item only when its exact inventory slot still authorizes it.
    private bool HasAuthorizedAreaAction(UnitController source, ActionDefinition action)
    {
        if (HasGrantedAction(source, action))
        {
            return true;

        }

        if (source == null || source.data == null || action == null ||
            attacker != source || pendingAction != action ||
            pendingConsumableIndex < 0 || source.data.carriedConsumables == null ||
            pendingConsumableIndex >= source.data.carriedConsumables.Count)
        {
            return false;

        }

        ConsumableInstance item = source.data.carriedConsumables[pendingConsumableIndex];
        ConsumableData definition = item.definition;
        return definition != null &&
            definition.UseKind == ConsumableUseKind.TargetedAttack &&
            definition.UseAction == action &&
            action.Category == ActionMenuCategory.Items &&
            action.CombatBehavior == CombatActionBehavior.DamageArea &&
            (definition.UsesActionPointsOnly || item.remainingUses > 0);

    }

    private bool ResolveAreaAction(UnitController source, ActionDefinition action,
        Vector3 impactPoint, int impactFloor)
    {
        if (source == null || source.data == null || source.currentNode == null ||
            action == null || !HasAuthorizedAreaAction(source, action) ||
            (action.CombatBehavior != CombatActionBehavior.DamageArea &&
            action.CombatBehavior != CombatActionBehavior.HealArea))
        {
            return false;

        }

        UnitStatusController statuses = source.GetComponent<UnitStatusController>();
        if (statuses != null && !statuses.CanUseAction(action))
        {
            return false;

        }

        if (action.AreaSameFloorOnly && impactFloor != source.currentNode.floorID)
        {
            Debug.Log("Area target must be on the caster's floor.", this);

            return false;

        }

        GridManager grid = FindAnyObjectByType<GridManager>();
        if (grid == null)
        {
            Debug.LogWarning("Area actions require an active GridManager.", this);

            return false;

        }

        float cellSize = grid.GridCellSize;
        Vector3 throwOffset = impactPoint - source.transform.position;
        throwOffset.y = 0f;
        float throwDistance = action.AreaThrowRangeInTiles * cellSize;
        if (throwOffset.sqrMagnitude > throwDistance * throwDistance + 0.0001f)
        {
            Debug.Log($"Area target is out of range ({action.AreaThrowRangeInTiles} tiles).", source);

            return false;

        }

        int cost = Mathf.CeilToInt(action.ApCost);
        if (source.data.currentActionPoints < cost)
        {
            return false;

        }

        int sourceIndex = FindUniqueEquipmentSourceIndex(source, action);
        source.data.ModifyActionPoints(-cost);
        source.GetComponent<UnitActionLoadout>()?.RecordSuccessfulUse(action);

        bool healing = action.CombatBehavior == CombatActionBehavior.HealArea;
        if (!healing)
        {
            statuses?.Remove("invisible");

        }

        lastAreaKilledTarget = false;
        int totalHealed = 0;
        int totalDamage = 0;
        int affected = 0;
        int eligible = 0;
        float radiusWorld = action.AreaRadiusInTiles * cellSize;
        float radiusSquared = radiusWorld * radiusWorld;
        UnitFaction sourceFaction = source.GetComponent<UnitFaction>();
        UnitController[] units = FindObjectsByType<UnitController>();
        foreach (UnitController target in units)
        {
            if (target == null || !target.isActiveAndEnabled || target.data == null ||
                target.data.currentHealth <= 0 || target.currentNode == null)
            {
                continue;

            }

            if (action.AreaSameFloorOnly && target.currentNode.floorID != impactFloor)
            {
                continue;

            }

            Vector3 delta = target.transform.position - impactPoint;
            delta.y = 0f;
            if (delta.sqrMagnitude > radiusSquared + 0.0001f)
            {
                continue;

            }

            UnitFaction member = target.GetComponent<UnitFaction>();
            if (healing)
            {
                if (sourceFaction == null || sourceFaction.Faction == null || member == null ||
                    member.Faction == null || (sourceFaction.Faction != member.Faction &&
                    (factionManager == null ||
                    !factionManager.AreAllied(sourceFaction.Faction, member.Faction))))
                {
                    continue;

                }

                eligible++;
                int before = target.data.currentHealth;
                target.data.ModifyHealth(action.AreaHealAmount);
                int restored = target.data.currentHealth - before;
                totalHealed += restored;
                affected++;
                Debug.Log($"[AoE Heal] {action.DisplayName} healed {target.data.characterName} " +
                    $"for {restored} HP ({before} -> {target.data.currentHealth}/" +
                    $"{target.data.maxHealth}).", target);

                continue;

            }

            if (!action.AreaFriendlyFire)
            {
                if (sourceFaction == null || member == null || factionManager == null ||
                    !factionManager.AreHostile(sourceFaction.Faction, member.Faction))
                {
                    continue;

                }

            }

            eligible++;
            // AoE cannot be dodged by the direct-attack Evasive roll.
            if (!DamageResolver.TryResolveAction(source, target, action, out DamageResult damage,
                sourceIndex, ignoreDodge: true))
            {
                continue;

            }

            affected++;
            totalDamage += damage.ActualDamage;
            if (damage.HealthBefore > 0 && damage.HealthAfter <= 0)
            {
                lastAreaKilledTarget = true;

            }
            Debug.Log($"[AoE] {action.DisplayName} hit {target.data.characterName}: " +
                $"{damage.ActualDamage} {damage.Request.DamageType} damage, " +
                $"HP {damage.HealthBefore} -> {damage.HealthAfter}/{target.data.maxHealth}.", target);
            FinalizeResolvedDamage(damage, sourceIndex);

        }

        if (healing)
        {
            Debug.Log($"[AoE Heal] {source.data.characterName} used {action.DisplayName} at " +
                $"{impactPoint:F2}, radius {action.AreaRadiusInTiles} tiles: " +
                $"{affected}/{eligible} friendly units healed, {totalHealed} total HP restored. " +
                $"AP {source.data.currentActionPoints}/{source.data.maxActionPoints}.", source);

            return true;

        }

        Debug.Log($"[AoE] {source.data.characterName} used {action.DisplayName} at " +
            $"point {impactPoint:F2} (floor {impactFloor}), " +
            $"radius {action.AreaRadiusInTiles} tiles: {affected}/{eligible} targets hit, " +
            $"{totalDamage} total HP damage. AP {source.data.currentActionPoints}/" +
            $"{source.data.maxActionPoints}.", source);

        return true;

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

        if (lineOfSight == null || !lineOfSight.HasPhysicalLineOfSight(source, target)) 
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

        UnitStatusController aiStatuses = source.GetComponent<UnitStatusController>();
        if (aiStatuses != null && !aiStatuses.CanUseAction(action))
        {
            return false;

        }

        UnitStatusController invisibleTarget = target.GetComponent<UnitStatusController>();
        if (invisibleTarget != null && invisibleTarget.HasControl(StatusControlFlags.Invisible))
        {
            return false;

        }

        int apCost = Mathf.CeilToInt(action.ApCost);
        if (!ownsAction || source.data.currentActionPoints < apCost ||
            Vector3.Distance(source.transform.position, target.transform.position) > action.CombatRange)
        {
            return false;

        }



        return ApplyDamage(source, target, action, false);

    }
    private bool TrySpawnProjectile(UnitController source, UnitController target, ActionDefinition action, Action<Collider, Vector3> onImpact)
    {
        if (source == null || target == null || action == null || action.Projectile == null)
        {
            return false;
        }

        ProjectileDefinition projectile = action.Projectile;

        if (projectile.ProjectilePrefab == null)
        {
            Debug.LogError($"Projectile prefab missing for {action.DisplayName}.");
            return false;
        }

        if (!projectile.ProjectilePrefab.TryGetComponent(out ProjectileController prefabController))
        {
            Debug.LogError($"ProjectileController missing from {projectile.ProjectilePrefab.name}.");
            return false;
        }

        Vector3 spawnPosition = source.transform.position + Vector3.up;

        Collider targetCollider = target.GetComponentInChildren<Collider>();

        Vector3 destination = targetCollider != null ? targetCollider.bounds.center : target.transform.position + Vector3.up;

        GameObject projectileObject = Instantiate(projectile.ProjectilePrefab, spawnPosition, Quaternion.identity);

        ProjectileController controller = projectileObject.GetComponent<ProjectileController>();

        controller.Launch(projectile, destination, onImpact, source.transform);

        return true;
    }

    // TODO: PROJECTILE SYSTEM
    // Projectile spawning, collision, and damage on impact are working.
    // NEXT: Fix turn synchronization so turns cannot advance while projectiles are flying.
    // Verify AP is consumed correctly when firing, even if the projectile misses.
    // Finish testing projectile collisions, then create special and explosive projectile prefabs.
    private bool ApplyDamage(UnitController source, UnitController target, ActionDefinition action, bool clearPlayerTargeting)
    {
        // Actions without projectiles retain their existing combat behavior.
        // Consumable projectile integration will be handled separately.
        if (action == null)
        {
            return false;
        }

        if (action.Projectile == null || (clearPlayerTargeting && pendingConsumableIndex >= 0))
        {
            return ResolveDamage(source, target, action, clearPlayerTargeting);
        }

        if (source == null || target == null || source.data == null || target.data == null)
        {
            return false;
        }

        UnitStatusController statuses = source.GetComponent<UnitStatusController>();

        if (statuses != null && !statuses.CanUseAction(action))
        {
            return false;
        }

        int apCost = Mathf.CeilToInt(action.ApCost);

        if (source.data.currentActionPoints < apCost)
        {
            return false;
        }

        // Launch the projectile and resolve damage only when it strikes the intended target.
        bool launched = TrySpawnProjectile(source, target, action, (hitCollider, impactPoint) =>
        {
            if (hitCollider == null)
            {
                return;
            }

            UnitController hitUnit = hitCollider.GetComponentInParent<UnitController>();

            if (hitUnit != target || target == null || !target.isActiveAndEnabled || target.data == null || target.data.currentHealth <= 0)
            {
                return;
            }

            ResolveDamage(source, target, action, false, true);
        });

        if (!launched)
        {
            return false;
        }

        // Pay for the attack when fired, regardless of whether the projectile hits.
        source.data.ModifyActionPoints(-apCost);
        source.GetComponent<UnitActionLoadout>()?.RecordSuccessfulUse(action);
        statuses?.Remove("invisible");

        if (clearPlayerTargeting)
        {
            CancelTargeting();
        }

        return true;
    }
    private bool ResolveDamage(UnitController source, UnitController target, ActionDefinition action, bool clearPlayerTargeting, bool costAlreadyPaid = false)
    {
        if (source == null || target == null || action == null ||
            source.data == null || target.data == null)
        {
            return false;

        }
        if (accuracySystem == null)
        {
            Debug.Log("[Combat Apply Damage] Accuracy System not assigned", this);
            return false;
        }
        UnitStatusController executionStatuses = source.GetComponent<UnitStatusController>();
        if (executionStatuses != null && !executionStatuses.CanUseAction(action))
        {
            return false;

        }

        // Each hit goes through the full resolver: individual defenses, status procs,
        // damage indicators and kill events. Cost/usage is charged once for the combo.
        int sourceIndex = FindUniqueEquipmentSourceIndex(source, action);
        int apCost = Mathf.CeilToInt(action.ApCost);
        int apBefore = source.data.currentActionPoints;
       
        bool landed = false;
        int hits = action.HitCount;

        for (int hitNumber = 0; hitNumber < hits; hitNumber++)
        {
            if (target == null || target.data == null || target.data.currentHealth <= 0)
            {
                break;

            }
            //Calculate and roll accuracy
            float hitChance = accuracySystem.CalculateHitChance(source, target, action);


            bool hit = accuracySystem.RollToHit(hitChance);
           
            if (!DamageResolver.TryResolveAction(source, target, action,
                out DamageResult damageResult, sourceIndex))
            {
                break;

            }

            if (!landed)
            {
                if (!costAlreadyPaid)
                {
                    source.data.ModifyActionPoints(-apCost);
                    source.GetComponent<UnitActionLoadout>()?.RecordSuccessfulUse(action);
                    source.GetComponent<UnitStatusController>()?.Remove("invisible");
                }

                soundDetectionSystem?.RegisterSound(source.currentNode, SoundType.Scuffle, 12, source);
                source.data.ModifyActionPoints(-apCost);
                source.GetComponent<UnitActionLoadout>()?.RecordSuccessfulUse(action);
                source.GetComponent<UnitStatusController>()?.Remove("invisible");
                landed = true;

                if (action.DamageStat == CombatDamageStat.Melee) // TRIGGER FOR ATTACK ANIMATION in baseCharAnimator
                {
                    Animator animator = source.GetComponentInChildren<Animator>();

                    if (animator != null)
                    {
                        animator.SetTrigger("Attack");
                    }
                }
            }

            if (!hit) 
            {
                Debug.Log($"[Combat] {source.data.characterName} used " +
                    $"{action.DisplayName} (hit {hitNumber + 1}/{hits})" +
                    $"on {target.data.characterName}: MISSED! " +
                    $"Hit Chance: {hitChance:F1}%.");
                continue;
            }

            string critical = damageResult.IsCritical && !damageResult.Evaded ? " CRITICAL!" : "";
            string dodged = damageResult.Evaded ? " DODGED!" : "";
            Debug.Log($"[Combat] {source.data.characterName} used {action.DisplayName} " +
                $"(hit {hitNumber + 1}/{hits}) on {target.data.characterName}: " +
                $"{damageResult.ActualDamage} HP damage{critical}{dodged}. " +
                $"Types: {DescribeDamageTypes(damageResult.Request, target.data)}. " +
                $"HP {damageResult.HealthBefore}->{damageResult.HealthAfter}/{target.data.maxHealth}; " +
                $"AP {apBefore}->{source.data.currentActionPoints}.", source);
            FinalizeResolvedDamage(damageResult, sourceIndex);
            if (damageResult.ActualDamage > 0 && target != null &&
                target.data.currentHealth > 0 && action.MutationKnockbackTiles > 0)
            {
                MutationTactics.TryKnockback(source, target, action.MutationKnockbackTiles);

            }

        }

        if (landed && clearPlayerTargeting)
        {
            CancelTargeting();

        }

        return landed;

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
    private bool IsValidUnitTarget(RaycastHit hit, UnitController source, ActionDefinition action) 
    {
        if (hit.collider == null || targetSystem == null) 
        {
            return false;
        }
        UnitController target = hit.collider.GetComponentInParent<UnitController>();

        if (target == null) 
        {
            return false;
        }

        return targetSystem.CanTargetUnit(source, target, action.CombatRange);

    }
    private bool IsValidGroundTarget(Vector3 point,int floorId,UnitController source,ActionDefinition action)
    {
        if (source == null || action == null || targetSystem == null)
        {
            return false;
        }
        if (action.AreaSameFloorOnly && (source.currentNode == null || floorId != source.currentNode.floorID))
        {
            return false;
        }

        GridManager grid = FindAnyObjectByType<GridManager>();

        if (grid == null)
        { 
            return false; 
        }

        float range = action.AreaThrowRangeInTiles * grid.GridCellSize;

        return targetSystem.CanTargetGround(source,point,floorId,range,false);
    }
    private void OnTargetingCancelled()
    {
        attacker = null;
        pendingAction = null;
        pendingConsumableIndex = -1;

    }

    public void CancelTargeting()
    {
        attacker = null;
        pendingAction = null;
        pendingConsumableIndex = -1;

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
