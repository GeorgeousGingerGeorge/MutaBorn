using System.Collections.Generic;
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

    public void Awake()
    {
        currentData = data;
        if (currentData != null)
        {
            currentHealth = currentData.maxHealth;
            currentState = currentData.coverState;
        }
       
    }

    public void InitializeNodes() 
    {
        GridManager gridManager = FindAnyObjectByType<GridManager>();
        occupancy = FindAnyObjectByType<Occupancy>();
        switch (currentData.typeOfObstacle)
        {

            case ObstacleType.Solid:
                {
                    firstnode = gridManager.GetNodeFromClosestWorldPosition(transform.position);
                    occupancy.RegisterSolidObstacles(firstnode, this);

                }
                break;
            case ObstacleType.Edges:
                {


                    Vector3 perpendicular = transform.right;
                    perpendicular.y = 0f;
                    perpendicular.Normalize();

                    float cellsizeCenter = gridManager.GridCellSize / 2f;

                    Vector3 positionA = transform.position + perpendicular * cellsizeCenter;
                    Vector3 positionB = transform.position - perpendicular * cellsizeCenter;


                    firstnode = gridManager.GetNodeFromClosestWorldPosition(positionA);
                    secondnode = gridManager.GetNodeFromClosestWorldPosition(positionB);

                    if (firstnode == null || secondnode == null || firstnode == secondnode)
                    {

                        return;
                    }

                    occupancy.RegisterEdgeObstacles(firstnode, secondnode, this);
                }
                break;
            case ObstacleType.FloorConnectors:
                {
                    FloorConnector connector = GetComponent<FloorConnector>();
                    switch (connector.connectorType) 
                    {
                        case ConnectorType.Stairs:
                            {
                                foreach (GridNode node in connector.nodes) 
                                {
                                    Debug.Log($"FloorID ->{node.floorID}, GridPosition -> {node.coords} ");
                                    occupancy.RegisterSolidObstacles(node, this);
                                }
                            }
                            break;
                        default:
                            break;
                    }
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
        switch (currentData.typeOfObstacle) 
        {
            case ObstacleType.Solid:
                occupancy.UnregisterSolidObstacles(firstnode, this);
                break;
            case ObstacleType.Edges:
                occupancy.UnregisterEdgeObstacles(firstnode, secondnode, this);
                break;
            default:
                break;
        }
        Destroy(this);
    }
}
