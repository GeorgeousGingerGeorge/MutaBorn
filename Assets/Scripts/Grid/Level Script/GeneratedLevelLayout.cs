using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class GeneratedLevelLayout
{

    [Header("Generation Information")]
    public int seed;

    [Header("Generated Chunks")]
    public List<ChunkInstance> chunks = new List<ChunkInstance>();

    [Header("Generated Connections")]
    public List<ConnectorPlacement> connectors = new List<ConnectorPlacement>();
}
