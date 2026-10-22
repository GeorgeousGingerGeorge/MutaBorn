using UnityEngine;

[System.Flags]
public enum ObstacleType 
{
    None = 0, 
    Solid = 1 << 0,
    Edges = 1 << 1
}