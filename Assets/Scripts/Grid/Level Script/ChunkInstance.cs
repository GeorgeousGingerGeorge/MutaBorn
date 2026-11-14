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
    public Vector2Int prefabGridOffset;

    [Header("Floor Information")]

    public List<ChunkFloorInstance> floors = new List<ChunkFloorInstance>();

    [Header("Global Floor Mapping")]
    public Dictionary<int, int> globalFloorMappings = new Dictionary<int, int>();

    public float generationWeight;
    public List<ChunkSocketInstance> sockets = new List<ChunkSocketInstance>();
}
