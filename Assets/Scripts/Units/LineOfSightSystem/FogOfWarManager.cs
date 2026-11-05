using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public enum FogState 
{ 
    CLEAR,
    DARK
}
public class FogOfWarManager : MonoBehaviour
{
    [Header("LineOfSight System- Sight Calculations")]
    [SerializeField] private LineOfSight lineOfSight;

    [Header("GridManager")]
    [SerializeField] private GridManager gridManager;


    [Header("Texture used to determine FogOfWar Across Grid")]
    private Dictionary<int, Texture2D> floorFogTextures = new Dictionary<int, Texture2D>();

    [Header("Current player units nodes they've visited and stored")]
    private HashSet<GridNode> visitedNodes = new HashSet<GridNode>();
    private HashSet<GridNode> exploredNodes = new HashSet<GridNode>();
    private HashSet<GridNode> newlyExploredNodes = new HashSet<GridNode>();

    [Header("Fog Of War Rendering")]
    [SerializeField] private Material fogMaterial;
    private Texture2DArray floorFogTextureArray;
    private Dictionary<int, int> floorTextureIndices = new Dictionary<int, int>();
    private Dictionary<int, Vector3> floorOrigins = new Dictionary<int, Vector3>();

    private static readonly int GridCellSizeID = Shader.PropertyToID("_GridCellSize");

    private Texture2D floorMetaDataTexture;

    private void Start() 
    {
        InitializeFogOfWar();
        CreateFloorFogTextureArray();
        CreateFloorMetaDataTexture();
        UpdateFriendlyUnitVisibility();

        fogMaterial.SetTexture("_FloorFogTextures", floorFogTextureArray);
        fogMaterial.SetTexture("_FloorMetaData",floorMetaDataTexture);
        fogMaterial.SetFloat(GridCellSizeID, gridManager.GridCellSize);
        fogMaterial.SetFloat("_FloorCount", floorFogTextures.Count);
    }
    public void UpdateFriendlyUnitVisibility() 
    {
        UnitController[] allUnits = FindObjectsByType<UnitController>();

        List<UnitController> friendlyUnits = new List<UnitController>();

        foreach (UnitController unit in allUnits) 
        {
            UnitFaction faction = unit.GetComponent<UnitFaction>();

            if (faction != null && faction.GetFactionID() == "playerFaction") 
            {
                friendlyUnits.Add(unit);
            }
        }

        UpdateVisibility(friendlyUnits);
    }
    private void UpdateVisibility(IEnumerable<UnitController> friendlyUnits) 
    {
        visitedNodes.Clear();

        foreach (UnitController unit in friendlyUnits) 
        {
            HashSet<GridNode> visibleNodes = lineOfSight.GetVisibleNodes(unit);

            foreach (GridNode node in visibleNodes) 
            {
                visitedNodes.Add(node);
                exploredNodes.Add(node);
            }
        }

        UpdateFloorFogTextures();
        UpdateFloorFogTextureArray();
    }
    private void CreateFloorFogTextures() 
    {
        foreach (int floorID in gridManager.GetFloorIDS()) 
        {
            Dictionary<Vector2Int, GridNode> floorGrid = gridManager.GetGrid(floorID);

            if (floorGrid == null || floorGrid.Count == 0) 
            {
                continue;
            }

            int width = 0;
            int height = 0;

            foreach (Vector2Int coords in floorGrid.Keys) 
            {
                width = Mathf.Max(width, coords.x + 1);
                height = Mathf.Max(height, coords.y + 1);
            }

            Texture2D fogTexture = new Texture2D(width, height, TextureFormat.R8, false);

            fogTexture.filterMode = FilterMode.Point;
            fogTexture.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++) 
            {
                pixels[i] = new Color32(0, 0, 0, 255);
            }

            fogTexture.SetPixels32(pixels);
            fogTexture.Apply();

            floorFogTextures.Add(floorID, fogTexture);

            GridNode reference = null;

            foreach(GridNode node in floorGrid.Values) 
            {
                reference = node;
                break;
            }

            if (reference != null) 
            {
                Vector3 floorOrigin = reference.cellWorldPosition - new Vector3(reference.coords.x * gridManager.GridCellSize, 0f 
                    , reference.coords.y * gridManager.GridCellSize);

                floorOrigins.Add(floorID, floorOrigin);
            }

            Debug.Log($"FogOfWar: Floor {floorID} texture size: {width}x{height}");
        }

        Debug.Log($"FogOfWar: Created {floorFogTextures.Count} floor textures");
        
    }
    
    private void UpdateFloorFogTextures() 
    {
        foreach (int floorID in gridManager.GetFloorIDS())
        {
            Dictionary<Vector2Int, GridNode> floorGrid = gridManager.GetGrid(floorID);

            if(floorGrid == null) 
            {
                continue;
            }

            Texture2D fogTexture = floorFogTextures[floorID];

            Color32[] pixels = new Color32[fogTexture.width * fogTexture.height];

            //Starts with every Pixel Unexplored
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 255);
            }

            foreach (GridNode node in floorGrid.Values) 
            {
                if (!exploredNodes.Contains(node)) 
                {
                    continue;
                }

                Vector2Int coords = node.coords;
                int pixelIndex = coords.y * fogTexture.width + coords.x;

                pixels[pixelIndex] = new Color32(255, 255, 255, 255);


            }

            fogTexture.SetPixels32(pixels);
            fogTexture.Apply();
            Debug.Log( $"Floor {floorID}: Explored nodes = " +$"{exploredNodes.Count(node => node.floorID == floorID)}");
        }
    }

    private void CreateFloorFogTextureArray()
    {
        int maxWidth = 0;
        int maxHeight = 0;

        foreach (Texture2D texture in floorFogTextures.Values) 
        {
            maxWidth = Mathf.Max(maxWidth, texture.width);
            maxHeight = Mathf.Max(maxHeight, texture.height);
        }

        int floorCount = floorFogTextures.Count;

        if (floorCount == 0) 
        {
            return;
        }

        floorFogTextureArray = new Texture2DArray(maxWidth, maxHeight, floorCount, TextureFormat.RGBA32, false, true);

        floorFogTextureArray.filterMode = FilterMode.Point;
        floorFogTextureArray.wrapMode = TextureWrapMode.Clamp;

        floorTextureIndices.Clear();

        int sliceIndex = 0;

        foreach (int floorID in gridManager.GetFloorIDS()) 
        {
            if (!floorFogTextures.TryGetValue(floorID, out Texture2D source)) 
            {
                continue;
            }

            Color32[] sourcePixels = source.GetPixels32();
            Color32[] arrayPixels = new Color32[maxWidth * maxHeight];
   
            //Initializes the slice as unexplored
            for (int i = 0; i < arrayPixels.Length; i++) 
            {
                arrayPixels[i] = new Color32(0, 0, 0, 255);
            }

            //Copy the floor texture into the lower-left portion.

            for (int y = 0; y < source.height; y++) 
            {
                for(int x = 0; x  < source.width; x++) 
                {
                    int sourceIndex = y * source.width + x;
                    int targetIndex = y * maxWidth + x;

                    arrayPixels[targetIndex] = sourcePixels[sourceIndex];
                }
            }

            floorFogTextureArray.SetPixels32(arrayPixels, sliceIndex);

            floorTextureIndices.Add(floorID, sliceIndex);

            sliceIndex++;

            
        }

        floorFogTextureArray.Apply();
        Debug.Log($"FogOfWar: Created texture array with {sliceIndex} slices " +$"({maxWidth}x{maxHeight}).");
    }

    private void UpdateFloorFogTextureArray() 
    {
        foreach(int floorID in gridManager.GetFloorIDS()) 
        {
            if (!floorTextureIndices.TryGetValue(floorID, out int sliceIndex))
            {
                continue;
            }
            if (!floorFogTextures.TryGetValue(floorID, out Texture2D source)) 
            {
                continue;
            }

            Color32[] sourcePixels = source.GetPixels32();

            Color32[] arrayPixels = new Color32[floorFogTextureArray.width *floorFogTextureArray.height];

            for (int i = 0; i < arrayPixels.Length; i++)
            { 
                arrayPixels[i] = new Color32(0, 0, 0, 255); 
            }

            for (int y = 0; y < source.height; y++)
            {
                for (int x = 0; x < source.width; x++)
                {
                    int sourceIndex = y * source.width + x;
                    int targetIndex =
                        y * floorFogTextureArray.width + x;

                    arrayPixels[targetIndex] = sourcePixels[sourceIndex];
                }
            }

            floorFogTextureArray.SetPixels32(arrayPixels,sliceIndex);
        }

        floorFogTextureArray.Apply();
    }
    private void CreateFloorMetaDataTexture() 
    {
        int floorCount = floorFogTextures.Count;

        if (floorCount == 0)
            return;

        floorMetaDataTexture = new Texture2D( 2,floorCount,TextureFormat.RGBAFloat,false, true);

        floorMetaDataTexture.filterMode = FilterMode.Point;
        floorMetaDataTexture.wrapMode = TextureWrapMode.Clamp;

        foreach (int floorID in gridManager.GetFloorIDS())
        {
            if (!floorTextureIndices.TryGetValue(floorID, out int index))
            { 
                continue;
            }

            if (!floorOrigins.TryGetValue(floorID, out Vector3 origin))
            {
                continue;
            }
            Texture2D floorTexture = floorFogTextures[floorID];

            // Pixel 0: world origin and texture-array index.
            floorMetaDataTexture.SetPixel( 0,index,new Color(origin.x,origin.y,origin.z,index));

            // Pixel 1: texture dimensions.
            floorMetaDataTexture.SetPixel(1, index, new Color(floorTexture.width, floorTexture.height, floorFogTextureArray.width, floorFogTextureArray.height));
        }

        floorMetaDataTexture.Apply();

        Debug.Log(
            $"FogOfWar: Created metadata texture for {floorCount} floors."
        );
    }
    
    private void InitializeFogOfWar() 
    {
        if(gridManager == null) 
        {
            Debug.LogWarning("FogOfWarManager: GridManager reference is missing");
            return;
        }

        CreateFloorFogTextures();

        exploredNodes.Clear();
        visitedNodes.Clear();

        Debug.Log("FogOfWar: initialized with all nodes unexplored");
    }


    public FogState GetFogState(GridNode node) 
    {
        if (node == null) 
        {
            return FogState.DARK;
        }

        return exploredNodes.Contains(node) ? FogState.CLEAR : FogState.DARK;
    }
}
