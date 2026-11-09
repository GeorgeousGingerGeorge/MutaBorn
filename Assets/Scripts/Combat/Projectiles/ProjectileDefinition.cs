using UnityEngine;

public enum ProjectileTrajectoryType
{
    Straight,
    Arc
}

[CreateAssetMenu(fileName = "NewProjectile", menuName = "Game/Projectiles/Projectile Definition")]
public class ProjectileDefinition : ScriptableObject
{
    [Header("Projectile Prefab")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Movement")]
    [SerializeField] private ProjectileTrajectoryType trajectoryType = ProjectileTrajectoryType.Straight;
    [SerializeField, Min(0.1f)] private float movementSpeed = 15f;
    [SerializeField, Min(0f)] private float arcHeight = 3f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers = ~0;

    public GameObject ProjectilePrefab => projectilePrefab;
    public ProjectileTrajectoryType TrajectoryType => trajectoryType;
    public float MovementSpeed => movementSpeed;
    public float ArcHeight => arcHeight;
    public LayerMask CollisionLayers => collisionLayers;
}