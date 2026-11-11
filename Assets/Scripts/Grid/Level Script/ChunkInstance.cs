using System.Collections.Generic;
using UnityEngine;

public class ChunkInstance
{
    public ChunkDefinition chunkDefinition;
    public PrefabDefinition prefabDefinition;

    [Header("Chunk Allocation")]
    public Vector2Int chunkGridOrigin;
    public Vector2Int allocatedDimensions;

    [Header("Prefab Instance")]
    public GameObject prefabInstance;
    public int rotationDegrees;
    public Vector2Int prefabGridOffset;

    [Header("Floor Information")]

    public List<ChunkFloorInstance> floors;

    [Header("Global Floor Mapping")]
    public Dictionary<int, int> globalFloorMappings;

    public float generationWeight;
    public List<ChunkSocketInstance> sockets = new List<ChunkSocketInstance>();
}
