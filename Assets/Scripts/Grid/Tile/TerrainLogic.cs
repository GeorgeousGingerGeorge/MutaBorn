using UnityEngine;

public class TerrainLogic : MonoBehaviour
{


    public static float GetTerrainMovementCost(TerrainType type) 
    {
        float movementCost;
        switch (type) 
        {
            case TerrainType.OpenGround:
                movementCost = 0f; 
                break;
            case TerrainType.HighGrass:
                movementCost = 1f;
                break;
            case TerrainType.Rubble:
                movementCost = 1f;
                break;
            case TerrainType.Mud:
                movementCost = 2f;
                break;
            default:
                movementCost = 0f;
                break;
        }

        return movementCost; 
    }
    public static bool CheckTerrainType(TerrainType type) 
    {
        bool isTraversable = false;

        switch (type) 
        {
            case TerrainType.OpenGround:
            case TerrainType.HighGrass:
            case TerrainType.Rubble:
            case TerrainType.Mud:
                isTraversable = true;
                break;
            default:
                isTraversable = false;
                break;
        }




        return isTraversable;
    }
}
