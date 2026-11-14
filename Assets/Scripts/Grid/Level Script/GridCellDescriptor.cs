using UnityEngine;

[System.Serializable]
public class GridCellDescriptor
{
    public int globalFloorID;
    public Vector2Int localCoordinates;
    public Vector2Int globalCoordinates;
    public Vector3 worldPosition;
    public TileController tile; 
}
