using UnityEngine;

public enum FloorHierarchyRole 
{
    GridSlice,
    Environment,
    ObstacleContainer,
    ConnectorContainer,
    SpawnArea,
    UnitContainer,
    Other
}
public class FloorHierarchy : MonoBehaviour
{
    [Header("Hierarchy Role")]
    [SerializeField]private FloorHierarchy role;

    [Header("Grid Information")]
    [SerializeField]private int sliceX;

    public FloorHierarchy Role => role;
    public int SliceX => sliceX;
}
