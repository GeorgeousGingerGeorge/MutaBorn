using UnityEngine;
using System.Collections.Generic;
using Unity.Collections.Tests.CoreCLR.TestJobs;


[CreateAssetMenu(fileName = "ConnectorDefinition", menuName ="Scriptable Objects/Connector Definition")]
public class ConnectorDefinition : ScriptableObject
{
    [Header("Connector Identity")]
    [SerializeField] private string connectorID;

    [Header("Prefab Socket")]
    [SerializeField] private GameObject connectorPrefab;

    [Header("Selection")]
    [SerializeField, Min(0f)] private float selectionWeight = 1f;

    [Header("Placement")]
    [SerializeField] private bool allowRotation = true;



    //Getters

    public string ConnectorID => connectorID;
    public GameObject ConnectorPrefab => connectorPrefab;
    public float SelectionWeight => selectionWeight;
    public bool AllowRotation => allowRotation;

    public FloorConnector Connector => connectorPrefab != null ? connectorPrefab.GetComponent<FloorConnector>() : null;

}
