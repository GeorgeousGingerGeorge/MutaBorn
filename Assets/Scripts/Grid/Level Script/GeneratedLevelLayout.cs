using System.Collections.Generic;
[System.Serializable]
public class GeneratedLevelLayout
{
    public int seed;
    public List<ChunkInstance> chunks = new List<ChunkInstance>();
    public List<ConnectorPlacement> connectors = new List<ConnectorPlacement>();
}
