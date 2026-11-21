

[System.Flags]
public enum ObstacleType 
{
    None = 0, 
    Solid = 1 << 0,
    Edges = 1 << 1,
    FloorConnectors = 1 << 2,
    WorldSpace = 1 << 3
}


public enum NodeCoverage 
{
    Single, // Means the obstacle only covers a single node
    Multiple, // Checks underneath a obstacle for all the nodes it occupies
    Edge, // means its the boundary between two nodes
    BoundaryEdge, // means its the boundary of a floor
}