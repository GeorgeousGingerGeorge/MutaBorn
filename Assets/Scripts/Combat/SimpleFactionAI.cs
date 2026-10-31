using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Faction AI: recover from initialization races, prioritize usable attacks, and spend remaining AP.
// No extra AI components are needed on individual units.
public sealed class SimpleFactionAI : MonoBehaviour
{
    [Header("References (Auto-found if empty)")]
    [SerializeField] private FactionManager factionManager;
    [SerializeField] private MovementManager movementManager;
    [SerializeField] private CombatActionExecutor combatExecutor;
    [SerializeField] private MissionObjectiveManager missionManager;

    [Header("Pacing")]
    [SerializeField, Min(0.1f)] private float movementSpeed = 6f;
    [SerializeField, Min(0f)] private float delayBetweenUnits = 0.3f;
    [SerializeField] private bool logDecisions = true;

    [Header("AI Decision Limits")]
    [SerializeField, Range(1, 20)] private int maxDecisionsPerUnit = 8;
    [SerializeField, Range(1, 5)] private int maximumRejectedActions = 2;
    [SerializeField, Min(0f)] private float delayBetweenActions = 0.12f;

    private Coroutine currentTurn;

    private void Awake()
    {
        if (factionManager == null)
        {
            factionManager = FindAnyObjectByType<FactionManager>();

        }

        if (movementManager == null)
        {
            movementManager = FindAnyObjectByType<MovementManager>();

        }

        if (combatExecutor == null)
        {
            combatExecutor = FindAnyObjectByType<CombatActionExecutor>();

        }

        if (missionManager == null)
        {
            missionManager = FindAnyObjectByType<MissionObjectiveManager>();

        }

    }

    private void RefreshManagers()
    {
        if (factionManager == null)
        {
            factionManager = FindAnyObjectByType<FactionManager>();

        }

        if (movementManager == null)
        {
            movementManager = FindAnyObjectByType<MovementManager>();

        }

        if (combatExecutor == null)
        {
            combatExecutor = FindAnyObjectByType<CombatActionExecutor>();

        }

        if (missionManager == null)
        {
            missionManager = FindAnyObjectByType<MissionObjectiveManager>();

        }

    }

    public bool TryBeginTurn(FactionData faction)
    {
        if (!isActiveAndEnabled || faction == null || currentTurn != null ||
            TurnManager.Instance == null || !TurnManager.Instance.IsFactionTurn(faction))
        {
            return false;

        }

        // Managers on other scene objects may not be ready when TurnManager.Start runs.
        // Accept the turn now and resolve them after Start has completed.
        RefreshManagers();

        if (logDecisions)
        {
            Debug.Log($"AI: beginning {faction.DisplayName}'s turn.", this);

        }

        currentTurn = StartCoroutine(PlayTurn(faction));

        return true;

    }

    private IEnumerator PlayTurn(FactionData faction)
    {
        // Never finish a turn synchronously inside TurnManager.AdvanceTurn().
        yield return null;
        RefreshManagers();
        if (factionManager == null || movementManager == null || combatExecutor == null)
        {
            Debug.LogError("AI: required managers are still missing; completing turn safely.", this);
            currentTurn = null;
            if (CanContinue(faction))
            {
                TurnManager.Instance.CompleteNonPlayerTurn(faction);

            }

            yield break;

        }

        // UnitController.Start and the delayed action-loadout Start must finish.
        // Give scene-placed objects one frame; late spawned units are checked below.
        int processedUnits = 0;
        UnitFaction[] members = FindObjectsByType<UnitFaction>();
        foreach (UnitFaction member in members)
        {
            if (!CanContinue(faction))
            {
                break;

            }

            if (member == null || !member.isActiveAndEnabled || member.Faction != faction)
            {
                continue;

            }

            UnitController actor = member.GetComponent<UnitController>();
            if (!IsLiving(actor))
            {
                continue;

            }

            processedUnits++;
            yield return PlayUnit(actor, faction);

            if (delayBetweenUnits > 0f && CanContinue(faction))
            {
                yield return new WaitForSeconds(delayBetweenUnits);

            }

        }

        currentTurn = null;
        if (logDecisions)
        {
            Debug.Log($"AI: finished {faction.DisplayName}'s turn ({processedUnits} living units checked).", this);

        }

        if (CanContinue(faction))
        {
            TurnManager.Instance.CompleteNonPlayerTurn(faction);

        }

    }

    private IEnumerator PlayUnit(UnitController actor, FactionData faction)
    {
        UnitActionLoadout loadout = actor.GetComponent<UnitActionLoadout>();
        if (loadout == null)
        {
            Debug.LogWarning($"AI: {actor.name} has no UnitActionLoadout.", actor);
            yield break;

        }

        // Version alone is not proof of success: InitializeForCombat increments it
        // even if UnitData or GameDataBase was unavailable.
        if (loadout.Actions.Count == 0 && actor.data != null)
        {
            loadout.InitializeForCombat();

        }

        if (loadout.Actions.Count == 0)
        {
            Debug.LogWarning($"AI: {actor.name} has no resolved actions after initialization.", actor);

        }

        HashSet<ActionDefinition> rejected = new HashSet<ActionDefinition>();
        HashSet<ActionDefinition> usedSelfActions = new HashSet<ActionDefinition>();
        bool moved = false;
        int failures = 0;

        for (int decision = 0; decision < maxDecisionsPerUnit && CanContinue(faction) && IsLiving(actor); decision++)
        {
            MutaBorn.Gameplay.UnitStatusController statuses = actor.GetComponent<MutaBorn.Gameplay.UnitStatusController>();
            if (statuses != null && statuses.HasControl(MutaBorn.Gameplay.StatusControlFlags.Stun))
            {
                break;

            }

            int apBefore = actor.data.currentActionPoints;
            if (apBefore <= 0)
            {
                break;

            }

            // One support decision may be useful, but it should not consume
            // the rest of the unit's turn or endlessly repeat.
            if (usedSelfActions.Count == 0 && TryUseMutationSupport(actor, faction, statuses))
            {
                usedSelfActions.Add(null);
                if (delayBetweenActions > 0f)
                {
                    yield return new WaitForSeconds(delayBetweenActions);

                }

                continue;

            }

            UnitController enemy = FindBestHostile(actor, faction, loadout, out Dictionary<GridNode, int> stepsToEnemy);
            if (enemy == null)
            {
                if (logDecisions)
                {
                    Debug.Log($"AI: {actor.data.characterName} found no reachable visible hostile.", actor);

                }

                break;

            }

            ActionDefinition best = null;
            float bestScore = float.NegativeInfinity;
            foreach (ActionDefinition action in loadout.Actions)
            {
                if (action == null || action.IsPassive || rejected.Contains(action) ||
                    actor.data.currentActionPoints < Mathf.CeilToInt(action.ApCost) ||
                    (statuses != null && !statuses.CanUseAction(action)))
                {
                    continue;

                }

                bool direct = action.CombatBehavior == CombatActionBehavior.DamageUnit &&
                    GetUsableAttacks(actor, enemy, new List<ActionDefinition> { action }).Count > 0;
                bool area = action.CombatBehavior == CombatActionBehavior.DamageArea &&
                    actor.currentNode != null && enemy.currentNode != null &&
                    actor.currentNode.floorID == enemy.currentNode.floorID &&
                    Vector3.Distance(actor.transform.position, enemy.transform.position) <=
                    action.AreaThrowRangeInTiles * GetGridCellSize();

                if (!direct && !area)
                {
                    continue;

                }

                // Prefer stronger, cheaper attacks; favor finishing injured targets.
                float score = action.DamageMultiplier * 10f + action.FlatDamageBonus +
                    action.HitCount * 2f - action.ApCost * 0.3f + Random.Range(0f, 2f);
                if (area)
                {
                    score += 3f;

                }

                if (score > bestScore)
                {
                    best = action;
                    bestScore = score;

                }

            }

            if (best != null)
            {
                bool success = best.CombatBehavior == CombatActionBehavior.DamageArea
                    ? combatExecutor.TryExecuteAiArea(actor, enemy, best)
                    : combatExecutor.TryExecuteAiAttack(actor, enemy, best);
                if (success)
                {
                    failures = 0;
                    if (logDecisions)
                    {
                        Debug.Log($"AI: {actor.data.characterName} used {best.DisplayName} on {enemy.data.characterName}.", actor);

                    }

                    if (actor.data.currentActionPoints >= apBefore)
                    {
                        // Zero-cost or unusual actions must not loop indefinitely.
                        rejected.Add(best);

                    }

                    if (delayBetweenActions > 0f)
                    {
                        yield return new WaitForSeconds(delayBetweenActions);

                    }

                    continue;

                }

                rejected.Add(best);
                failures++;
                if (logDecisions)
                {
                    Debug.LogWarning($"AI: {actor.data.characterName} had {best.DisplayName} rejected by combat executor.", actor);

                }

                if (failures >= maximumRejectedActions)
                {
                    break;

                }

                continue;

            }

            // Move once, then reassess attacks with remaining AP. This avoids
            // oscillating between destinations in the same unit turn.
            if (moved || (statuses != null && !statuses.CanMoveVoluntarily))
            {
                break;

            }

            List<ActionDefinition> attacks = new List<ActionDefinition>();
            foreach (ActionDefinition action in loadout.Actions)
            {
                if (action != null && !action.IsPassive &&
                    action.CombatBehavior == CombatActionBehavior.DamageUnit)
                {
                    attacks.Add(action);

                }

            }

            if (attacks.Count == 0)
            {
                break;

            }

            GridNode destination = PickDestination(actor, enemy, attacks, stepsToEnemy);
            if (destination == null || destination == actor.currentNode)
            {
                break;

            }

            GridNode origin = actor.currentNode;
            moved = true;
            yield return MoveTo(actor, destination, faction);
            if (actor.currentNode == origin)
            {
                if (logDecisions)
                {
                    Debug.LogWarning($"AI: {actor.data.characterName} could not complete movement.", actor);

                }

                break;

            }

        }

    }

    private float GetGridCellSize()
    {
        GridManager grid = FindAnyObjectByType<GridManager>();
        return grid != null ? grid.GridCellSize : 1f;

    }

    private UnitController FindBestHostile(UnitController actor, FactionData faction,
        UnitActionLoadout loadout, out Dictionary<GridNode, int> stepsToEnemy)
    {
        // Existing graph search respects multi-floor connections and visibility.
        // Prefer targets we can attack immediately, otherwise the nearest reachable one.
        UnitController nearest = FindNearestHostile(actor, faction, out stepsToEnemy);
        if (nearest == null)
        {
            return null;

        }

        if (actor.currentNode == null)
        {
            return nearest;

        }

        foreach (UnitFaction member in FindObjectsByType<UnitFaction>())
        {
            if (member == null || !member.isActiveAndEnabled ||
                !factionManager.AreHostile(faction, member.Faction))
            {
                continue;

            }

            UnitController candidate = member.GetComponent<UnitController>();
            if (!IsLiving(candidate) || candidate.currentNode == null ||
                candidate.currentNode.floorID != actor.currentNode.floorID)
            {
                continue;

            }

            MutaBorn.Gameplay.UnitStatusController status = candidate.GetComponent<MutaBorn.Gameplay.UnitStatusController>();
            if (status != null && status.HasControl(MutaBorn.Gameplay.StatusControlFlags.Invisible))
            {
                continue;

            }

            foreach (ActionDefinition action in loadout.Actions)
            {
                if (action == null || action.IsPassive || action.CombatBehavior != CombatActionBehavior.DamageUnit ||
                    actor.data.currentActionPoints < Mathf.CeilToInt(action.ApCost))
                {
                    continue;

                }

                if (Vector3.Distance(actor.transform.position, candidate.transform.position) <= action.CombatRange)
                {
                    Dictionary<GridNode, int> candidateSteps = BuildStepsToTarget(actor, candidate);
                    if (!candidateSteps.ContainsKey(actor.currentNode))
                    {
                        continue;

                    }

                    stepsToEnemy = candidateSteps;
                    return candidate;

                }

            }

        }

        return nearest;

    }

    private bool TryUseMutationSupport(UnitController actor, FactionData faction,
        MutaBorn.Gameplay.UnitStatusController statuses)
    {
        UnitActionLoadout loadout = actor.GetComponent<UnitActionLoadout>();
        if (loadout == null || actor.currentNode == null)
        {
            return false;

        }

        if (loadout.Version == 0)
        {
            loadout.InitializeForCombat();

        }

        GridManager grid = FindAnyObjectByType<GridManager>();
        float cellSize = grid != null ? grid.GridCellSize : 1f;
        foreach (ActionDefinition ability in loadout.Actions)
        {
            if (ability == null || ability.IsPassive ||
                actor.data.currentActionPoints < Mathf.CeilToInt(ability.ApCost) ||
                (statuses != null && !statuses.CanUseAction(ability)))
            {
                continue;

            }

            if (ability.CombatBehavior == CombatActionBehavior.HealArea)
            {
                UnitController lowest = null;
                int mostMissing = 0;
                foreach (UnitController unit in FindObjectsByType<UnitController>())
                {
                    if (unit == null || unit.data == null || unit.data.currentHealth <= 0 ||
                        unit.currentNode == null || unit.currentNode.floorID != actor.currentNode.floorID)
                    {
                        continue;

                    }

                    UnitFaction member = unit.GetComponent<UnitFaction>();
                    if (member == null || member.Faction == null ||
                        (member.Faction != faction && (factionManager == null ||
                        !factionManager.AreAllied(faction, member.Faction))))
                    {
                        continue;

                    }

                    Vector3 delta = unit.transform.position - actor.transform.position;
                    delta.y = 0f;
                    if (delta.sqrMagnitude >
                        Mathf.Pow(ability.AreaThrowRangeInTiles * cellSize, 2f))
                    {
                        continue;

                    }

                    int missing = unit.data.maxHealth - unit.data.currentHealth;
                    if (missing > mostMissing)
                    {
                        mostMissing = missing;
                        lowest = unit;

                    }

                }

                if (lowest != null && combatExecutor.TryExecuteAiArea(actor, lowest, ability))
                {
                    if (logDecisions)
                    {
                        Debug.Log($"AI: {actor.data.characterName} used {ability.DisplayName} " +
                            $"near {lowest.data.characterName}.", actor);

                    }

                    return true;

                }

            }
            else if (ability.CombatBehavior == CombatActionBehavior.ApplySelfStatus &&
                ability.SelfStatus != null && ability.SelfStatus.Id == "invisible" &&
                combatExecutor.TryExecuteAiSelfStatus(actor, ability))
            {
                if (logDecisions)
                {
                    Debug.Log($"AI: {actor.data.characterName} activated {ability.DisplayName}.", actor);

                }

                return true;

            }

        }

        return false;

    }

    private GridNode PickDestination(UnitController actor, UnitController enemy,
        List<ActionDefinition> attacks, Dictionary<GridNode, int> stepsToEnemy)
    {
        if (actor.currentNode == null || enemy.currentNode == null || stepsToEnemy == null)
        {
            return null;

        }

        float movementEfficiency = actor.data.MovementEfficiency;
        Dictionary<GridNode, float> reachable = movementManager.GetReachableNodes(
            actor.currentNode, actor.data.currentActionPoints * movementEfficiency);
        if (reachable == null)
        {
            return null;

        }

        GridNode bestAttackTile = null;
        GridNode bestApproachTile = null;
        float bestAttackCost = float.PositiveInfinity;
        int currentSteps = stepsToEnemy.TryGetValue(actor.currentNode, out int currentDistance)
            ? currentDistance : int.MaxValue;
        int bestApproachSteps = currentSteps;
        float bestApproachCost = float.PositiveInfinity;

        foreach (KeyValuePair<GridNode, float> entry in reachable)
        {
            GridNode node = entry.Key;
            if (node == null || node == actor.currentNode ||
                !stepsToEnemy.TryGetValue(node, out int steps))
            {
                continue;

            }

            int moveCost = Mathf.CeilToInt(entry.Value / movementEfficiency);
            int remainingAP = actor.data.currentActionPoints - moveCost;
            if (remainingAP < 0)
            {
                continue;

            }

            Vector3 potentialPosition = node.cellWorldPosition + Vector3.up * 0.26f;
            float distance = Vector3.Distance(potentialPosition, enemy.transform.position);
            bool couldAttack = false;

            if (node.floorID == enemy.currentNode.floorID)
            {
                foreach (ActionDefinition action in attacks)
                {
                    if (remainingAP >= Mathf.CeilToInt(action.ApCost) &&
                        distance <= action.CombatRange)
                    {
                        couldAttack = true;

                        break;

                    }

                }

            }

            if (couldAttack && (bestAttackTile == null || moveCost < bestAttackCost))
            {
                bestAttackTile = node;
                bestAttackCost = moveCost;

            }

            // BFS distance follows actual grid connections, including stairs.
            // This lets a unit approach a staircase even when walking away from
            // the target's world position is necessary to reach the next floor.
            if (steps < bestApproachSteps ||
                (steps == bestApproachSteps && entry.Value < bestApproachCost))
            {
                bestApproachTile = node;
                bestApproachSteps = steps;
                bestApproachCost = entry.Value;

            }

        }

        return bestAttackTile != null ? bestAttackTile : bestApproachTile;

    }

    private IEnumerator MoveTo(UnitController actor, GridNode destination, FactionData faction)
    {
        MutaBorn.Gameplay.UnitStatusController statuses = actor.GetComponent<MutaBorn.Gameplay.UnitStatusController>();
        if (statuses != null && !statuses.CanMoveVoluntarily)
        {
            yield break;

        }

        if (actor.currentNode == null || destination == null)
        {
            yield break;

        }

        GridNode origin = actor.currentNode;
        float movementEfficiency = actor.data.MovementEfficiency;
        float movementBudget = actor.data.currentActionPoints * movementEfficiency;
        Dictionary<GridNode, float> reachable = movementManager.GetReachableNodes(origin,
            movementBudget);
        if (reachable == null || !reachable.TryGetValue(destination, out float cost))
        {
            yield break;

        }

        movementManager.SetNodes(origin, destination, movementBudget);
        List<GridNode> path = movementManager.GetPath();
        if (path == null || path.Count < 2 || path[path.Count - 1] != destination)
        {
            yield break;

        }

        // Clone the shared manager's path, because it is used by player movement too.
        List<GridNode> steps = new List<GridNode>(path);
        int totalAPCost = Mathf.CeilToInt(cost / movementEfficiency);
        if (totalAPCost > actor.data.currentActionPoints)
        {
            yield break;

        }

        actor.data.ModifyActionPoints(-totalAPCost);

        foreach (GridNode node in steps)
        {
            if (node == null || node == actor.currentNode)
            {
                continue;

            }

            if (!CanContinue(faction) || !IsLiving(actor))
            {
                yield break;

            }

            if (actor.occupancy == null || !actor.occupancy.CanEnterNode(actor.currentNode, node, actor))
            {
                yield break;

            }

            Vector3 goal = node.cellWorldPosition + Vector3.up * 0.26f;
            while (Vector3.Distance(actor.transform.position, goal) > 0.01f)
            {
                if (!CanContinue(faction) || !IsLiving(actor))
                {
                    yield break;

                }

                actor.transform.position = Vector3.MoveTowards(actor.transform.position, goal,
                    movementSpeed * Time.deltaTime);
                yield return null;

            }

            actor.transform.position = goal;
            actor.SetNode(node);

        }

    }

    private List<ActionDefinition> GetUsableAttacks(UnitController actor, UnitController enemy,
        List<ActionDefinition> actions)
    {
        List<ActionDefinition> available = new List<ActionDefinition>();
        if (actor.currentNode == null || enemy.currentNode == null ||
            actor.currentNode.floorID != enemy.currentNode.floorID)
        {
            return available;

        }

        float distance = Vector3.Distance(actor.transform.position, enemy.transform.position);

        MutaBorn.Gameplay.UnitStatusController statuses = actor.GetComponent<MutaBorn.Gameplay.UnitStatusController>();
        foreach (ActionDefinition action in actions)
        {
            if ((statuses == null || statuses.CanUseAction(action)) &&
                actor.data.currentActionPoints >= Mathf.CeilToInt(action.ApCost) &&
                distance <= action.CombatRange)
            {
                available.Add(action);

            }

        }

        return available;

    }

    private UnitController FindNearestHostile(UnitController actor, FactionData faction,
        out Dictionary<GridNode, int> stepsToEnemy)
    {
        stepsToEnemy = null;
        if (actor.currentNode == null || actor.occupancy == null)
        {
            return null;

        }

        UnitController nearest = null;
        int shortestPath = int.MaxValue;
        float shortestWorldDistance = float.PositiveInfinity;

        foreach (UnitFaction member in FindObjectsByType<UnitFaction>())
        {
            if (member == null || !member.isActiveAndEnabled ||
                !factionManager.AreHostile(faction, member.Faction))
            {
                continue;

            }

            UnitController other = member.GetComponent<UnitController>();
            if (!IsLiving(other) || other == actor || other.currentNode == null)
            {
                continue;

            }

            MutaBorn.Gameplay.UnitStatusController enemyStatuses =
                other.GetComponent<MutaBorn.Gameplay.UnitStatusController>();
            if (enemyStatuses != null &&
                enemyStatuses.HasControl(MutaBorn.Gameplay.StatusControlFlags.Invisible))
            {
                if (logDecisions)
                {
                    Debug.Log($"AI: ignores invisible target {other.data.characterName}.", other);

                }

                continue;

            }

            Dictionary<GridNode, int> distances = BuildStepsToTarget(actor, other);
            if (!distances.TryGetValue(actor.currentNode, out int pathSteps))
            {
                continue;

            }

            float worldDistance = Vector3.Distance(actor.transform.position, other.transform.position);
            if (pathSteps < shortestPath ||
                (pathSteps == shortestPath && worldDistance < shortestWorldDistance))
            {
                shortestPath = pathSteps;
                shortestWorldDistance = worldDistance;
                nearest = other;
                stepsToEnemy = distances;

            }

        }

        return nearest;

    }

    // Traverse the real GridNode neighbor graph from the target, so routes through
    // FloorConnector stairs count as reachable. The target itself is occupied,
    // but the acting unit only needs to approach it, not walk onto its tile.
    private static Dictionary<GridNode, int> BuildStepsToTarget(UnitController actor, UnitController target)
    {
        Dictionary<GridNode, int> steps = new Dictionary<GridNode, int>();
        Queue<GridNode> queue = new Queue<GridNode>();
        GridNode targetNode = target.currentNode;
        steps.Add(targetNode, 0);
        queue.Enqueue(targetNode);

        while (queue.Count > 0)
        {
            GridNode current = queue.Dequeue();
            int nextSteps = steps[current] + 1;

            foreach (GridNode neighbor in current.GetNeighbors())
            {
                if (neighbor == null || steps.ContainsKey(neighbor) ||
                    !TerrainLogic.CheckTerrainType(neighbor.currentTerrain) ||
                    !actor.occupancy.CanEnterNode(current, neighbor, actor))
                {
                    continue;

                }

                steps.Add(neighbor, nextSteps);
                queue.Enqueue(neighbor);

            }

        }

        return steps;

    }

    private bool CanContinue(FactionData faction)
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.IsFactionTurn(faction))
        {
            return false;

        }

        return missionManager == null ||
            (missionManager.Status != MissionStatus.Victory &&
             missionManager.Status != MissionStatus.Defeat);

    }

    private static bool IsLiving(UnitController unit)
    {
        return unit != null && unit.isActiveAndEnabled && unit.data != null &&
            unit.data.currentHealth > 0;

    }

    private void OnDisable()
    {
        if (currentTurn != null)
        {
            StopCoroutine(currentTurn);
            currentTurn = null;

        }

    }

}
