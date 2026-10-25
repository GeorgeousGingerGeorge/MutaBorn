using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TileController selectedTile { get; private set; }
    private TileController selectedUnitTile = null; 
    public UnitController selectedUnit { get; private set; }

    private Occupancy occupancy;
    private MouseLocation mouseInWorld;
    private InputManager input;

    private void Start() 
    {
        mouseInWorld = FindAnyObjectByType<MouseLocation>();
        input = FindAnyObjectByType<InputManager>();
        occupancy = FindAnyObjectByType<Occupancy>();
    }

    private void Update() 
    {
        if (selectedUnit != null && !CanCommandUnit(selectedUnit))
        {
           
            ClearUnitSelection();
            ClearDestination();

        }

        if (mouseInWorld == null || input == null)
            return;
        if (!input.OnLeftClick())
            return;

        // Target clicks belong to targeting mode, not unit/terrain selection.
        if (TargetingModeController.BlocksSelection)
            return;

        // Do not select units or tiles through Action Menu UI.
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        HandleClickSelection();

        
    }

    //New Method to help prevent selecting or controlling other Factions Units
    private bool CanCommandUnit(UnitController unit)
    {
        TurnManager turns = TurnManager.Instance;

        if (unit == null || turns == null || !turns.IsPlayerTurn)
        {
            return false;

        }

        UnitFaction unitFaction = unit.GetComponent<UnitFaction>();

        return unitFaction != null && unitFaction.Faction != null && turns.IsFactionTurn(unitFaction.Faction);

    }

    private void SelectUnit(UnitController unit) 
    {
        
 

        if (selectedUnit == unit)
        {
            ClearUnitSelectedTile();
            ClearUnitSelection();
            ClearDestination();
           
            return;
        }
            
        if (selectedUnit != null) 
        {
            UnitMovements previousMovement = selectedUnit.GetComponent<UnitMovements>();

            if (previousMovement != null) 
            {
                previousMovement.ClearMovementRange();
            }
            selectedUnit.SetSelected(false);
         
        }

        ClearUnitSelection();
        ClearUnitSelectedTile();

        ClearDestination();

        selectedUnit = unit;
        selectedUnit.SetSelected(true);

        GridManager gridManager = occupancy.ReturnGrid();
        if (gridManager != null && selectedUnit.currentNode != null)
        {
            selectedUnitTile = gridManager.FindTile(unit.currentNode);

            if (selectedUnitTile != null)
            {
                selectedUnitTile.SetUnitSelected(true);
            }
        }
       
        

      if(CanCommandUnit(unit))
      {

            UnitMovements movement = unit.GetComponent<UnitMovements>();

            if (movement != null)
            {
                movement.ShowMovementRange();
            }
        }
    }
    private void SelectTile(TileController tile) 
    {
        if (selectedTile == tile)
        {
            ClearDestination();
            return;
        }
        if (selectedTile != null) 
        {
            selectedTile.SetSelected(false);
        }

        selectedTile = tile;
        selectedTile.SetSelected(true);

       
    }


    public void ClearDestination() 
    {
        if (selectedTile != null) 
        {
            selectedTile.SetSelected(false);

        }

        selectedTile = null;
    }
    public void ClearUnitSelection() 
    {
        if (selectedUnit != null) 
        {
            UnitMovements movement = selectedUnit.GetComponent<UnitMovements>();
            if (movement != null) 
            {
                movement.ClearMovementRange();
                
            }

            selectedUnit.SetSelected(false);
        }

        selectedUnit = null;
    }
    public void ClearUnitSelectedTile()
    {
        if (selectedUnit == null)
        {
           
            return;
        }

        selectedUnitTile.SetUnitSelected(false);
        selectedUnitTile = null;

    }
       

    private void HandleClickSelection() 
    {
        if (mouseInWorld.unitHit && mouseInWorld.unitTarget.collider != null)
        {
            UnitController currentUnit = mouseInWorld.unitTarget.collider.GetComponentInParent<UnitController>();

            if (currentUnit != null)
            {
                SelectUnit(currentUnit);
                return;
            }

        }

        if (mouseInWorld.surfaceHit && mouseInWorld.groundTarget.collider != null)
        {
            TileController currentTile = mouseInWorld.groundTarget.collider.GetComponentInParent<TileController>();

            if (currentTile == null || currentTile.node == null) 
            {
                return; 
            }

            UnitController unit = occupancy.FindUnit(currentTile.node);

            if (unit != null) 
            {
                SelectUnit(unit);
                return;
            }

            SelectTile(currentTile);

            Debug.Log($"Terrain: {currentTile.node.currentTerrain}");
            Debug.Log($"Coordinates: {currentTile.node.coords}");
            Debug.Log($"Coordinates: {currentTile.node.floorID}");

        }
    }

 
}
