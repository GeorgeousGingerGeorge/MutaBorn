using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class PrefabFloorDefinition
{
    [Header("Local Floor Information")]
    [SerializeField] private int localFloorID;

    [Header("Local Positon")]
    [SerializeField] private Vector3 localPosition;

    [Header("Grid Position")]
    [SerializeField] private Vector2Int gridSize;

    [Header("Potential Socket Sides")]
    [SerializeField] private SocketSide socketSides = SocketSide.All;

    [Header("Socket Type Config")]
    [SerializeField] private SocketConnectionType socketType = SocketConnectionType.None;

    [SerializeField] private List<PrefabSocketRule> socketRules = new List<PrefabSocketRule>();

    [Header("ReadOnly Access")]

    public int LocalFloorID => localFloorID;
    public Vector3 LocalPosition => localPosition;
    public Vector2Int GridSize => gridSize;
    public SocketSide SocketSides => socketSides;
    public SocketConnectionType SocketCategory => socketType;
    public IReadOnlyList<PrefabSocketRule> SocketRules => socketRules;


    public List<PrefabSocket> GeneratePerimeterSockets() 
    {
        List<PrefabSocket> sockets = new List<PrefabSocket>();

        if (gridSize.x <= 0 || gridSize.y <= 0) 
        {
            return sockets;
        }

        //Check North - Positive Z axis of the Prefab

        if((socketSides & SocketSide.North) != 0)
        {
            for (int x = 0; x < gridSize.x; x++) 
            {
                AddPerimeterSocket(sockets, new Vector2Int(x, gridSize.y - 1), Vector2Int.up);
            }
        }

        //Check South - Negative Z axis of the prefab
        if ((socketSides & SocketSide.South) != 0) 
        {
            for (int x = 0; x < gridSize.x; x++) 
            {
                AddPerimeterSocket(sockets, new Vector2Int(x, 0), Vector2Int.down);
            }
        }

        //Check East - Positive X Axis of the prefab
        if ((socketSides & SocketSide.East) != 0) 
        {
            for (int y = 0; y < gridSize.y; y++) 
            {
                AddPerimeterSocket(sockets ,new  Vector2Int(gridSize.x - 1, y), Vector2Int.right);
            }
        }

        //Check West = Negative X Axis of the prefab
        if ((socketSides & SocketSide.West) != 0) 
        {
            for (int y = 0; y < gridSize.y; y++) 
            {
                AddPerimeterSocket(sockets,new Vector2Int(0, y), Vector2Int.left);
            }
        }

        return sockets;
    }

    private void AddPerimeterSocket(List<PrefabSocket> sockets, Vector2Int position, Vector2Int direction) 
    {

        SocketConnectionType allowed = GetAllowedConnections(position, direction);

        sockets.Add(new PrefabSocket(localFloorID, position, direction, allowed));
    }

    private SocketConnectionType GetAllowedConnections(Vector2Int position, Vector2Int direction) 
    {
        foreach (PrefabSocketRule rule in socketRules) 
        {
            if (rule.GridPosition == position && rule.Direction == direction) 
            {
                return rule.AllowedConnections;
            }
        }

        //This Perimeter has no rule 
        return SocketConnectionType.None;
    }
}
