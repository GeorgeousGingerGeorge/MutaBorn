using UnityEngine;


public enum SocketType 
{
    Standard,
    Wide,
    Restricted,
    Vertical,
    Special
}
[System.Flags]
public enum SocketSide 
{
    None = 0,
    North = 1 << 0,
    East = 1 << 1,
    South = 1 << 2,
    West = 1 << 3,

    All = North | East | South | West
}
[System.Serializable]
public struct PrefabSocket 
{
    public int localFloorID;
    public Vector2Int gridPosition;
    public Vector2Int direction;

    public PrefabSocket(int floorID, Vector2Int position, Vector2Int facing) 
    {
        localFloorID = floorID;
        gridPosition = position;
        direction = facing;
    }
}