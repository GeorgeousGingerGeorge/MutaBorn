using System.Collections.Generic;
using MutaBorn.Gameplay;
using UnityEngine;

// Shared grid-side implementations for the mutation actions. No campaign state lives here.
public static class MutationTactics
{
    public static bool TryScan(UnitController owner, ActionDefinition action)
    {
        GridManager grid = Object.FindAnyObjectByType<GridManager>();
        if (owner == null || owner.currentNode == null || grid == null)
        {

            return false;

        }

        int found = 0;
        foreach (WorldTrap trap in Object.FindObjectsByType<WorldTrap>())
        {
            if (trap == null || trap.IsRevealed ||
                !InRange(owner.currentNode, grid.GetNodeFromClosestWorldPosition(trap.transform.position),
                    action.AreaRadiusInTiles))
            {

                continue;

            }

            trap.Reveal();
            found++;

        }

        FactionManager factions = Object.FindAnyObjectByType<FactionManager>();
        UnitFaction ownerFaction = owner.GetComponent<UnitFaction>();
        foreach (UnitController target in Object.FindObjectsByType<UnitController>())
        {
            if (target == null || target == owner || target.data == null ||
                target.data.currentHealth <= 0 || !InRange(owner.currentNode, target.currentNode,
                    action.AreaRadiusInTiles))
            {

                continue;

            }

            UnitFaction member = target.GetComponent<UnitFaction>();
            if (factions == null || ownerFaction == null || member == null ||
                !factions.AreHostile(ownerFaction.Faction, member.Faction))
            {

                continue;

            }

            UnitStatusController statuses = target.GetComponent<UnitStatusController>();
            if (statuses != null && statuses.HasControl(StatusControlFlags.Invisible))
            {
                statuses.Remove("invisible");
                found++;

            }

        }

        Debug.Log($"[Mutation Scan] {owner.data.characterName} scanned {action.AreaRadiusInTiles} tiles; revealed {found} threats.", owner);

        return true;

    }

    public static bool TryDeploy(UnitController owner, ActionDefinition action, Vector3 point, int floorId)
    {
        GridManager grid = Object.FindAnyObjectByType<GridManager>();
        Occupancy occupancy = Object.FindAnyObjectByType<Occupancy>();
        if (owner == null || owner.currentNode == null || grid == null || occupancy == null ||
            action.MutationDeployPrefab == null || floorId != owner.currentNode.floorID)
        {

            return false;

        }

        GridNode node = grid.GetNodeFromClosestWorldPosition(point);
        if (node == null || node.floorID != floorId ||
            !TerrainLogic.CheckTerrainType(node.currentTerrain) ||
            !InRange(owner.currentNode, node, action.AreaThrowRangeInTiles) ||
            occupancy.IsUnitInNode(node) || occupancy.IsBlockedBySolidObstacle(node))
        {

            return false;

        }

        foreach (MutationDeployable deployed in Object.FindObjectsByType<MutationDeployable>())
        {
            if (deployed != null && deployed.Node == node)
            {

                return false;

            }

        }

        GameObject spawned = Object.Instantiate(action.MutationDeployPrefab,
            node.cellWorldPosition + Vector3.up * 0.4f, Quaternion.identity);
        spawned.name = action.DisplayName + " (" + owner.data.characterName + ")";
        float size = grid.GridCellSize;
        spawned.transform.localScale = action.CombatBehavior == CombatActionBehavior.MutationDeployCover
            ? new Vector3(size * 0.85f, size * 0.75f, size * 0.85f)
            : new Vector3(size * 0.5f, size * 0.2f, size * 0.5f);

        MutationDeployable lifetime = spawned.GetComponent<MutationDeployable>();
        if (lifetime == null)
        {
            Object.Destroy(spawned);

            return false;

        }

        lifetime.Initialize(owner, node, action.MutationDeployDurationTurns);
        ObstacleController cover = spawned.GetComponent<ObstacleController>();
        if (cover != null)
        {
            cover.InitializeNodes();
            if (!cover.IsBlocking)
            {
                Object.Destroy(spawned);

                return false;

            }

            cover.SetValue_Durability(action.MutationStructureHealth);

        }

        Debug.Log($"[Mutation Deploy] {owner.data.characterName} placed {action.DisplayName} at {node.coords}.", owner);

        return true;

    }

    public static bool TryRushAdjacent(UnitController owner, UnitController target)
    {
        if (owner == null || target == null || owner.currentNode == null ||
            target.currentNode == null || owner.currentNode.floorID != target.currentNode.floorID)
        {

            return false;

        }

        GridManager grid = Object.FindAnyObjectByType<GridManager>();
        Occupancy occupancy = Object.FindAnyObjectByType<Occupancy>();
        if (grid == null || occupancy == null)
        {

            return false;

        }

        GridNode current = owner.currentNode;
        List<GridNode> path = new List<GridNode>();
        for (int i = 0; i < 32 && ChebyshevDistance(current, target.currentNode) > 1; i++)
        {
            Vector2Int delta = target.currentNode.coords - current.coords;
            Vector2Int step = new Vector2Int(System.Math.Sign(delta.x), System.Math.Sign(delta.y));
            GridNode next = grid.GetNodeFromGridPosition(current.floorID, current.coords + step);
            if (next == null || !TerrainLogic.CheckTerrainType(next.currentTerrain) ||
                !occupancy.CanEnterNode(current, next, owner))
            {

                return false;

            }

            path.Add(next);
            current = next;

        }

        if (ChebyshevDistance(current, target.currentNode) > 1)
        {

            return false;

        }

        foreach (GridNode next in path)
        {
            MoveToNode(owner, next);

        }

        if (path.Count > 0)
        {
            Object.FindAnyObjectByType<VisibilityManager>()?.UpdateVisibility();

        }

        return true;

    }

    public static void TryKnockback(UnitController source, UnitController target, int tiles)
    {
        if (source == null || target == null || source.currentNode == null ||
            target.currentNode == null || source.currentNode.floorID != target.currentNode.floorID ||
            target.footPrint != Vector2Int.one)
        {

            return;

        }

        GridManager grid = Object.FindAnyObjectByType<GridManager>();
        Occupancy occupancy = Object.FindAnyObjectByType<Occupancy>();
        if (grid == null || occupancy == null)
        {

            return;

        }

        Vector2Int delta = target.currentNode.coords - source.currentNode.coords;
        Vector2Int direction = new Vector2Int(System.Math.Sign(delta.x), System.Math.Sign(delta.y));
        if (direction == Vector2Int.zero)
        {

            return;

        }

        for (int i = 0; i < tiles; i++)
        {
            GridNode current = target.currentNode;
            GridNode next = grid.GetNodeFromGridPosition(current.floorID, current.coords + direction);
            if (next == null || !TerrainLogic.CheckTerrainType(next.currentTerrain) ||
                !occupancy.CanEnterNode(current, next, target))
            {

                break;

            }

            MoveToNode(target, next);

        }

        Object.FindAnyObjectByType<VisibilityManager>()?.UpdateVisibility();

    }

    public static void TryCounterOnParry(UnitController defender, UnitController attacker)
    {
        if (defender == null || attacker == null || defender.data == null || attacker.data == null ||
            defender.data.currentHealth <= 0 || attacker.data.currentHealth <= 0 ||
            Vector3.Distance(defender.transform.position, attacker.transform.position) > 2.5f)
        {

            return;

        }

        UnitStatusController statuses = defender.GetComponent<UnitStatusController>();
        if (statuses == null ||
            !(statuses.HasStatus("hyperreflexive-parry-1") ||
              statuses.HasStatus("hyperreflexive-parry-2") ||
              statuses.HasStatus("hyperreflexive-parry-3")))
        {

            return;

        }

        bool hasMutation = false;
        foreach (ActiveMutationRecord record in defender.data.activeMutationNodes)
        {
            if (record != null && (record.MutationId == MutationID.HyperreflexiveArc ||
                record.MutationId == MutationID.HyperreflexiveArcII ||
                record.MutationId == MutationID.HyperreflexiveArcIII))
            {
                hasMutation = true;

                break;

            }

        }

        if (!hasMutation)
        {

            return;

        }

        int damage = Mathf.Max(1, Mathf.CeilToInt(defender.data.meleeDamage * 0.5f));
        DamageRequest request = new DamageRequest(defender, attacker, damage, 1f, 0, 1,
            null, DamageType.Blunt, DamageDeliveryType.Direct);
        if (DamageResolver.TryResolve(request, out DamageResult result))
        {
            CombatActionExecutor.PublishResolvedEffectDamage(result);
            Debug.Log($"[Parry] {defender.data.characterName} counterattacked {attacker.data.characterName}.", defender);

        }

    }

    private static void MoveToNode(UnitController unit, GridNode node)
    {
        unit.transform.position = node.cellWorldPosition + Vector3.up * 0.26f;
        unit.SetNode(node);

    }

    private static bool InRange(GridNode source, GridNode target, int tiles)
    {

        return source != null && target != null && source.floorID == target.floorID &&
            ChebyshevDistance(source, target) <= Mathf.Max(0, tiles);

    }

    private static int ChebyshevDistance(GridNode source, GridNode target)
    {

        return Mathf.Max(Mathf.Abs(source.coords.x - target.coords.x),
            Mathf.Abs(source.coords.y - target.coords.y));

    }

}
