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
    

    [Header("Floor Information")]

    public List<PrefabFloorDefinition> floors;

    [Header("Global Floor Mapping")]
    public Dictionary<int, int> globalFloorMappings;
}
