using System;
using UnityEngine;

[System.Flags]
public enum SocketConnectionType 
{
   None = 0,
   DoorWay = 1 << 0,
   OpenPassage = 1 << 1,
   PathWay = 1 << 2,

   Stairway = 1 << 3,
   Ladder = 1 << 4,
   Elevator = 1 << 5,

   Special = 1 << 6
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
    public SocketConnectionType allowedConnections;

    public PrefabSocket(int floorID, Vector2Int position, Vector2Int facing, SocketConnectionType type) 
    {
        localFloorID = floorID;
        gridPosition = position;
        direction = facing;
        allowedConnections = type;
    }
}

[System.Serializable]
public struct PrefabSocketRule 
{
    [Header("Perimeter Tile")]
    [SerializeField] private Vector2Int gridPosition;

    [Header("Direction")]
    [SerializeField] private Vector2Int direction;

    [Header("Allowed Connection")]
    [SerializeField] private SocketConnectionType allowedConnections;

    public Vector2Int GridPosition => gridPosition;
    public Vector2Int Direction => direction;

    public SocketConnectionType AllowedConnections => allowedConnections;
}