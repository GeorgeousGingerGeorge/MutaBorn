using System.Collections.Generic;
using UnityEngine;


public enum ConnectorType 
{
    Stairs,
    Path,
    DoorWay,
    Ladder,
    Elevator,
    Ramp,
    Drop
}
[System.Serializable]
public struct GridFootPrint 
{
    public Vector2Int size;
    public Vector2Int offset;
}
public class FloorConnector : MonoBehaviour
{
    [Header("ConnectorType")]
    [SerializeField] private ConnectorType _connector;
    public ConnectorType connectorType => _connector;

    [Header("ConnectionPoints Coordinates in the world")]
    [SerializeField] private Transform lowerConnection;
    [SerializeField] private Transform upperConnection;

    private GridNode lowerNode;
    private GridNode upperNode;

    [Header("OccupiedNodes- Holds all the nodes this object covers")]
    private List<GridNode> occupiedNodes = new List<GridNode>();

    public IReadOnlyList<GridNode> nodes => occupiedNodes;

    public void Initialize(GridManager gridManager)
    {
        if (gridManager == null)
        {
            return;
        }

        InitializeConnectionNodes(gridManager);
        CalculateOccupiedNodes(gridManager);
        Connect(gridManager);


        Debug.Log($"[FloorConnector] Initializing: {name}");

        Debug.Log($"Lower marker: {lowerConnection.position}");
        Debug.Log($"Upper marker: {upperConnection.position}");

        Debug.Log($"Lower node: {lowerNode != null}");
        Debug.Log($"Upper node: {upperNode != null}");

        if (lowerNode != null)
        {
            Debug.Log(
                $"Lower node floor: {lowerNode.floorID}, " +
                $"coordinates: {lowerNode.coords}, " +
                $"world position: {lowerNode.cellWorldPosition}"
            );
        }

        if (upperNode != null)
        {
            Debug.Log(
                $"Upper node floor: {upperNode.floorID}, " +
                $"coordinates: {upperNode.coords}, " +
                $"world position: {upperNode.cellWorldPosition}"
            );
        }

    }
    private void InitializeConnectionNodes(GridManager gridManager) 
    {
        lowerNode = gridManager.GetNodeFromClosestWorldPosition(lowerConnection.position);
        upperNode = gridManager.GetNodeFromClosestWorldPosition(upperConnection.position);
    }
    private void CalculateOccupiedNodes(GridManager gridManager) 
    {
        occupiedNodes.Clear();

        float heightDifference = Mathf.Abs(upperConnection.position.y - lowerConnection.position.y);

        float horizontalDifference = Vector2.Distance(new Vector2(lowerConnection.position.x, lowerConnection.position.z),
            new Vector2(upperConnection.position.x, upperConnection.position.z));

        Vector3 lowerPosition = lowerNode.cellWorldPosition;
        Vector3 upperPosition = upperNode.cellWorldPosition;


        Vector3 horizontalDirection = upperPosition - lowerPosition;

        horizontalDirection.y = 0f;

        float horizontalDistance = horizontalDirection.magnitude;

        if (horizontalDistance < gridManager.GridCellSize) 
        {
            return;
        }

        horizontalDirection.Normalize();

        int steps = Mathf.RoundToInt(horizontalDistance / gridManager.GridCellSize);

        for (int i = 1; i < steps; i++) 
        {
            Vector3 nodePosition = lowerPosition + horizontalDirection * (i * gridManager.GridCellSize);

            GridNode node = gridManager.GetNodeFromClosestWorldPosition(nodePosition);

            if (node != null && node != lowerNode && node != upperNode && !occupiedNodes.Contains(node)) 
            {
                occupiedNodes.Add(node);             
            }
        }


    }
    public void Connect(GridManager gridManager)
    {
        GridNode lowerNode = null;
        GridNode upperNode = null;

     

        lowerNode = gridManager.GetNodeFromClosestWorldPosition(lowerConnection.position);
        upperNode = gridManager.GetNodeFromClosestWorldPosition(upperConnection.position);
     
        if (lowerNode == null || upperNode == null) 
        {
            return;
        }

        lowerNode.AddConnections(upperNode);
        upperNode.AddConnections(lowerNode);
    }
   
  
    public void SetWorldPosition(Vector3 position) 
    {
        transform.position = position;
    }
  
  
}
