using System.Collections.Generic;
using UnityEngine;

// Shared placement rules for roster crew and mission-owned faction units.
// Doesn't create UnitData, register combat units, or own mission state.
internal static class MissionSpawnAreaTiles
{
    internal static GridNode FindNearestNode(Dictionary<Vector2Int, GridNode> nodes, Vector3 location)
    {
        GridNode closest = null;
        float best = float.MaxValue;
        foreach (GridNode node in nodes.Values)
        {
            float distance = (node.cellWorldPosition - location).sqrMagnitude;
            if (distance < best)
            {
                best = distance;
                closest = node;

            }

        }

        return closest;

    }

    internal static List<GridNode> ShuffledCandidates(Dictionary<Vector2Int, GridNode> floor,
        Vector2Int anchor, int width, int depth)
    {
        List<GridNode> result = new List<GridNode>();
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (floor.TryGetValue(anchor + new Vector2Int(x, z), out GridNode node))
                {
                    result.Add(node);

                }

            }

        }

        for (int i = result.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            GridNode temp = result[i];
            result[i] = result[j];
            result[j] = temp;

        }

        return result;

    }

    internal static bool FitsEntirelyInsideArea(GridNode node, Vector2Int footprint,
        Vector2Int start, int width, int depth, Dictionary<Vector2Int, GridNode> floor)
    {
        if (footprint.x < 1 || footprint.y < 1 || node.coords.x < start.x || node.coords.y < start.y ||
            node.coords.x + footprint.x > start.x + width || node.coords.y + footprint.y > start.y + depth)
        {
            return false;

        }

        for (int x = 0; x < footprint.x; x++)
        {
            for (int z = 0; z < footprint.y; z++)
            {
                if (!floor.ContainsKey(node.coords + new Vector2Int(x, z)))
                {
                    return false;

                }

            }

        }

        return true;

    }
   
    internal static bool IsFootprintFree(GridNode anchor, Vector2Int footprint, HashSet<Vector2Int> reserved)
    {
        for (int x = 0; x < footprint.x; x++)
        {
            for (int z = 0; z < footprint.y; z++)
            {
                if (reserved.Contains(anchor.coords + new Vector2Int(x, z)))
                {
                    return false;

                }

            }

        }

        return true;

    }

    internal static void ReserveFootprint(GridNode anchor, Vector2Int footprint, HashSet<Vector2Int> reserved)
    {
        for (int x = 0; x < footprint.x; x++)
        {
            for (int z = 0; z < footprint.y; z++)
            {
                reserved.Add(anchor.coords + new Vector2Int(x, z));

            }

        }

    }

    // Scene units may not have registered into Occupancy yet; reserve their
    // starting footprints so we don't accidentally spawn on top of them.
    internal static void ReserveSceneUnits(Dictionary<Vector2Int, GridNode> floor,
        GridManager gridManager, HashSet<Vector2Int> reserved)
    {
        UnitController[] units = Object.FindObjectsByType<UnitController>();
        foreach (UnitController unit in units)
        {
            if (unit == null || !unit.gameObject.activeInHierarchy)
            {
                continue;

            }

            GridNode anchor = FindNearestNode(floor, unit.transform.position);
            if (anchor != null &&
                Mathf.Abs(anchor.cellWorldPosition.y - unit.transform.position.y) <= gridManager.GridCellSize)
            {
                ReserveFootprint(anchor, unit.footPrint, reserved);

            }

        }

    }

}
