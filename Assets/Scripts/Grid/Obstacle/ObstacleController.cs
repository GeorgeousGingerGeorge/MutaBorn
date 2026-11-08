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
    private CoverState currentState;
    public bool blockLineOfSight { get; private set; }

    private bool registered;

    public bool IsBlocking => registered;
    public Occupancy CurrentOccupancy => occupancy;

    public void Awake()
    {
        InitializeData();

    }

    private bool InitializeData()
    {
        if (currentData != null)
        {

            return true;

        }

        currentData = data;
        if (currentData == null)
        {
            Debug.LogError($"Obstacle '{name}' has no ObstacleData assigned.", this);

            return false;

        }

        currentHealth = currentData.maxHealth;
        currentState = currentData.coverState;
        blockLineOfSight = currentData.blocksLineOfSight;

        return true;

    }

    public void InitializeNodes()
    {
        if (!InitializeData())
        {

            return;

        }

        GridManager gridManager = FindAnyObjectByType<GridManager>();
        occupancy = FindAnyObjectByType<Occupancy>();
        if (gridManager == null || occupancy == null)
        {
            Debug.LogError($"Obstacle '{name}' requires GridManager and Occupancy.", this);

            return;

        }

        switch (currentData.typeOfObstacle)
        {
            case ObstacleType.Solid:
                firstnode = gridManager.GetNodeFromClosestWorldPosition(transform.position);

                break;

            case ObstacleType.Edges:
                Vector3 perpendicular = transform.right;
                perpendicular.y = 0f;
                perpendicular.Normalize();

                float offset = gridManager.GridCellSize / 2f;
                firstnode = gridManager.GetNodeFromClosestWorldPosition(transform.position + perpendicular * offset);
                secondnode = gridManager.GetNodeFromClosestWorldPosition(transform.position - perpendicular * offset);

                break;

            case ObstacleType.FloorConnectors:
                FloorConnector connector = GetComponent<FloorConnector>();
                if (connector != null && connector.connectorType == ConnectorType.Stairs)
                {
                    foreach (GridNode node in connector.nodes)
                    {
                        Debug.Log($"FloorID ->{node.floorID}, GridPosition -> {node.coords} ");
                        occupancy.RegisterSolidObstacles(node, this);

                    }

                    registered = true;

                }

                return;

        }

        if (!TrySetBlocking(true))
        {
            Debug.LogWarning($"Obstacle '{name}' could not register its grid nodes. Check placement or overlaps.", this);

        }

    }

    // Used by doors and gates without destroying the obstacle component.
    public bool TrySetBlocking(bool blocked)
    {
        if (!InitializeData() || occupancy == null || firstnode == null)
        {

            return false;

        }

        if (blocked == registered)
        {

            return true;

        }

        switch (currentData.typeOfObstacle)
        {
            case ObstacleType.Solid:
                if (blocked)
                {
                    if (occupancy.IsUnitInNode(firstnode) ||
                        occupancy.FindSolidObstacle(firstnode) != null)
                    {

                        return false;

                    }

                    occupancy.RegisterSolidObstacles(firstnode, this);

                }
                else
                {
                    occupancy.UnregisterSolidObstacles(firstnode, this);

                }

                break;

            case ObstacleType.Edges:
                if (secondnode == null || secondnode == firstnode)
                {

                    return false;

                }

                if (blocked)
                {
                    if (occupancy.FindEdgeObstacle(firstnode, secondnode) != null ||
                        occupancy.FindEdgeObstacle(secondnode, firstnode) != null)
                    {

                        return false;

                    }

                    occupancy.RegisterEdgeObstacles(firstnode, secondnode, this);

                }
                else
                {
                    occupancy.UnregisterEdgeObstacles(firstnode, secondnode, this);

                }

                break;

            default:

                return false;

        }

        registered = blocked;

        return true;

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
        if (currentHealth <= (currentData.maxHealth / 2) && currentHealth > (currentData.maxHealth / 4))
        {
            ChangeState(CoverState.LOW);
        }
        else if (currentHealth <= ((currentData.maxHealth / 4)))
        {
            ChangeState(CoverState.NONE);
        }
        else if (currentHealth <= 0) 
        {
            Destroy(this);
        }
    }

    public void RepairObstacle(int repair)
    {
        currentHealth += repair;

        if (currentHealth > (currentData.maxHealth / 2))
        {
            ChangeState(CoverState.HIGH);
        }
        else if (currentHealth < (currentData.maxHealth / 2) && currentHealth > (currentData.maxHealth / 4)) 
        {
            ChangeState(CoverState.LOW);
        }
        
    }

    public void ChangeState(CoverState state) //Needed later for when we change state 
    {
        if (currentState == state) 
        {
            return;
        }

        switch (state) 
        {
            case CoverState.HIGH:
                {
                    currentState = state;
                }
                break;
            case CoverState.LOW:
                {
                    currentState = state;
                }
                break;
            case CoverState.NONE:
                {
                    currentState = state;
                }
                break;
            default:
                break; 
        }
    }
    public void DestroyObstacle()
    {
        if (currentData != null && currentData.typeOfObstacle == ObstacleType.FloorConnectors)
        {
            FloorConnector connector = GetComponent<FloorConnector>();
            if (occupancy != null && connector != null && connector.connectorType == ConnectorType.Stairs)
            {
                foreach (GridNode node in connector.nodes)
                {
                    occupancy.UnregisterSolidObstacles(node, this);

                }

            }

        }
        else
        {
            TrySetBlocking(false);

        }

        Destroy(this);

    }
}
