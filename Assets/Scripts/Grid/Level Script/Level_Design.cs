using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct PlacedFloor
{
   [Header("Level Position and Size")]
   [SerializeField] public FloorIdentification floorIndex;
}
[CreateAssetMenu(fileName = "Level_Design", menuName = "Scriptable Objects/Level_Design")]
public class Level_Design : ScriptableObject
{
    [Header("Level")]
    [SerializeField] public GameObject levelPrefab;
    [Header("Level Name")]
    [SerializeField] public string levelName; 

    [Header("Floor/WorldPosition/Size")]
    [SerializeField] public List<PlacedFloor> placedFloors;
   
}
