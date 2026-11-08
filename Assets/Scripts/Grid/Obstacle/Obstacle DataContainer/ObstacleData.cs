
using UnityEngine;




[CreateAssetMenu(fileName = "ObstacleData", menuName = "Scriptable Objects/ObstacleData")]
public class ObstacleData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string ObstacleName;
    [SerializeField] private ObstacleType TypeOfObstacle;

    [Header("Movement")]
    [SerializeField] private bool BlocksMovement = true;
    [Header("Line of Sight")]
    [SerializeField] private bool BlocksLineOfSight = true;

    [Header("Projectile Collision")]
    [SerializeField] private bool BlocksProjectile = true;

    [Header("Traversal")]
    [SerializeField] private bool CanBeVaulted;
    [SerializeField] private bool CanBeClimbed;

    [Header("Physical Cover")]
    [SerializeField] private CoverState coverstate;
    [SerializeField, Min(0f)] private float heightOfCover;

    [Header("Destuction")]

    [SerializeField] private ObstacleDestructionStages[] DestructionStages;
    [SerializeField] private bool IsDestructible;
    [SerializeField] private int MaxHealth;




    [Header("Read-Only values")]

    public string obstacleName => ObstacleName;
    public ObstacleType typeOfObstacle => TypeOfObstacle;
    public bool blocksMovement => BlocksMovement;
    public bool blocksLineOfSight => BlocksLineOfSight;
    public bool blocksProjectile => BlocksProjectile;
    public bool canBeVaulted => CanBeVaulted;
    public bool canBeClimbed => CanBeClimbed;
    public CoverState coverState => coverstate;
    public float coverHeight => heightOfCover;
    public bool isDestructible => IsDestructible;
    public int maxHealth => MaxHealth;
    public ObstacleDestructionStages[] destructionStages => DestructionStages;
}
