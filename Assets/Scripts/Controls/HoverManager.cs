using UnityEngine;

public class HoverManager : MonoBehaviour
{
    [Header("Used for deteremining hover state")]
    public bool isHovering { get; private set; }

    [Header("Holds the Raycast we need")]
    private MouseLocation mouseInWorld;
    
    [Header("The Objects we'll be detecting")]
    private TileController previousTile;
    private UnitController previousUnit;
    private ObstacleController previousObstacle;

    public void Start()
    {
        mouseInWorld = FindAnyObjectByType<MouseLocation>();
    }

  
    public void Update()
    {
        TileController currentTile = null;
        UnitController currentUnit = null;
        ObstacleController currentObstacle = null;

        if (mouseInWorld.unitHit)
        {
            currentUnit = mouseInWorld.unitTarget.collider.GetComponentInParent<UnitController>();
           
        }
        UnitHover(currentUnit);
       
        if (mouseInWorld.obstacleHit)
        {
            currentObstacle = mouseInWorld.obstacleTarget.collider.GetComponentInParent<ObstacleController>();
            
        }
        ObstacleHover(currentObstacle);
        
        if (mouseInWorld.surfaceHit) 
        {
            currentTile = mouseInWorld.groundTarget.collider.GetComponentInParent<TileController>();
            
        }
        TileHover(currentTile);
    }

    private void TileHover(TileController current) 
    {
        //Checks if the previousTile is still holding a tile and it isn't the currentTile
        if (previousTile != null && previousTile != current) 
        {
            previousTile.SetHover(false);
        }
     
        //Null Check on current
       
        if (current != null) 
        {
            current.SetHover(true);
        }

        previousTile = current; // Sets the previousTile to current

    }
    private void UnitHover(UnitController current) 
    {
        //Checks if previous unit is not null and compares it to current  
        if (previousUnit != null && previousUnit != current)
        {
            Debug.Log("We've entered the previousUnits section");
            previousUnit.SetHover(false);
        }

        //Null check on current
        if (current == null) 
        {
            return;
        }
        else if (current != null)
        {
            current.SetHover(true);
        }
        previousUnit = current; //Assigns previous unit
    }
    private void ObstacleHover(ObstacleController current) 
    {
    
    }
}
