using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TileController selectedTile { get; private set; }
    public bool isSelected { get; private set; }
    private bool isClicked = false;

    MouseLocation mouseInWorld;
    InputManager input;
    UnitController previousUnit;
    TileController previousTile;
    ObstacleController previousObstacle; 

    
    private void Start()
    {
        mouseInWorld = FindAnyObjectByType<MouseLocation>();
        input = FindAnyObjectByType<InputManager>();
    }

    // Update is called once per frame
    void Update()
    {
        isClicked = input.OnLeftClick();

        TileController currentTile = null;
        UnitController currentUnit = null;
        //ObstacleController currentObstacle = null;

        if (mouseInWorld.unitHit && isClicked) 
        {
            currentUnit = mouseInWorld.unitTarget.collider.GetComponentInParent<UnitController>();
        }
        UnitSelected(currentUnit);

        if (mouseInWorld.surfaceHit && isClicked) 
        {
            currentTile = mouseInWorld.groundTarget.collider.GetComponentInParent<TileController>();

            if (currentTile != null) 
            {
                selectedTile = currentTile;
                isSelected = true; 
            }
        }
        TileSelected(currentTile);
    }

    private void UnitSelected(UnitController current) 
    {
        if (previousUnit == current && current != null) 
        {
            current.SetSelected(true);
            previousUnit = null;
            return; 
        }

        if (previousUnit != null) 
        {
            previousUnit.SetSelected(false);
        }

        current.SetSelected(true);
        previousUnit = current; 
    }

    private void TileSelected(TileController current) 
    {
        if(previousTile == current && current != null) 
        {
            current.SetSelected(false);
            previousTile = null;
            return;
        }

        if (previousTile != null) 
        {
            previousTile.SetSelected(false);
        }

        current.SetSelected(true);

        previousTile = current; 
    }
}
