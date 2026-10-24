using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TileController selectedTile { get; private set; }
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
        UnitSelectedLogic();
        TileSelectedLogic();
    

        
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
        if (!CanCommandUnit(unit))
        {
            return;

        }

        if (selectedUnit == unit)
        {
            ClearUnitSelection();
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

        ClearDestination();

        selectedUnit = unit;
        selectedUnit.SetSelected(true);

        UnitMovements movement = unit.GetComponent<UnitMovements>();

        if (movement != null) 
        {
            movement.ShowMovementRange();
        }
    }
    private void SelecteTile(TileController tile) 
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
    private void UnitSelectedLogic() 
    {
        if (mouseInWorld.unitHit)
        {
            UnitController currentUnit = mouseInWorld.unitTarget.collider.GetComponentInParent<UnitController>();

            if (currentUnit != null)
            {
                SelectUnit(currentUnit);
                return;
            }

        }
    }
    private void TileSelectedLogic() 
    {
        if (mouseInWorld.surfaceHit)
        {
            TileController currentTile = mouseInWorld.groundTarget.collider.GetComponentInParent<TileController>();
            UnitController unit = occupancy.FindUnit(currentTile.node);
           
            if (currentTile != null)
            {
                SelecteTile(currentTile);

                if (unit != null)
                {
                    SelectUnit(unit);
                    return;
                }
                else 
                {
                
                }
                return;
            }
        }
    }
}
