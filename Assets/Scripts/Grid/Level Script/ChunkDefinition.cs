using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChunkDefinition", menuName = "Scriptable Objects/ChunkDefinition")]
public class ChunkDefinition : ScriptableObject
{
    [Header("Chunk Identity")]
    [SerializeField] private string chunkID;
    [SerializeField] private ChunkCategory category;

    [Header("Spawnable Area (Grid Cells)")]
    [SerializeField] private Vector2Int minimumDimensions;
    [SerializeField] private Vector2Int maximumDimensions;

    [Header("Prefab Selection")]
    [SerializeField] private List<PrefabDefinition> allowedPrefabs;

    [Header("Weight Requirements")]
    [SerializeField] private float minimumWeight;
    [SerializeField] private float maximumWeight;

    [Header("Neighbor Influence")]

    [SerializeField] private float influenceStrength;

    [Header("Prefab Floor Requirements")]
    [SerializeField] private int minimumFloors = 1;
    [SerializeField] private int maximumFloors = 3;


    public string ChunkID => chunkID;
    public ChunkCategory Category => category;
    public Vector2Int MinimumDimensions => minimumDimensions;
    public Vector2Int MaximumDimensions => maximumDimensions;
    public List<PrefabDefinition> AllowwedPrefabs => allowedPrefabs;
    public float MinimumWeight => minimumWeight;
    public float MaximumWeight => maximumWeight;

    public float InfluenceStrength => influenceStrength;
    public int MinimumFloors => minimumFloors;
    public int MaximumFloors => maximumFloors;

}
