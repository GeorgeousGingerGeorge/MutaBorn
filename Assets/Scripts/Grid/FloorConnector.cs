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

    public Vector3 GetWorldPosition() 
    {
        return worldPosition;
    }
}
