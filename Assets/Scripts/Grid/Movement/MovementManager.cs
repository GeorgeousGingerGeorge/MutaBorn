using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using UnityEngine;


public class MovementManager : MonoBehaviour
{
    [Header("Necessary Information Game Information")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Occupancy occupancy;
    public Occupancy readOccupancy => occupancy;

    [Header("Beginning Node and Destination Node")]
    public GridNode startNode { get; private set; }
    public GridNode targetNode { get; private set; }
    public float movementCost { get; private set; }

    [Header("Holds the Path to the Destination")]
    private List<GridNode> route = new List<GridNode>();
    private Dictionary<Vector2Int, GridNode> grid = new Dictionary<Vector2Int, GridNode>();

    
    public void SetNodes(GridNode start, GridNode destination, float movementAllowed) 
    {
        movementCost = 0f;
        if (gridManager == null)
            return;

        startNode = start;
        if (startNode == null)
            return;

        targetNode = destination;
        if (targetNode == null)
            return;

       
        grid = gridManager.GetGrid(startNode.floorID);
        
        if(grid == null) 
        {
            return;
        }

        route = FindPath(startNode, targetNode, movementAllowed);
        if (route == null) 
        {
            return; 
        }
    }

    private List<GridNode> FindPath(GridNode startNode, GridNode targetNode, float movementAllowed) 
    {
        
        Dictionary<GridNode, float> distance = new Dictionary<GridNode, float>();
        Dictionary<GridNode, GridNode> previousNode = new Dictionary<GridNode, GridNode>();


        List<GridNode> unvisited = new List<GridNode>();
        List<GridNode> visited = new List<GridNode>();

        distance[startNode] = 0;
        unvisited.Add(startNode);

        while (unvisited.Count > 0)
        {
            int front = 0;
            GridNode current = unvisited[front];

            foreach (GridNode node in unvisited)
            {
                if (distance[node] < distance[current])
                {
                    current = node;
                }
            }

            unvisited.Remove(current);

            if (visited.Contains(current))
                continue;

            float currentCost = distance[current];

            if (currentCost > movementAllowed)
                break;

            visited.Add(current);


            if (current == targetNode)
            {
                movementCost = currentCost; 
                return ReconstructedPath(previousNode, startNode, targetNode);
            }

            foreach (GridNode neighbor in current.GetNeighbors())
            {
                if(neighbor == null || visited.Contains(neighbor))
                    continue;

                GridNode nextNode = neighbor;
               
                if (occupancy == null) 
                {
                    return null;
                }
                if (nextNode.floorID != current.floorID)
                {
                    if (!TryGetNextNode(current, nextNode, occupancy.FindUnit(startNode), out nextNode)) 
                    {
                        continue;
                    }
                }
                else
                {
                    if (!TerrainLogic.CheckTerrainType(neighbor.currentTerrain))
                    {
                        continue;
                    }
                    if (!occupancy.CanEnterNode(current, nextNode, occupancy.FindUnit(startNode)))
                    {
                        continue;
                    }
                }

                float newCost = distance[current] + GetMovementCost(nextNode, current);

                if (newCost > movementAllowed)
                    continue;

                if (!distance.TryGetValue(nextNode, out float oldCost) || newCost < oldCost) 
                {
                    distance[nextNode] = newCost;
                    previousNode[nextNode] = current;

                    if (!unvisited.Contains(nextNode)) 
                    {
                        unvisited.Add(nextNode);
                    }
                }
            }
        }

       
            return null;
    }
    public Dictionary<GridNode, float> GetReachableNodes(GridNode startNode, float movementAllowed) 
    {
        Dictionary<GridNode, float> distances = new Dictionary<GridNode, float>();

        List<GridNode> unvisited = new List<GridNode>();
        List<GridNode> visited = new List<GridNode>();

        if (startNode == null || movementAllowed < 0f) 
        {
            return distances;
        }

        distances[startNode] = 0f;
        unvisited.Add(startNode);

        while (unvisited.Count > 0) 
        {
            int front = 0; 
            GridNode current = unvisited[front];

            foreach (GridNode node in unvisited) 
            {
                if (distances[node] < distances[current]) 
                {
                    current = node;
                }
            }

            unvisited.Remove(current);

            if (visited.Contains(current))
                continue;

            

            float currentCost = distances[current];

            if (currentCost > movementAllowed)
                break;

            visited.Add(current);

            foreach (GridNode neighbor in current.GetNeighbors()) 
            {
                if (neighbor == null || visited.Contains(neighbor)) 
                    continue;

                GridNode nextNode = neighbor;
                if (occupancy == null)
                {
                    return null;
                }

                if (nextNode.floorID != current.floorID)
                {
                    if(!TryGetNextNode(current, nextNode, occupancy.FindUnit(startNode), out nextNode)) 
                    {
                        continue;
                    }
                }
                else 
                {
                    if (!TerrainLogic.CheckTerrainType(nextNode.currentTerrain))
                    {
                        continue;
                    }
                    if (!occupancy.CanEnterNode(current, nextNode, occupancy.FindUnit(startNode)))
                    {
                        continue;
                    }
                }
                

                float stepCost = GetMovementCost(nextNode, current);

                if (stepCost < 0f)
                    continue;

                float newCost = distances[current] + GetMovementCost(nextNode, current);

                if (newCost > movementAllowed)
                    continue;

                if(!distances.TryGetValue(nextNode, out float oldCost) || newCost < oldCost)
                {
                    distances[nextNode] = newCost;

                    if (!unvisited.Contains(nextNode))
                        unvisited.Add(nextNode);
                }
            }
        }

        return distances;
    }

    private List<GridNode> ReconstructedPath(Dictionary<GridNode, GridNode> previousNode, GridNode home, GridNode destination) 
    {
        List<GridNode> path = new List<GridNode>();
        GridNode target = destination;


        while (target != home) 
        {
            path.Add(target);
            if (!previousNode.TryGetValue(target, out target)) 
            {
                return null; 
            }
        }
        path.Add(home);
        path.Reverse();

        return path; 
    }
    private bool TryGetNextNode(GridNode current, GridNode neighbor, UnitController unit, out GridNode nextNode)
    {
        nextNode = null;

        if (current.floorID == neighbor.floorID) 
        {
            return false;  
        }

        if (occupancy.CanEnterNode(current, neighbor, unit)) 
        {
            nextNode = neighbor;
            return true;
        }

        UnitController occupyingUnit = occupancy.FindUnit(neighbor);

        if(occupyingUnit == null || occupyingUnit == unit) 
        {
            return false;
        }

        float nearestDistance = float.MaxValue;

        foreach (GridNode option in neighbor.GetNeighbors()) 
        {
            if (option == null || option.floorID != neighbor.floorID) 
            {
                continue;
            }

            if (!TerrainLogic.CheckTerrainType(option.currentTerrain)) 
            {
                continue;
            }

            if(!occupancy.CanEnterNode(neighbor, option, unit)) 
            {
                continue;
            }

            float distance = (option.cellWorldPosition - neighbor.cellWorldPosition).sqrMagnitude;

            if (distance < nearestDistance) 
            {
                nearestDistance = distance;
                nextNode = option; 
            }
        }

        return nextNode != null; 
    
    }

    //Calculation--MovementCost, need to add implentation for tile effect
    private float GetMovementCost(GridNode neighbor, GridNode home) 
    {
        float movementCost = neighbor.GetNodeMovementCost();

        float dx = Mathf.Abs(neighbor.coords.x - home.coords.x);
        float dy = Mathf.Abs(neighbor.coords.y - home.coords.y);

        bool isDiagonal = (dx == 1) && (dy == 1);

        if (isDiagonal) 
        {
            movementCost *= Mathf.Sqrt(2f);
        }
        return movementCost;
    }

    public float GetSegmentCost(GridNode from, GridNode to)
    {
        return GetMovementCost(to, from);

    }

    public List<GridNode> GetPath() 
    {
        return route; 
    }
    public GridManager GetGridManager() 
    {
        return gridManager;
    }
}
