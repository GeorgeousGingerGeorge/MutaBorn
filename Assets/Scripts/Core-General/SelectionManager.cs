using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TileController selectedTile { get; private set; }
    public UnitController selectedUnit { get; private set; }

    private MouseLocation mouseInWorld;
    private InputManager input;

    private void Start() 
    {
        mouseInWorld = FindAnyObjectByType<MouseLocation>();
        input = FindAnyObjectByType<InputManager>();

    }

    private void Update() 
    {
        if (mouseInWorld == null || input == null)
            return;
        if (!input.OnLeftClick())
            return;

        if(mouseInWorld.unitHit)
        {
            UnitController currentUnit = mouseInWorld.unitTarget.collider.GetComponentInParent<UnitController>();

            if (currentUnit != null) 
            {
                SelectUnit(currentUnit);
                return;
            }

        }

        if (mouseInWorld.surfaceHit) 
        {
            TileController currentTile = mouseInWorld.groundTarget.collider.GetComponentInParent<TileController>();

            if (currentTile != null) 
            {
                SelecteTile(currentTile);
                return;
            }
        }
    }

    private void SelectUnit(UnitController unit) 
    {
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

}
