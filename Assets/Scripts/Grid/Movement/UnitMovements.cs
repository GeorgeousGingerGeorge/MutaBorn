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
    private List<TileController> highlightedTiles = new List<TileController>();
    private Animator animBaseChar;

    public void Awake() 
    {
        movementController = FindAnyObjectByType<MovementManager>();
        selectionController = FindAnyObjectByType<SelectionManager>();
        unit = this.GetComponent<UnitController>();
        animBaseChar = GetComponent<Animator>();

    }
    
    public void Update() 
    {

        
        HandleMovement();
        animBaseChar.SetFloat("Speed", isMoving ? 1f : 0);
    

    }

    private void HandleMovement() 
    {
        if (unit == null || selectionController.selectedUnit != unit) 
        {
            return;
        }
        if (selectionController.selectedTile == null) 
        {
            return;
        }

       
        if (!isMoving)
        {
            GridNode targetNode = selectionController.selectedTile.node;
            unit.InitializeNode();
            movementController.SetNodes(unit.currentNode, targetNode, unit.CurrentActionPoints());


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
            selectionController.ClearUnitSelection();
            movementPath.Clear();
            pathIndex = 0;
            return;
        }



        GridNode nextnode = movementPath[pathIndex];
        bool isFloorChange = unit.currentNode.floorID != nextnode.floorID;



        Vector3 target = nextnode.cellWorldPosition;
        target.y += 0.5f;

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
        ClearMovementRange();
        Dictionary<GridNode, float> reachables = new Dictionary<GridNode, float>();

        unit.InitializeNode();
        GridNode startNode = unit.currentNode;

        reachables = movementController.GetReachableNodes(startNode, unit.CurrentActionPoints());
       

        foreach (KeyValuePair<GridNode, float> values in reachables) 
        {
            GridNode node = values.Key;
            float cost = values.Value;

            if(cost > unit.CurrentActionPoints())
            {
                continue;

            }
            if (node == startNode) 
            {
                continue;
            }

            TileController tile = movementController.GetGridManager().FindTile(node);

            if(tile != null)
            { 
                tile.SetMovementRange(true);
                highlightedTiles.Add(tile);
            }
        }
        
    }
    public void ClearMovementRange() 
    {
        if (highlightedTiles.Count == 0) 
        {
            return;
        }

        foreach (TileController tile in highlightedTiles) 
        {
            if (tile != null)
            {
                tile.SetMovementRange(false);

            }
        }

        highlightedTiles.Clear();
    }
 
    
}
