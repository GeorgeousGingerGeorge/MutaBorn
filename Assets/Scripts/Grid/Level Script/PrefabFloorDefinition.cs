using UnityEngine;
using System.Collections.Generic;

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



    [Header("ReadOnly Access")]

    public int LocalFloorID => localFloorID;
    public Vector3 LocalPosition => localPosition;
    public Vector2Int GridSize => gridSize;
    public SocketSide SocketSides => socketSides;


    public List<PrefabSocket> GeneratePerimeterSockets() 
    {
        List<PrefabSocket> sockets = new List<PrefabSocket>();


        //Check North - Positive Z axis of the Prefab

        if((socketSides & SocketSide.North) != 0)
        {
            for (int x = 0; x < gridSize.x; x++) 
            {
                sockets.Add(new PrefabSocket(localFloorID, new Vector2Int(x, gridSize.y - 1),Vector2Int.up));
            }
        }

        //Check South - Negative Z axis of the prefab
        if ((socketSides & SocketSide.South) != 0) 
        {
            for (int x = 0; x < gridSize.x; x++) 
            {
                sockets.Add(new PrefabSocket(localFloorID, new Vector2Int(x, 0), Vector2Int.down));
            }
        }

        //Check East - Positive X Axis of the prefab
        if ((socketSides & SocketSide.East) != 0) 
        {
            for (int y = 0; y < gridSize.y; y++) 
            {
                sockets.Add(new PrefabSocket(localFloorID, new Vector2Int(y, gridSize.x - 1), Vector2Int.right));
            }
        }

        //Check West = Negative X Axis of the prefab
        if ((socketSides & SocketSide.West) != 0) 
        {
            for (int y = 0; y < gridSize.y; y++) 
            {
                sockets.Add(new PrefabSocket(localFloorID, new Vector2Int(y, 0), Vector2Int.left));
            }
        }

        return sockets;
    }

}
