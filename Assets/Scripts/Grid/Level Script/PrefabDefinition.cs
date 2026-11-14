using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PrefabDefinition", menuName = "Scriptable Objects/PrefabDefinition1")]
public class PrefabDefinition : ScriptableObject
{
    [Header("Prefab Identity")]
    [SerializeField] private string prefabID;

    [Header("Prefab Reference")]
    [SerializeField] private GameObject prefab;

    [Header("Prefab Selection")]
    [SerializeField, Min(0f)] private float selectionWeight = 1f;

    [Header("Placement")]
    [SerializeField] private bool allowRotation = true;

    [Header("Floor Definition")]
    [SerializeField] private List<PrefabFloorDefinition> floors = new List<PrefabFloorDefinition>();


    [Header("ReadOnly Entries")]
    public string PrefabID => prefabID;
    public GameObject Prefab => prefab;
    public float SelectionWeight => selectionWeight;
    public bool AllowRotation => allowRotation;
    public IReadOnlyList<PrefabFloorDefinition> Floors => floors;

    public int FloorCount => floors.Count;

    public List<PrefabSocket> GetPotentialSockets() 
    {
        List<PrefabSocket> sockets = new List<PrefabSocket>();

        foreach (PrefabFloorDefinition floor in floors) 
        {
            sockets.AddRange(floor.GeneratePerimeterSockets());
        }

        return sockets;
    }
}
