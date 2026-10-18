using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assemblies;

public class MovementManager : MonoBehaviour
{
    [Header("GridManager info")]
    [SerializeField] private GridManager gridManager;

    [Header("Beginning Node and Destination Node")]
    public GridNode startNode { get; private set; }
    public GridNode targetNode { get; private set; }

    [Header("Holds the Path to the Destination")]
    private List<GridNode> route = new List<GridNode>();
    private Dictionary<Vector2Int, GridNode> grid = new Dictionary<Vector2Int, GridNode>();

    public void SetNodes(GridNode start, GridNode destination) 
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

        route = FindPath(startNode, targetNode);
        if (route == null) 
        {
            return; 
        }
    }

    private List<GridNode> FindPath(GridNode startNode, GridNode targetNode) 
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
            visited.Add(current);


            if (current == targetNode)
            {
                //return ReconstructedPath(previousNode, startNode, targetNode);
            }

            foreach (GridNode neighbor in current.GetConnections())
            {
                if (visited.Contains(neighbor))
                    continue;


                float newCost = distance[current] + GetMovementCost(neighbor, current);

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

    //Calculation--MovevementCost, need to add implentation for tile effect
    private float GetMovementCost(GridNode neighbor, GridNode home) 
    {
        float movementCost = neighbor.GetMovementCost();

        float dx = Mathf.Abs(neighbor.coords.x - home.coords.x);
        float dy = Mathf.Abs(neighbor.coords.y - home.coords.y);

        bool isDiagonal = (dx == 1) && (dy == 1);

        if (isDiagonal) 
        {
            movementCost *= Mathf.Sqrt(2f);
        }
        return movementCost;
    }
}
