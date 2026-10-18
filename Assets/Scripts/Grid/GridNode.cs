using System.Collections.Generic;
using UnityEngine;

public class GridNode
{

    [Header("Grid Information")]
    public Vector2Int coords { get; private set; }
    public Vector3 cellWorldPosition { get; private set; }
    public int floorID { get; private set; }

    [Header("Grid Information- Neighbors to this node")]
    private List<GridNode> connectTo = new List<GridNode>();

    [Header("Node Inforamtion")]
    public float movementCost = 2f;
        

    public GridNode(Vector2Int gridCoords, int currentFloor, Vector3 worldPosition) 
    {
        coords = gridCoords;
        floorID = currentFloor;
        cellWorldPosition = worldPosition;
    }

    //Setters

    //Adds a node to a list of neighbors if they're next to the current node
    public void AddConnections(GridNode current) 
    {
        if(!connectTo.Contains(current))
            connectTo.Add(current);
    }


    //Getters;

    public IReadOnlyList<GridNode> GetConnections() 
    {
        return connectTo; 
    }
    public List<GridNode> GetNeighbors() 
    {
        return connectTo;
    }
    public float GetMovementCost() 
    {
        return movementCost;
    }
   

}
