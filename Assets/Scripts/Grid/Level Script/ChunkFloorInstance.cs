using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkFloorInstance
{
    [Header("Source Definition")]
    public PrefabFloorDefinition sourceDefinition;

    [Header("Floor Identification")]
    public int localFloorID;
    public int globalFloorID;

    [Header("Final Position")]
    public Vector3 worldPosition;

    [Header("Prefab Hierarchy")]
    public Transform floorTransform;

    [Header("ActualGridCells")]
    public List<GridCellDescriptor> cells = new List<GridCellDescriptor>();
   
}
