using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Prototype opponent controller: nearest hostile target, move into range, one random attack.
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

    public bool TryBeginTurn(FactionData faction)
    {
        if (!isActiveAndEnabled || faction == null || currentTurn != null ||
            TurnManager.Instance == null || !TurnManager.Instance.IsFactionTurn(faction))
        {
            return false;

        }

        if (factionManager == null || movementManager == null || combatExecutor == null)
        {
            Debug.LogWarning("SimpleFactionAI: required managers are missing; falling back to turn auto-skip.", this);

            return false;

        }

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
        UnitController enemy = FindNearestHostile(actor, faction, out Dictionary<GridNode, int> stepsToEnemy);
        if (enemy == null)
        {
            if (logDecisions)
            {
                Debug.Log($"AI: {actor.data.characterName} has no reachable hostile unit (check faction hostility, floor connectors, and obstacles).", this);

            }

            yield break;

        }

        UnitActionLoadout loadout = actor.GetComponent<UnitActionLoadout>();
        if (loadout == null)
        {
            Debug.LogWarning($"AI: {actor.data.characterName} has no UnitActionLoadout.", actor);

            yield break;

        }

        if (loadout.Version == 0)
        {
            loadout.InitializeForCombat();

        }

        // Get actual granted abilities, not a hardcoded attack list.
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
            Debug.LogWarning($"AI: {actor.data.characterName} has no granted DamageUnit actions.", actor);

            yield break;

        }

        List<ActionDefinition> options = GetUsableAttacks(actor, enemy, attacks);
        if (options.Count == 0)
        {
            GridNode destination = PickDestination(actor, enemy, attacks, stepsToEnemy);
            if (destination != null && destination != actor.currentNode)
            {
                if (logDecisions)
                {
                    Debug.Log($"AI: {actor.data.characterName} approaching {enemy.data.characterName} via floor {destination.floorID}.", this);

                }

                yield return MoveTo(actor, destination, faction);

            }
            else if (logDecisions)
            {
                Debug.Log($"AI: {actor.data.characterName} found {enemy.data.characterName}, but has no reachable movement tile with remaining AP.", this);

            }

        }

        if (!CanContinue(faction) || !IsLiving(actor) || !IsLiving(enemy))
        {
            yield break;

        }

        options = GetUsableAttacks(actor, enemy, attacks);
        if (options.Count == 0)
        {
            if (logDecisions)
            {
                Debug.Log($"AI: {actor.data.characterName} approached {enemy.data.characterName} but cannot attack yet.", this);

            }

            yield break;

        }

        ActionDefinition chosen = options[Random.Range(0, options.Count)];
        bool success = combatExecutor.TryExecuteAiAttack(actor, enemy, chosen);
        if (logDecisions)
        {
            Debug.Log(success
                ? $"AI: {actor.data.characterName} used {chosen.DisplayName} on {enemy.data.characterName}."
                : $"AI: {actor.data.characterName} attempted {chosen.DisplayName} on {enemy.data.characterName}, but CombatActionExecutor rejected it.", this);

        }

    }

    private GridNode PickDestination(UnitController actor, UnitController enemy,
        List<ActionDefinition> attacks, Dictionary<GridNode, int> stepsToEnemy)
    {
        if (actor.currentNode == null || enemy.currentNode == null || stepsToEnemy == null)
        {
            return null;

        }

        Dictionary<GridNode, float> reachable = movementManager.GetReachableNodes(
            actor.currentNode, actor.data.currentActionPoints);
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

            int moveCost = Mathf.CeilToInt(entry.Value);
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
        if (actor.currentNode == null || destination == null)
        {
            yield break;

        }

        GridNode origin = actor.currentNode;
        Dictionary<GridNode, float> reachable = movementManager.GetReachableNodes(origin,
            actor.data.currentActionPoints);
        if (reachable == null || !reachable.TryGetValue(destination, out float cost))
        {
            yield break;

        }

        movementManager.SetNodes(origin, destination, actor.data.currentActionPoints);
        List<GridNode> path = movementManager.GetPath();
        if (path == null || path.Count < 2 || path[path.Count - 1] != destination)
        {
            yield break;

        }

        // Clone the shared manager's path, because it is used by player movement too.
        List<GridNode> steps = new List<GridNode>(path);
        int totalAPCost = Mathf.CeilToInt(cost);
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

        foreach (ActionDefinition action in actions)
        {
            if (actor.data.currentActionPoints >= Mathf.CeilToInt(action.ApCost) &&
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
