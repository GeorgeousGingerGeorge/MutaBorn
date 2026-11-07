using System.Collections.Generic;
using UnityEngine;

public class DetectionSystem : MonoBehaviour
{

    [SerializeField] private LineOfSight lineOfSight;
    [SerializeField] private FogOfWarManager fogController;
    [SerializeField] private Occupancy occupancy;
    private HashSet<UnitController> detectedUnits = new HashSet<UnitController>();

    public IReadOnlyCollection<UnitController> DetectedUnits => detectedUnits;



    //=====================================
    //
    // Unit Detection Update
    //
    //=====================================

    public void UpdateDetection() 
    {
        if (lineOfSight == null || fogController == null || occupancy == null) 
        {
            Debug.LogWarning("DetectionSystem: Either LineOfSight, FogOfWarManager or Occupancy is Null-Referenced");
            return;
        }

        HashSet<UnitController> friendlyUnits = new HashSet<UnitController>();
        HashSet<UnitController> otherUnits = new HashSet<UnitController>();


        foreach (UnitController unit in occupancy.GetUnits()) 
        {

            UnitFaction faction = unit.GetComponent<UnitFaction>();

            if (faction != null && faction.GetFactionID() == "playerFaction")
            {
                friendlyUnits.Add(unit);
            }
            else 
            {
                otherUnits.Add(unit);
            }
        }

        //Removed Detected Units that have returned to the fog
        RemoveUndetectedUnit();


        foreach (UnitController target in otherUnits) 
        {
            if (detectedUnits.Contains(target)) 
            {
                continue;
            }

            foreach (UnitController observer in friendlyUnits) 
            {

                if (lineOfSight.CanSee(observer, target)) 
                {
                    detectedUnits.Add(target);
                    break;
                }
            }

        }
    }










    //======================================
    //
    //  Unit Removal- if unit enters fog or stealth
    //
    //======================================

    public void RemoveUndetectedUnit() 
    {
        detectedUnits.RemoveWhere(unit => unit == null || fogController.GetFogState(unit.currentNode) == FogState.DARK);
    }


    //======================================
    //
    //  Detection Query- Checks if there's a unit in front
    //
    //======================================


    public bool isDetetected(UnitController unit) 
    {
        if (unit == null) 
        {
            return false;
        }

        return detectedUnits.Contains(unit);
    }
}
