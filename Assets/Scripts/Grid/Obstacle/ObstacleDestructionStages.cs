using UnityEngine;

[System.Serializable]
public class ObstacleDestructionStages : MonoBehaviour
{
    [Header("Name of the Obstacles Destruction Stage")]
    public ObstacleCondition stageName;

    [Header("PercentBassed Threshold")]
    [Tooltip("Minimum health percentage required to use this stage. " +
             "For example, 0.75 means 75% health.")]
    [Range(0f, 1f)]
    public float healthThreshold;

    [Header("Visual")]
    public GameObject stageVisual;

    [Header("Movement")]
    public bool blockMovement;
    [Header("Line OF Sight")]
    public bool blockLineofSight;
    [Header("ProjectileCollision")]
    public bool blockProjectile;

    [Header("Cover")]
    public CoverState coverState;
    [Min(0f)] public float obstacleHeight;
}
