using System.Collections.Generic;

using UnityEngine;


public class Occupancy : MonoBehaviour
{
    [SerializeField ]private GridManager gridManager;

    [Header("Containers holding occupancy based on the node")]
    private Dictionary<GridNode, UnitController> units = new Dictionary<GridNode, UnitController>();
    private Dictionary<GridNode, ObstacleController> solidObstacles = new Dictionary<GridNode, ObstacleController>();
    private Dictionary<(GridNode, GridNode), ObstacleController> edgeObstacles = new Dictionary<(GridNode,GridNode), ObstacleController>();

    
    public bool CanEnterNode(GridNode currentNode, GridNode destination, UnitController unit = null) 
    {
        if (currentNode == null || destination == null) 
        {
            return false; 
        }

        if (!currentNode.GetNeighbors().Contains(destination)) 
        {
            return false;
        }


        
     
        if (IsBlockedBySolidObstacle(destination))
        {
            return false;
        }

        if (unit == null) 
        {
            if (IsBlockedBySolidObstacle(destination)) 
            {
                return false;
            }

            if (IsUnitInNode(destination))
            {
                return false;
            
            }
        }   
        else if(!IsFootPrintMovable(destination, unit)) 
        {
            return false;

        }
     
        if (IsBlockedByEdgeObstacle(currentNode, destination)) 
        {
            return false;
        }



        return true; 
    
    }

    public bool IsFootPrintMovable(GridNode destination, UnitController unit) 
    {
        if (!TryGetFootprintNodes(destination, unit, out List<GridNode> footprintNodes)) 
        {
            return false;
        }

        foreach (GridNode node in footprintNodes) 
        {
            if(units.TryGetValue(node, out UnitController occupyingUnit) && occupyingUnit != unit) 
            {
                return false;
            }

            if (IsBlockedBySolidObstacle(node)) 
            {
                return false;
            }
        }

        return true; 
    }
    public bool IsBlockedBySolidObstacle(GridNode destination) 
    {
        if (solidObstacles.ContainsKey(destination)) 
        {
            return true;
        }

        return false; 
    }
    public bool IsUnitInNode(GridNode destination) 
    {
        if (units.ContainsKey(destination)) 
        {
            return true;
        }
        return false; 
    }

    public bool IsBlockedByEdgeObstacle(GridNode current, GridNode destination) 
    {
        if (edgeObstacles.ContainsKey((current, destination))) 
        {
            return true;
        }

        if (edgeObstacles.ContainsKey((destination, current)))
        {
            return true;
        }

        return false;
    }

    //Registry-Adding
    public void RegisterUnit(GridNode currentNode, UnitController unit) 
    {

       if(!TryGetFootprintNodes(currentNode, unit , out List<GridNode> footprintNodes))
       {
            return;

       }

        foreach (GridNode node in footprintNodes) 
        {
            if (units.TryGetValue(node, out UnitController exists) && exists != unit) 
            {
                return;
            }

            if (IsBlockedBySolidObstacle(node)) 
            {
                return;
            }
        }

        foreach (GridNode node in footprintNodes) 
        {
            units[node] = unit;
        }

        
        
    }
    public void RegisterSolidObstacles(GridNode currentNode, ObstacleController solidObstacle) 
    {
        if (currentNode == null || solidObstacle == null) 
        {
            return;
        }


        if (solidObstacles.ContainsKey(currentNode)) 
        {
            return; 
        }
        solidObstacles.Add(currentNode, solidObstacle);
    }

    public void RegisterEdgeObstacles(GridNode firstNode, GridNode secondNode, ObstacleController edgeObstacle) 
    {
        if (firstNode == null || secondNode == null || edgeObstacle == null) 
        {
            return;
        }

        edgeObstacles.Add((firstNode, secondNode), edgeObstacle);
    }

    //Registry-Removing
    public void UnregisterUnit(GridNode currentNode, UnitController unit) 
    {
        if (currentNode == null || unit == null)
        {
            return;

        }

        if (!TryGetFootprintNodes(currentNode, unit, out List<GridNode> footprintNodes))
        {
            return;
        }

        foreach (GridNode node in footprintNodes)
        {
            if (units.TryGetValue(node, out UnitController existing) && existing == unit)
            {
                units.Remove(node);
            }
        }
    }
    public void UnregisterSolidObstacles(GridNode currentNode, ObstacleController solidObstacle) 
    {
        if (currentNode == null || solidObstacle == null) 
        {
            return;
        }

        if (solidObstacles.TryGetValue(currentNode, out ObstacleController exists) && exists == solidObstacle)
        {
            solidObstacles.Remove(currentNode);
        }

    }
    public void UnregisterEdgeObstacles(GridNode firstNode, GridNode secondNode, ObstacleController edgeObstacle) 
    {
        if (firstNode == null || secondNode == null  || edgeObstacle == null)
        {
            return;
        }

        if (edgeObstacles.TryGetValue((firstNode,secondNode), out ObstacleController exists) &&  exists == edgeObstacle)
        {
            edgeObstacles.Remove((firstNode, secondNode));
        }
    }
    private bool TryGetFootprintNodes(GridNode anchorNode, UnitController unit, out List<GridNode> footprintNodes)
    {
        footprintNodes = new List<GridNode>();

        if (anchorNode == null || unit == null || gridManager == null)
            return false;

        Vector2Int footprint = unit.footPrint;

        if (footprint.x < 1 || footprint.y < 1)
            return false;

        Dictionary<Vector2Int, GridNode> grid =
            gridManager.GetGrid(anchorNode.floorID);

        if (grid == null)
            return false;

   

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minZ = int.MaxValue;
        int maxZ = int.MinValue;

        foreach (Vector2Int coords in grid.Keys) 
        {
            minX = Mathf.Min(minX, coords.x);
            maxX = Mathf.Max(maxX, coords.x);
            minZ = Mathf.Min(minZ, coords.y);
            maxZ = Mathf.Max(maxZ, coords.y);
        }

        int gridWidth = maxX - minX + 1;
        int gridDepth = maxZ - minZ + 1;

        if (footprint.x > gridWidth || footprint.y > gridDepth)
        {
            return false;
        }

        int anchorX = Mathf.Clamp(anchorNode.coords.x, minX, maxX - footprint.x + 1);
        int anchorZ = Mathf.Clamp(anchorNode.coords.y, minZ, maxZ - footprint.y + 1);


        for (int x = 0; x < footprint.x; x++)
        {
            for (int z = 0; z < footprint.y; z++)
            {
                Vector2Int coords = new Vector2Int(anchorX + x, anchorZ + z);

                if (!grid.TryGetValue(coords, out GridNode node))
                {
                    footprintNodes.Clear();
                    return false;
                }

                footprintNodes.Add(node);
            }
        }

        return true;
    }
    public UnitController FindUnit(GridNode node) 
    {
        if (units.TryGetValue(node, out UnitController unit))
        {
            return unit;

        }

        return null;
    }
    public GridManager ReturnGrid()
    {
        return gridManager;
    }
}
