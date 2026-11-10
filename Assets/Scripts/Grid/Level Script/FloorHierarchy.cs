using UnityEngine;

public enum FloorHierarchyRole 
{
    FloorSlice,
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
    [SerializeField]private FloorHierarchyRole role;

    [Header("Grid Information")]
    [SerializeField]private int sliceX;

    public FloorHierarchyRole Role => role;
    public int SliceX => sliceX;
}
