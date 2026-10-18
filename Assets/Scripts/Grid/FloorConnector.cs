using UnityEngine;

public class FloorConnector : MonoBehaviour
{
    [Header("Coordinates in the World")]
    [SerializeField] private Vector3 worldPosition;
    [Header("Lower Floor Connection")]
    [SerializeField] private int lowerFloorID;
    [SerializeField] private Vector2Int lowerGridPosition;
    [Header("Upper Floor Connection")]
    [SerializeField] private int upperFloorID;
    [SerializeField] private Vector2Int upperGridPosition;

    public void Connect(GridManager gridManager)
    {
        GridNode lowerNode = gridManager.GetNodeFromGridPosition(lowerFloorID, lowerGridPosition);
        GridNode upperNode = gridManager.GetNodeFromGridPosition(upperFloorID, upperGridPosition);

        
        if (lowerNode == null || upperNode == null) 
        {
            return;
        }

        lowerNode.AddConnections(upperNode);
        upperNode.AddConnections(lowerNode);
    }

    //Setters
    public void SetWorldPosition(Vector3 worldPosition)
    {
        this.worldPosition = worldPosition;
    }
    public void SetLowerFloor(int index) 
    {
        lowerFloorID = index;
    }
    public void SetLowerGridNode(Vector2Int lowerGridPosition) 
    {
        this.lowerGridPosition = lowerGridPosition;
    }
    public void SetUpperFloor(int index) 
    {
        upperFloorID = index;
    }
    public void SetUpperGridNode(Vector2Int upperGridPosition)
    {
        this.upperGridPosition = upperGridPosition;
    }



    //Getters
    public Vector3 GetWorldPosition() 
    {
        return worldPosition;
    }

   
}
