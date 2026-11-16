using UnityEngine;

[System.Flags]
public enum TerrainType 
{
    TestTerrain = 0,
    OpenGround = 1 << 0,
    HighGrass = 1 << 1,
    Rubble = 1 << 2, 
    Mud = 1 << 3

}

