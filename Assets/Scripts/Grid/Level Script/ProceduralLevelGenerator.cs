using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection.Metadata.Ecma335;
using Unity.VisualScripting;

public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Generation Profile")]
    [SerializeField] private LevelGenerationProfile generationProfile;

    [Header("Generation Profile")]
    [SerializeField, Min(1)] private int allocationAttempts = 25;

    private System.Random random;

    public GeneratedLevelLayout generatedLayout { get; private set; }

    public int CurrentSeed { get; private set; }

    public List<ChunkPlacementCandidate> placementCandidates = new List<ChunkPlacementCandidate>();

    
    
    //Starts Generation

    public bool TryGeneratInitialLayout(out GeneratedLevelLayout layout) 
    {
        layout = null;

        if (!ValidateProfile()) 
        {
            return false;
        }

        CurrentSeed = generationProfile.RandomizedSeed ? unchecked((int)DateTime.UtcNow.Ticks) : generationProfile.Seed;

        random = new System.Random(CurrentSeed);

        generatedLayout = new GeneratedLevelLayout { seed = CurrentSeed };

        if (!TryCreateChunk(generationProfile.EntranceChunk, Vector2Int.zero, out ChunkInstance entranceChunk)) 
        {
            Debug.Log($"Failed to create starting Chunk");
            return false;
        }

        generatedLayout.chunks.Add(entranceChunk);

        layout = generatedLayout;
        Debug.Log($"Initial chunk Created Successfully. Seed: {CurrentSeed}");

        return true;

    }

   
    //Creates ChunkInstance
    private bool TryCreateChunk(ChunkDefinition definition, Vector2Int chunkOrigin, out ChunkInstance chunk) 
    {
        chunk = null;

        if (definition == null) 
        {
            return false;
        }

        for (int attempt = 0; attempt < allocationAttempts; attempt++) 
        {
            Vector2Int dimensions = GenerateChunkDimensions(definition);

            if (!TrySelectPrefab(definition, dimensions, out PrefabDefinition selectPrefab, out int rotation, out Vector2Int prefabFootprint)) 
            {
                continue;
            }

            Vector2Int prefabOffset = new Vector2Int((dimensions.x - prefabFootprint.x) / 2, (dimensions.y - prefabFootprint.y) / 2);
            chunk = new ChunkInstance
            {
                chunkDefinition = definition,
                prefabDefinition = selectPrefab,
                chunkGridOrigin = chunkOrigin,
                allocatedDimensions = dimensions,
                prefabGridOffset = prefabOffset,
                rotationDegrees = rotation,
                prefabInstance = null,
                floors = new List<ChunkFloorInstance>()
            };
            return true;
        }

        Debug.Log($"Couldn't Find Compatible Prefab for chunk" + $"{definition.ChunkID} after {allocationAttempts} attempts");
        return false;
    }

    //Gets the Dimensions of that current chunk
    private Vector2Int GenerateChunkDimensions(ChunkDefinition definition) 
    {
        Vector2Int minimum = definition.MinimumDimensions;
        Vector2Int maximum = definition.MaximumDimensions;


        int x = random.Next(minimum.x, maximum.x + 1);
        int z = random.Next(minimum.y, maximum.y + 1);

        return new Vector2Int(x, z);

    }

    //Goes through all available prefabs for this chunk definition and determines if its correct for it
    private bool TrySelectPrefab(ChunkDefinition definition, Vector2Int allocatedDimensions,
        out PrefabDefinition selectPrefab, out int selectedRotation, out Vector2Int selectedFootprint)
    {
        selectPrefab = null;
        selectedRotation = 0;
        selectedFootprint = Vector2Int.zero;

        List<PrefabDefinition> candidates = new List<PrefabDefinition>();
        List<List<int>> candidateRotations = new List<List<int>>();

        foreach (PrefabDefinition prefab in definition.AllowedPrefabs)
        {
            if (prefab == null || prefab.Prefab == null) 
            {
                continue;
            }

            if (prefab.Floors == null || prefab.FloorCount < definition.MinimumFloors || prefab.FloorCount > definition.MaximumFloors) 
            {
                continue;
            }

            List<int> validRotations = new List<int>();

            if (TryGetPrefabFootPrint(prefab, allocatedDimensions, 0, out Vector2Int footprint)) 
            {
                validRotations.Add(0);
            }

            if (prefab.AllowRotation && TryGetPrefabFootPrint(prefab, allocatedDimensions, 90, out footprint)) 
            {
                validRotations.Add(90);
            }

            if (validRotations.Count == 0) 
            {
                continue;
            }

            if (prefab.SelectionWeight <= 0f) 
            {
                continue;
            }

            candidates.Add(prefab);
            candidateRotations.Add(validRotations);
        }

        if (candidates.Count == 0) 
        {
            return false;
        }

        float totalWeight = 0f;

        foreach(PrefabDefinition candidate in candidates) 
        {
            totalWeight += candidate.SelectionWeight;
        }

        float selection = (float)random.NextDouble() * totalWeight;
        int selectedIndex = 0;

        for (int i = 0; i < candidates.Count; i++) 
        {
            selection -= candidates[i].SelectionWeight;

            if (selection <= 0f) 
            {
                selectedIndex = i;
                break;
            }
        }

        selectPrefab = candidates[selectedIndex];

        List<int> rotations = candidateRotations[selectedIndex];

        selectedRotation = rotations[random.Next(0, rotations.Count)];

        TryGetPrefabFootPrint(selectPrefab, allocatedDimensions, selectedRotation, out selectedFootprint);

        return true;
    }

    //Verify Prefab Floor 
    public bool TryGetPrefabFootPrint(PrefabDefinition prefab, Vector2Int allocatedDimensions, int rotation, out Vector2Int footPrint) 
    {
        footPrint = Vector2Int.zero;

        int maximumX = 0;
        int maximumZ = 0;

        foreach (PrefabFloorDefinition floor in prefab.Floors) 
        {
            if (floor == null || floor.GridSize.x <= 0 || floor.GridSize.y <= 0)
            {
                return false;            
            }

            Vector2Int dimensions = floor.GridSize;
            if(rotation == 90) 
            {
                dimensions = new Vector2Int(floor.GridSize.y, floor.GridSize.x);


            }
            maximumX = Mathf.Max(maximumX, dimensions.x);
            maximumZ = Mathf.Max(maximumZ, dimensions.y);
        }

        footPrint = new Vector2Int(maximumX, maximumZ);

        return footPrint.x <= allocatedDimensions.x && footPrint.y <= allocatedDimensions.y;
    }

    private void RegisterPlacementCandidates(ChunkInstance chunk) 
    {
        float nextWeight = Mathf.Max(0f, chunk.generationWeight, chunk.chunkDefinition.InfluenceStrength);

        foreach(ChunkSocketInstance socket in chunk.sockets) 
        {
            if(socket == null || socket.isConnected) 
            {
                continue;
            }

            ChunkPlacementCandidate candidate = new ChunkPlacementCandidate(socket, nextWeight, 0);

            placementCandidates.Add(candidate);
        }
    }
    private bool ValidateProfile() 
    {
        if (generationProfile == null) 
        {

            Debug.Log($"Missing LevelGenerationProfile");
            return false;
        }

        if(generationProfile.EntranceChunk == null) 
        {
            Debug.Log($"LevelGenerationProfile has no startingChunk", this);
            return false;

        }

        if (generationProfile.MaxmimumLevelDimension.x <= 0 || generationProfile.MaxmimumLevelDimension.y <= 0) 
        {
            Debug.Log($"Invalid Maximum Level Dimensions", this);

            return false;
        }

        if (generationProfile.MaximumChunks < 1 || generationProfile.MinimumChunks > generationProfile.MaximumChunks) 
        {
            Debug.Log("Invalid Chunk Generation limits");
            return false;
        }

        return true;
    }

    [ContextMenu("Generate Initial Layout Test")]

    private void GenerateInitialLayoutTest() 
    {
        if (!TryGeneratInitialLayout(out GeneratedLevelLayout layout)) 
        {
            return;
        }

        ChunkInstance chunk = layout.chunks[0];

        Debug.Log(
            $"Initial Layout Test\n" +
            $"Seed: {layout.seed}\n" +
            $"Chunk: {chunk.chunkDefinition.ChunkID}\n" +
            $"Prefab: {chunk.prefabDefinition.PrefabID}\n" +
            $"Allocation: {chunk.allocatedDimensions}\n" +
            $"Origin: {chunk.chunkGridOrigin}\n", this);
    }
}
