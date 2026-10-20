using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Node References- FirstNode, SecondNode (Solid || Edge) ")]
    public GridNode firstnode { get; private set; } = null;
    public GridNode secondnode { get; private set; } = null;
    private Occupancy occupancy;

    [Header("Obstacle Information")]
    [SerializeField] private ObstacleData data;

    public ObstacleData currentData { get; private set; }
    private int currentHealth;


    public void Awake()
    {
        currentData = data;
        if (currentData != null)
        {
            currentHealth = currentData.maxHealth;
        }
        InitializeNodes();
    }

    public void InitializeNodes() 
    {
        GridManager gridManager = FindAnyObjectByType<GridManager>();
        Occupancy occupancy = FindAnyObjectByType<Occupancy>();
        switch (currentData.typeOfObstacle) 
        {
            
            case ObstacleType.Solid:
                {
                    firstnode = gridManager. GetNodeFromClosestWorldPosition(transform.position);
                    occupancy.RegisterSolidObstacles(firstnode, this);
                   
                }
                break;
            case ObstacleType.Edges:
                {
                    
                }
                break;
            default:
                break;

        }
    }
    
    public void SetValue_Health(int currentHealth) 
    {
        this.currentHealth = currentHealth;
    }
   
    public int GetValue_Health() 
    {
        return currentHealth;
    }
    

    //Will be moved into the obstacle logic script later
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= (currentData.maxHealth / 2))
        {
            ChangeState(CoverState.LOW);
        }
        else if (currentHealth <= ((currentData.maxHealth/4))) 
        {
            ChangeState(CoverState.NONE);
        }
    }

    public void RepairObstacle(int repair)
    {
        currentHealth += repair;

        if (currentHealth > (currentData.maxHealth / 2)) 
        {
            ChangeState(CoverState.HIGH);
        }
        
    }

    public void ChangeState(CoverState state) //Needed later for when we change state 
    {
        switch (state) 
        {
            case CoverState.HIGH:
                { 
                    
                }
                break;
            case CoverState.LOW:
                { 
                    
                }
                break;
            case CoverState.NONE:
                
                break;
            default:
                break; 
        }
    }
    public void DestroyObstacle() 
    {
        switch (currentData.typeOfObstacle) 
        {
            case ObstacleType.Solid:
                occupancy.UnregisterSolidObstacles(firstnode, this);
                break;
            case ObstacleType.Edges:
                occupancy.UnregisterEdgeObstacles(firstnode, secondnode, this);
                break;
        }
        Destroy(this);
    }
}
