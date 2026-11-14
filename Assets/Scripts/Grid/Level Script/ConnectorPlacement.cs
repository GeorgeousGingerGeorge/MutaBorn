using UnityEngine;

public enum ConnectorConnectionMode 
{
    SameFloor,
    BetweenFloor,
    Either
}

[System.Serializable]
public class ConnectorPlacement
{
    [Header("Connector Definition")]
    public ConnectorDefinition connectorDefinition;
    [Header("First Connection EndPoint")]
    public ChunkSocketInstance firstSocket;
    [Header("Second Connection Endpoint")]
    public ChunkSocketInstance secondSocket;

    [Header("Final Placement")]
    public Vector3 worldPosition;
    public Quaternion worldRotation;

    [Header("Instantiated Connector")]
    public GameObject connectorInstance;
}
