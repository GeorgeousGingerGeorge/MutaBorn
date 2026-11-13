
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;

public class UnitMovements : MonoBehaviour
{
    private MovementManager movementController;
    private VisibilityManager visibileController;
    public MovementManager movementManager => movementController;
    private SelectionManager selectionController;
    private TurnManager turnController;
    private UnitFaction factionController;
    private SoundDetectionSystem soundController;
    
   
   
    private UnitController unit;

    private float moveSpeed = 10f;
    public bool isMoving { get; private set; } = false;
    private int pathIndex = 0;
    private float movementEfficiencyAtStart = 1f;
    private List<GridNode> movementPath = new List<GridNode>();
    public Dictionary<GridNode, float> reachableNodes { get; private set; }
    private List<TileController> highlightedTiles = new List<TileController>();
    private Animator animBaseChar;
  

    public void Awake() 
    {
        movementController = FindAnyObjectByType<MovementManager>();
        selectionController = FindAnyObjectByType<SelectionManager>();
        turnController = FindAnyObjectByType<TurnManager>();
        factionController = GetComponent<UnitFaction>();
        visibileController = FindAnyObjectByType<VisibilityManager>();
        soundController = FindAnyObjectByType<SoundDetectionSystem>();
        unit = GetComponent<UnitController>();

        animBaseChar.SetFloat("IdleOffset", Random.value); // Idle animation offset (applies to all idle anims) | value = 0 < x > 1
    }

    // Called after UnitController replaces the race-specific model.
    public void RefreshRaceAnimator(Animator animator)
    {
        animBaseChar = animator;

        if (animBaseChar != null)
        {
            animBaseChar.SetFloat("IdleOffset", Random.value);
            animBaseChar.SetFloat("Speed", 0f);
        }
    }

    public void Update() 
    {
            if (animBaseChar != null)
        {
            animBaseChar.SetFloat("Speed", isMoving ? 1f : 0f);
            animBaseChar.transform.position = transform.position;
        }

    }

    public bool PlayerMovement(GridNode destination)
    {
        if (!turnController.IsFactionTurn(factionController.Faction) && factionController.GetFactionID() != "playerFaction") 
        {
            return false;
        }

        if (selectionController.selectedUnit != unit || 
            selectionController == null || 
            !selectionController.CanCommandUnit(unit)) 
        {
            return false; 
        }

       return AttemptMovement(destination);



    }
    public bool AIMovement(GridNode destination = null, float aiMoveSpeed = 10f) 
    {
        if (unit == null) 
        {
            return false; 
        }

        if (destination == null) 
        {
            ShowMovementRange();
        }

        moveSpeed = aiMoveSpeed;

        if (!AttemptMovement(destination)) 
        {
            return false;
        }

        return true;
    }


    private bool AttemptMovement(GridNode destination) 
    {
        if (isMoving || destination == null) 
        {
            return false;
        }

        MutaBorn.Gameplay.UnitStatusController statuses = unit.GetComponent<MutaBorn.Gameplay.UnitStatusController>();

        if (statuses != null && !statuses.CanMoveVoluntarily) 
        {
            selectionController.ClearDestination();
            return false;
        }

        if (unit.currentNode == null) 
        {
            unit.InitializeNode();
        }

        movementEfficiencyAtStart = unit.data != null ? unit.data.MovementEfficiency : 1f;

        if (movementEfficiencyAtStart <= 0f) 
        {
            return false; 

        }
        float movementBudget = unit.CurrentActionPoints() * movementEfficiencyAtStart;

        if (movementBudget <= 0f) 
        {
            return false;

        }

        movementController.SetNodes(unit.currentNode, destination, movementBudget);

        List<GridNode> path = movementController.GetPath();

        if (path == null || path.Count == 0) 
        {
            return false;
        }


        movementPath.Clear();
        movementPath.AddRange(path);

        pathIndex = 0;

        while (pathIndex < movementPath.Count && movementPath[pathIndex] == unit.currentNode) 
        {
            pathIndex++;
        }

        isMoving = true;
        CameraController.Instance?.FocusOnUnit(unit, true);

        StartCoroutine(HandleMovement());

        return true;
    }
    private IEnumerator HandleMovement()
    {

        if (!isMoving || movementPath == null) 
        {
            yield return null; 
        }

        while (pathIndex < movementPath.Count)
        {

            GridNode nextnode = movementPath[pathIndex];

            //Transform its position
            Vector3 target = nextnode.cellWorldPosition;
            target.y += 0.26f;

            //Rotation in directions its going
            Vector3 direction = target - transform.position;
            direction.y = 0f;

            //Actually handles the rotation
            unit.unitDirection.SetFacing(direction);

            //Handles the movement
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            if (factionController.GetFactionID() == "playerFaction")
            {
                visibileController.UpdateVisibility();
            }
            if (Vector3.Distance(transform.position, target) < 0.01f)
            {

                transform.position = target;

                movementController.readOccupancy.UnregisterUnit(unit.currentNode, unit);

                unit.SetNode(nextnode);
               
                movementController.readOccupancy.RegisterUnit(unit.currentNode, unit);

                pathIndex++;
            }

            yield return null;
        }

        CompleteMovement();

    }
    private void CompleteMovement() 
    {
        soundController?.RegisterSound(unit.currentNode, SoundType.Footsteps, 8, unit);
        visibileController.UpdateVisibility();
        visibileController.UpdateAICurrentDetection();
        unit.data.ModifyActionPoints(-Mathf.CeilToInt(movementController.movementCost / movementEfficiencyAtStart));

        isMoving = false;

       

        movementPath.Clear();
        pathIndex = 0;
        moveSpeed = 10f; 

        if (selectionController.selectedUnit != null && selectionController.selectedUnit == unit)
        {
            selectionController.ClearDestination();
            selectionController.RefreshSelectedUnitAfterMovement(unit);

        }
       
    }


    public void ShowMovementRange() 
    {
        ClearMovementRange();
        MutaBorn.Gameplay.UnitStatusController statuses = unit.GetComponent<MutaBorn.Gameplay.UnitStatusController>();
        if (statuses != null && !statuses.CanMoveVoluntarily)
        {
            return;

        }
        Dictionary<GridNode, float> reachables = new Dictionary<GridNode, float>();

        if(unit.currentNode == null) 
        {
            unit.InitializeNode();
        }
      


        GridNode startNode = unit.currentNode;

        float movementBudget = unit.CurrentActionPoints() *
           (unit.data != null ? unit.data.MovementEfficiency : 1f);

        if (unit.GetComponent<UnitFaction>().GetFactionID() != "playerFaction") 
        {
            reachableNodes = movementController.GetReachableNodes(startNode, movementBudget);
            return;
        }

       
        reachables = movementController.GetReachableNodes(startNode, movementBudget);
       
        

        foreach (KeyValuePair<GridNode, float> values in reachables) 
        {
            
            GridNode node = values.Key;
            float cost = values.Value;

            if(cost > movementBudget)
            {
                continue;

            }
            if (node == startNode) 
            {
                TileController unitSelectedTile = movementController.GetGridManager().FindTile(node);
                unitSelectedTile.SetUnitSelected(true);
                highlightedTiles.Add(unitSelectedTile);
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
                if (tile.isUnitSelected)
                {
                    tile.SetUnitSelected(false);
                    continue;
                }
                tile.SetMovementRange(false);

            }
        }

        highlightedTiles.Clear();
    }
 
    
}
