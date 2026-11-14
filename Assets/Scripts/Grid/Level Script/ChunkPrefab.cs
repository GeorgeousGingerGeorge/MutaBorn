using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class PrefabTileData 
{
    public TileController tileController;

    public Transform tileTransform;
    public Vector2Int localCoordinates;
}

[System.Serializable]

public class PrefabFloorData
{
    public PrefabFloorDefinition definition;
    public Transform floorTransform;
    public List<PrefabTileData> tiles = new List<PrefabTileData>();


}
public class ChunkPrefab : MonoBehaviour
{
    [Header("Grid Validation")]
    [SerializeField] private float coordinateTolerance = 0.01f;

    public bool TryCollectPrefabData(PrefabDefinition prefabDefinition, float gridCellSize, out List<PrefabFloorData> collectedFloors) 
    {
        collectedFloors = new List<PrefabFloorData>();

        if (prefabDefinition == null) 
        {
            Debug.Log($"[{name}] Missing PrefabDefinition");
            return false;
        }

        if (gridCellSize <= 0f) 
        {
            Debug.Log($"[{name}] Invalid GridCellSize");

            return false;
        }

        bool isValid = true;

        foreach (PrefabFloorDefinition floorDefinition in prefabDefinition.Floors) 
        {
            string floorName = $"Floor_{floorDefinition.LocalFloorID}";

            Transform floorTransform = transform.Find(floorName);

            if (floorTransform == null)
            {
                Debug.Log($"[{name}] Could not find {floorName}", this);
                isValid = false;
                continue;
            }

            if (floorDefinition.GridSize.x <= 0 || floorDefinition.GridSize.y <= 0) 
            {
                Debug.Log($"[{name}] Invalid Grid Dimensions on {floorName}", this);
                isValid = false;
                continue;
            }

            PrefabFloorData floorData = new PrefabFloorData { definition = floorDefinition, floorTransform = floorTransform };

            HashSet<int> registeredSlices = new HashSet<int>();
            HashSet<TileController> registeredTiles = new HashSet<TileController>();

            //Inspect the direct children of the floor
            foreach (Transform floorChild in floorTransform) 
            {
                if (!floorChild.TryGetComponent(out FloorHierarchy hierarchy)) 
                {
                    //No Hierarchy objects, they're ignored
                    continue;
                }

                //Only Floorslice participate in grid extraction
                if(hierarchy.Role != FloorHierarchyRole.FloorSlice) 
                {
                    continue;
                }

                int gridX = hierarchy.SliceX;
                DiagnoseSlice(floorChild, gridX, floorDefinition.GridSize.y);

                if (gridX < 0 || gridX >= floorDefinition.GridSize.x) 
                {
                    Debug.Log($"[{name}] Invalid Slice X coordinate: {gridX}" + $"on {floorChild.name}", this);
                    isValid = false;
                    continue;
                }

                if (!registeredSlices.Add(gridX))
                {
                    Debug.Log($"[{name}] Duplicate FloorSlice X Coordinat: {gridX}", floorChild);

                    isValid = false;
                    continue;
                }

                int gridZ = 0;

                foreach (Transform tileObject in floorChild)
                {
                    TileController tile = tileObject.GetComponentInParent<TileController>();

                    if (tile == null) 
                    {
                        continue;
                    }

                    if (!registeredTiles.Add(tile)) 
                    {
                        Debug.Log($"[{name}] TileController '{tile.name}'" + " appeared more than one in its register", this);
                        continue;
                    }

                    if (gridZ >= floorDefinition.GridSize.y) 
                    {
                        Debug.Log($"[{name}] Too many tiles in floorSlice " + $"{gridX}",floorChild);
                        isValid = false;
                        break;
                    }

                    PrefabTileData tileData = new PrefabTileData
                    {
                        tileController = tile,
                        tileTransform = tileObject,
                        localCoordinates = new Vector2Int(gridX, gridZ)
                    };

                    floorData.tiles.Add(tileData);

                    gridZ++;
                }
                if (gridZ != floorDefinition.GridSize.y) 
                {
                    Debug.Log($"[{name}] FloorSlice {gridX} contains {gridZ} tiles, but the definition expects " +
                        $"{floorDefinition.GridSize.y}", floorChild);




                }

            }

            if (registeredSlices.Count == 0) 
            {
                Debug.Log($"[{name}] No Valid FloorSlices found on {floorName}. ", floorTransform);
                isValid = false;
            }

            collectedFloors.Add(floorData);
        }
        return isValid;
    }

    private void DiagnoseSlice(
    Transform floorSlice,
    int sliceX,
    int expectedZ)
    {
        int rawDirectHits = 0;

        HashSet<TileController> directTiles = new();

        foreach (Transform child in floorSlice)
        {
            TileController tile =
                child.GetComponentInParent<TileController>();

            if (tile != null)
            {
                rawDirectHits++;
                directTiles.Add(tile);
            }
            else
            {
                // Check whether a TileController exists further down.
                TileController nested =
                    child.GetComponentInChildren<TileController>(true);

                if (nested != null)
                {
                    Debug.LogWarning(
                        $"Slice X={sliceX}: '{child.name}' contains " +
                        $"a nested TileController that the current " +
                        $"extraction method cannot find.",
                        child);
                }
            }
        }

        TileController[] allDescendants =
            floorSlice.GetComponentsInChildren<TileController>(true);

        HashSet<TileController> recursiveTiles =
            new(allDescendants);

        Debug.Log(
            $"SLICE X={sliceX}\n" +
            $"Expected Z count: {expectedZ}\n" +
            $"Direct children: {floorSlice.childCount}\n" +
            $"Direct controller references: {rawDirectHits}\n" +
            $"Unique direct controllers: {directTiles.Count}\n" +
            $"Recursive controllers: {recursiveTiles.Count}\n" +
            $"Duplicate direct references: " +
            $"{rawDirectHits - directTiles.Count}\n" +
            $"Potentially missed nested controllers: " +
            $"{recursiveTiles.Count - directTiles.Count}",
            floorSlice);
    }
}
