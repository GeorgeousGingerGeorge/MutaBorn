using UnityEngine;



[CreateAssetMenu(fileName = "ObstacleData", menuName = "Scriptable Objects/ObstacleData")]
public class ObstacleData : ScriptableObject
{
    [Header("Identity")]
    public string obstacleName;
    public ObstacleType typeOfObstacle;

    [Header("Movement")]
    public bool blocksMovement = true;
    [Header("Line of Sight")]
    public bool blocksLineOfSight = true;

    [Header("Cover")]
    public CoverState coverState;
    public CoverDirections directions;

    [Header("Destuction")]
    public bool isDestructible;
    public int maxHealth;


}
