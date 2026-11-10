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
    public int globalFloorID;
    [Header("Global Grid Position")]
    public Vector2Int globalWorldPosition;
    public Vector2Int globalDirection;

    [Header("World Position")]
    public Vector3 worldPosition;

    [Header("Connection Status")]
    public bool isConnected;
}
