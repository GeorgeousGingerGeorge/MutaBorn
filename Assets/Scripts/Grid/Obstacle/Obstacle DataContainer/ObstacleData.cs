
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

    [Header("Sound")]
    [SerializeField] private bool BlocksSound = true;
    [Header("Traversal")]
    [SerializeField] private bool CanBeVaulted;
    [SerializeField] private bool CanBeClimbed;

    [Header("Physical Cover")]
    [SerializeField] private CoverState coverstate;
    [SerializeField, Min(0f)] private float heightOfCover = 0.8f;

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
    public bool blocksSound => BlocksSound;
    public bool canBeVaulted => CanBeVaulted;
    public bool canBeClimbed => CanBeClimbed;
    public CoverState coverState => coverstate;
    public float coverHeight => heightOfCover;
    public bool isDestructible => IsDestructible;
    public int maxHealth => MaxHealth;
    public ObstacleDestructionStages[] destructionStages => DestructionStages;


    public void CreateObstacleData(string name = null, ObstacleType type = ObstacleType.None, bool movement = false,
        bool sight = false, bool projectiles = false, bool sound = false, bool vaulted = false, bool climbed = false, CoverState state = CoverState.NONE,
        float height = 0.8f, bool destructible = false, int health = 0, ObstacleDestructionStages[] stages = null)
    {

        ObstacleName = name;
        TypeOfObstacle = type;
        BlocksMovement = movement;
        BlocksLineOfSight = sight;
        BlocksProjectile = projectiles;
        BlocksSound = sound;
        CanBeVaulted = vaulted;
        CanBeClimbed = climbed;
        coverstate = state;
        heightOfCover = height;
        IsDestructible = destructible;
        MaxHealth = health;
        DestructionStages = stages;

    }
}
