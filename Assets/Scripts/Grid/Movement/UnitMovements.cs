using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;

public class UnitMovements : MonoBehaviour
{
    private MovementManager movementController;
    private SelectionManager selectionController;
    private UnitController unit;

    private float moveSpeed = 20f;
    private bool isSelected = false;
    private bool isMoving = false;
    private int pathIndex = 0;
    private List<GridNode> movementPath = new List<GridNode>();

    public void Awake() 
    {
        movementController = FindAnyObjectByType<MovementManager>();
        selectionController = FindAnyObjectByType<SelectionManager>();
        unit = this.GetComponent<UnitController>();

    }
    
    public void Update() 
    {

        
        HandleMovement();
    

    }

    private void HandleMovement() 
    {
        if (selectionController.selectedTile == null) 
        {
            return;
        }

       
        if (!isMoving)
        {
            GridNode targetNode = selectionController.selectedTile.node;
            unit.InitializeNode();
            movementController.SetNodes(unit.currentNode, targetNode);


            movementPath = movementController.GetPath();
            if (movementPath == null || movementPath.Count == 0)
            {

                isMoving = false;
                selectionController.ClearDestination();
                return;
            }

            pathIndex = 0;
            while (pathIndex < movementPath.Count && movementPath[pathIndex] == unit.currentNode)
            {
                pathIndex++;
            }
            isMoving = true;
        }

        if (pathIndex >= movementPath.Count)
        {
            isMoving = false;
            selectionController.ClearDestination();
            movementPath.Clear();
            return;
        }



        GridNode nextnode = movementPath[pathIndex];
        bool isFloorChange = unit.currentNode.floorID != nextnode.floorID;



        Vector3 target = nextnode.cellWorldPosition;
        target.y += 1;

        if (!isFloorChange)
        {
            target.y = transform.position.y;
        }
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {

            transform.position = target;

            unit.SetNode(nextnode);
            pathIndex++;
        }

    }



    public void ShowMovementRange() 
    {
        Dictionary<GridNode, float> reachables = new Dictionary<GridNode, float>();
    
        GridNode startNode = selectionController.selectedUnit.currentNode;

        reachables = movementController.GetReachableNodes(startNode, unit.StaminaCost());

        foreach (GridNode node in reachables.Keys) 
        {
            if (node == startNode) 
            {
                continue;
            }

            TileController tile = movementController.GetGridManager().FindTile(node);

            if(tile != null)
                tile.SetMovementRange(true);
        }
        
    }
    public void ClearMovementRange() 
    {
        Dictionary<GridNode, float> reachables = new Dictionary<GridNode, float>();

        GridNode startNode = selectionController.selectedUnit.currentNode;

        reachables = movementController.GetReachableNodes(startNode, unit.StaminaCost());

        foreach (GridNode node in reachables.Keys)
        {
            if (node == startNode)
            {
                continue;
            }

            TileController tile = movementController.GetGridManager().FindTile(node);

            if (tile != null)
                tile.SetMovementRange(false);
        }
    }
 
    
}
