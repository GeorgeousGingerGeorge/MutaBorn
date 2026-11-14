using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelGenerationProfile", menuName = "Scriptable Objects/LevelGenerationProfile")]
public class LevelGenerationProfile : ScriptableObject
{
    [Header("Generation Seed")]
    [SerializeField] private bool randomizedSeed = true;
    [SerializeField] private int seed = 12345;

    [Header("Starting Chunk")]
    [SerializeField] private ChunkDefinition entranceChunk;
    [SerializeField] private ChunkDefinition evacuationChunk;

    [Header("AdditionalChunks")]
    [SerializeField] private List<ChunkDefinition> availableChunks;

    [Header("Level Size (Grid Cells)")]
    [SerializeField] private Vector2Int maximumLevelDimensions = new Vector2Int(40, 40);


    [Header("Generation Limits")]

    [SerializeField] private int minimumChunks = 5;
    [SerializeField] private int maximumChunks = 15;


    [SerializeField] private int maximumGridCells = 800;
    [SerializeField] private int maximumLogicalFloors = 3;

    //Gettes

    public bool RandomizedSeed => randomizedSeed;
    public int Seed => seed;
    public ChunkDefinition EntranceChunk => entranceChunk;
    public ChunkDefinition EvacuationChunk => evacuationChunk;

    public List<ChunkDefinition> AvailableChunks => availableChunks;

    public Vector2Int MaximumLevelDimension => maximumLevelDimensions;

    public int MinimumChunks => minimumChunks;
    public int MaximumChunks => maximumChunks;
    public int MaximumGridCells => maximumGridCells;
    public int MaximumLogicalFloors => maximumLogicalFloors;


}
