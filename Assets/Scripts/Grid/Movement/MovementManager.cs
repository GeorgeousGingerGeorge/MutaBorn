using System.Collections.Generic;
using UnityEngine;


public class MovementManager : MonoBehaviour
{
    [Header("Necessary Information Game Information")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Occupancy occupancy;

    [Header("Beginning Node and Destination Node")]
    public GridNode startNode { get; private set; }
    public GridNode targetNode { get; private set; }

    [Header("Holds the Path to the Destination")]
    private List<GridNode> route = new List<GridNode>();
    private Dictionary<Vector2Int, GridNode> grid = new Dictionary<Vector2Int, GridNode>();

    
    public void SetNodes(GridNode start, GridNode destination, float movementAllowed) 
    {
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
                return ReconstructedPath(previousNode, startNode, targetNode);
            }

            foreach (GridNode neighbor in current.GetNeighbors())
            {
                if(neighbor == null || visited.Contains(neighbor))
                    continue;
                if (!TerrainLogic.CheckTerrainType(neighbor.currentTerrain)) 
                {
                    continue; 
                }
                if (occupancy == null) 
                {
                    return null;
                }
                if (!occupancy.CanEnterNode(current, neighbor)) 
                {
                    continue;
                }
                
                float newCost = distance[current] + GetMovementCost(neighbor, current);

                if (newCost > movementAllowed)
                    continue;

                if (!distance.TryGetValue(neighbor, out float oldCost) || newCost < oldCost) 
                {
                    distance[neighbor] = newCost;
                    previousNode[neighbor] = current;

                    if (!unvisited.Contains(neighbor)) 
                    {
                        unvisited.Add(neighbor);
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
                if (occupancy == null)
                {
                    return null;
                }
                if (!occupancy.CanEnterNode(current, neighbor))
                {
                    continue;
                }

                float stepCost = GetMovementCost(neighbor, current);

                if (stepCost < 0f)
                    continue;

                float newCost = distances[current] + GetMovementCost(neighbor, current);

                if (newCost > movementAllowed)
                    continue;

                if(!distances.TryGetValue(neighbor, out float oldCost) || newCost < oldCost)
                {
                    distances[neighbor] = newCost;

                    if (!unvisited.Contains(neighbor))
                        unvisited.Add(neighbor);
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

    public List<GridNode> GetPath() 
    {
        return route; 
    }
    public GridManager GetGridManager() 
    {
        return gridManager;
    }
}
