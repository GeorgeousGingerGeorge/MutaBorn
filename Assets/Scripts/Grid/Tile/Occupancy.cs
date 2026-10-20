using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.Collections.Tests.CoreCLR.TestJobs;

public class Occupancy : MonoBehaviour
{
    [Header("Containers holding occupancy based on the node")]
    private Dictionary<GridNode, UnitController> units = new Dictionary<GridNode, UnitController>();
    private Dictionary<GridNode, ObstacleController> solidObstacles = new Dictionary<GridNode, ObstacleController>();
    private Dictionary<(GridNode, GridNode), ObstacleController> edgeObstacles = new Dictionary<(GridNode,GridNode), ObstacleController>();

    public bool CanEnterNode(GridNode currentNode, GridNode destination) 
    {
        if (currentNode == null || destination == null) 
        {
            return false; 
        }

        if (!currentNode.GetNeighbors().Contains(destination)) 
        {
            return false;
        }

        if (units.ContainsKey(destination)) 
        {
            return false; 
        }

        if (IsBlockedBySolidObstacle(destination)) 
        {
            return false; 
        }
        if (IsUnitInNode(destination)) 
        {
            return false;
        }
        if (IsBlockedByEdgeObstacle(currentNode, destination)) 
        {
            return false;
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

        return false;
    }

    //Registry-Adding
    public void RegisterUnit(GridNode currentNode, UnitController unit) 
    {
        if (currentNode == null || unit == null) 
        {
            return; 
        }
        

        units.Add(currentNode, unit);
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


        if (units.TryGetValue(currentNode, out UnitController exists) && exists == unit) 
        {
            units.Remove(currentNode);
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


}
