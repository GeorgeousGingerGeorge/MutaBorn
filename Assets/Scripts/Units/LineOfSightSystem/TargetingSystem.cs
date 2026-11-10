using System.Collections.Generic;
using UnityEngine;

public class TargetingSystem : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private LineOfSight lineOfSight;
    [SerializeField] private DetectionSystem detectionSystem;
    [SerializeField] private StealthSystem stealthSystem;
    [SerializeField] private GridManager gridManager;

    [Header("Ground Target Validation")]

    [SerializeField, Min(0f)]
    private float groundTargetTolerance = 0.1f;

    public bool CanTargetUnit(UnitController attacker, UnitController target, float range, bool use3DDistance = false) 
    {
        if (attacker == null || target == null || attacker == target || range < 0f) 
        {
            return false;
        }

        if (lineOfSight == null || detectionSystem == null || stealthSystem == null) 
        {
            Debug.LogWarning("Targeting System: A single or all dependencies are missing");
            return false;
        }

        //Checks Faction
        if (!IsValidFactionTarget(attacker, target, out UnitFaction attackerFaction)) 
        {
            return false;
        }

        //Player Attacks Require the target to have been discovered by DetectionSystem
        if (attackerFaction.GetFactionID() == "playerFaction") 
        {
            if (!detectionSystem.isDetected(target, "playerFaction")) 
            {
                return false;
            }
            
        }
        else 
        {
            if (!detectionSystem.isDetected(target, attackerFaction.GetFactionID())) 
            {
                return false;
            }
        }

        //Invisible units cannot be directly targeted

        if (stealthSystem.IsUnitInvisible(target)) 
        {
            return false;
        }

        if (!lineOfSight.HasPhysicalLineOfSight(attacker, target)) 
        {
            return false;
        }

        return IsWithinRange(attacker.transform.position, target.transform.position, range, use3DDistance);


    }
   
    private bool IsValidFactionTarget(UnitController attacker, UnitController target, out UnitFaction attackerFaction)
    {
        attackerFaction = attacker.GetComponent<UnitFaction>();
        
        if (attacker == null || target == null)
        {
            return false;
        }

      
        UnitFaction targetFaction = target.GetComponent<UnitFaction>();
       

        if (attackerFaction == null || targetFaction == null) 
        {
            return false;
        }

        switch (attackerFaction.GetFactionID()) 
        {
            case "playerFaction":
                {
                    return targetFaction.GetFactionID() == "raiderFaction";
                }
            case "raiderFaction":
                { 
                    return targetFaction.GetFactionID() == "playerFaction" || targetFaction.GetFactionID() == "civilians";

                }
            case "civilians":
                {
                    return targetFaction.GetFactionID() == "raiderFaction";
                }
            default:

                Debug.LogWarning("TargetingSystem: FactionID was not a choice ");
                return false;
        }
    }
    public bool CanTargetGround(UnitController attacker, Vector3 targetPosition, int targetFloorID, float range, bool use3DDistance = false)
    {
        if (attacker == null || gridManager == null || range < 0f) 
        {
            return false;
        }


        Dictionary<Vector2Int, GridNode> floorGrid = gridManager.GetGrid(targetFloorID);

        if (floorGrid == null || floorGrid.Count == 0) 
        {
            return false;
        }

        //Find the Closest Node on the requested Floor only

        GridNode closestNode = null;
        float closestDistance = float.MaxValue;

        foreach (GridNode node in floorGrid.Values) 
        {
            float distance = Vector3.SqrMagnitude(targetPosition - node.cellWorldPosition);

            if (distance < closestDistance) 
            {
                closestDistance = distance;
                closestNode = node;
            }
        }
        if (closestNode == null) 
        {
            return false;
        }

        //Confirmation-TargetPosition falls within X/Z FootPrint

        Vector3 difference = targetPosition - closestNode.cellWorldPosition;

        float halfCell = gridManager.GridCellSize * 0.5f;

        if (Mathf.Abs(difference.x) > halfCell + groundTargetTolerance || Mathf.Abs(difference.z) > halfCell + groundTargetTolerance) 
        {
            return false;
        }

        return IsWithinRange(attacker.transform.position, targetPosition, range, use3DDistance);

    }

    private bool IsWithinRange(Vector3 origin, Vector3 destination, float range, bool use3DDistance) 
    {
        Vector3 difference = destination - origin;

        if (!use3DDistance) 
        {
            difference.y = 0f;
        }

        return difference.sqrMagnitude <= range * range; 
    }
}
