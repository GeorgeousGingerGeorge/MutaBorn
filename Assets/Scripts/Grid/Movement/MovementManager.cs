using System.Collections.Generic;
using UnityEngine;

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

        grid = gridManager.GetNodeFromGridPosition(startNode.floorID);
    }   

}
