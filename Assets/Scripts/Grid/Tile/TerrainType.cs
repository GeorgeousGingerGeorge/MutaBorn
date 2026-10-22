using UnityEngine;
[System.Flags]
public enum TerrainType 
{
    Dirt = 0,
    HighGrass = 1 << 0,
    Rubble = 1 <<1, 
    Mud = 1 << 2

}