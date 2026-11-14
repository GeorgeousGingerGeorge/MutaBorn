using UnityEngine;

[System.Serializable]
public class ChunkSocketInstance
{

    [Header("Owning Chunk")]
    public ChunkInstance ownerChunk;

    [Header("Prefab Socket")]
    public int socketIndex;
    public PrefabSocket sourceSocket;

    [Header("Global Floor")]
    public int localFloorID;
    public int globalFloorID = -1;
    public float floorElevation;

    [Header("Global Grid Position")]
    public Vector2Int globalGridPosition;
    public Vector2Int globalDirection;

    [Header("World Position")]
    public Vector3 worldPosition;

    [Header("Connection Status")]
    public bool isConnected;

    public SocketConnectionType connectedAS;  //What Type selected when connected

    public ChunkSocketInstance connectedSocket; // The Other Chunks Socket
    public SocketConnectionType AllowedConnections => sourceSocket.allowedConnections;
}
