using UnityEngine;

[System.Serializable]
public class FloorIdentification
{
    [Header("GridFloor Information")]
    [SerializeField] public int floorID; 
    [SerializeField] public Vector3 worldPosition;
    [SerializeField] public Vector2Int gridSize;

    public int GetFloorID()
    {
        return floorID;

    }

    public Vector3 GetWorldPosition() 
    {
        return worldPosition;
    }
    public Vector2Int GetGridSize() 
    {
        return gridSize;
    }
}
