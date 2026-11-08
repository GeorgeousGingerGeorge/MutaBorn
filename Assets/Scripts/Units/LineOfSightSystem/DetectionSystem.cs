using System.Collections.Generic;
using Unity.Collections.Tests.CoreCLR.TestJobs;
using UnityEngine;
using UnityEngine.Rendering;


public class DetectionSystem : MonoBehaviour
{

    [SerializeField] private LineOfSight lineOfSight;
    [SerializeField] private FogOfWarManager fogController;
    [SerializeField] private Occupancy occupancy;
    private Dictionary<string, HashSet<UnitController>> detectedByFaction = new Dictionary<string, HashSet<UnitController>>();
    private Dictionary<string, Dictionary<UnitController, GridNode>> lastKnownLocations = new Dictionary<string, Dictionary<UnitController, GridNode>>();

    private Dictionary<string, HashSet<UnitController>> visibleByFaction = new Dictionary<string, HashSet<UnitController>>();

    public IReadOnlyDictionary <string, HashSet<UnitController>> DetectedUnits => detectedByFaction;



    //=====================================
    //
    // Unit Detection Update
    //
    //=====================================

    public void UpdateDetection() //Player Faction Detection
    {
        if (lineOfSight == null || fogController == null || occupancy == null)
        {
            Debug.LogWarning("DetectionSystem: Either LineOfSight, FogOfWarManager or Occupancy is Null-Referenced");
            return;
        }

        if (!detectedByFaction.TryGetValue("playerFaction", out HashSet<UnitController> detectedUnits))
        {
            detectedUnits = new HashSet<UnitController>();
            detectedByFaction.Add("playerFaction", detectedUnits);
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
    public void UpdateAIDetection(string observerFactionID) //AI Detection
    {
        if (lineOfSight == null || occupancy == null) 
        {
            Debug.LogWarning("DetectionSystem: Either LineOfSight, FogOfWarManager or Occupancy is Null-Referenced");
            return;
        }

        if (!detectedByFaction.TryGetValue(observerFactionID, out HashSet<UnitController> detectedUnits)) 
        {
            detectedUnits = new HashSet<UnitController>();
            detectedByFaction.Add(observerFactionID, detectedUnits);
        }
        if (!visibleByFaction.TryGetValue(observerFactionID, out HashSet<UnitController> visibleUnits)) 
        {
            visibleUnits = new HashSet<UnitController>();
            visibleByFaction.Add(observerFactionID, visibleUnits);
        }
        if (!lastKnownLocations.TryGetValue(observerFactionID, out Dictionary<UnitController, GridNode> locations))
        {
            locations = new Dictionary<UnitController, GridNode>();
            lastKnownLocations.Add(observerFactionID, locations);
        }

        HashSet<UnitController> friendlyUnits = new HashSet<UnitController>();
        HashSet<UnitController> otherUnits = new HashSet<UnitController>();


        foreach (UnitController unit in occupancy.GetUnits()) 
        {

            UnitFaction faction = unit.GetComponent<UnitFaction>();
            if (faction == null) 
            {
                continue;
            }

            if (faction.GetFactionID() == observerFactionID)
            {
                friendlyUnits.Add(unit);
            }
            else 
            {
                otherUnits.Add(unit);
            }
        }

        //Removed Detected Units that have returned to the fog
        detectedUnits.RemoveWhere(unit => unit == null);
        visibleUnits.Clear();


        foreach (UnitController target in otherUnits) 
        {
            if (target == null || target.currentNode == null) 
            {
                continue;
            }

            foreach (UnitController observer in friendlyUnits) 
            {

                if (lineOfSight.HasPhysicalLineOfSight(observer, target)) 
                {
                    detectedUnits.Add(target);

                    visibleUnits.Add(target);

                    locations[target] = target.currentNode;
                    break;
                }
            }

        }
    }

    public GridNode GetLastKnownLocation(UnitController target, string observerFactionID) 
    {
        if (target == null) 
        {
            return null;
        }

        if (!lastKnownLocations.TryGetValue(observerFactionID, out Dictionary<UnitController, GridNode> locations)) 
        {
            return null;
        }

        locations.TryGetValue(target, out GridNode location);

        return location;
    }

    public void TryDetectUnit(UnitController target, string observerFactionID,float detectionChance) 
    {
       
        if (target == null )
        {
            return;
        }

     
        if (!detectedByFaction.TryGetValue(observerFactionID, out HashSet<UnitController> detectedUnits)) 
        {
            return;
        }

        if (detectedUnits.Contains(target)) 
        {
            return;
        }

        if (Random.Range(0f, 100f) < detectionChance) 
        {
            detectedUnits.Add(target);
        }
    }

    public bool IsCurrentlyVisible(UnitController unit, string observerFactionID) 
    {
        if(unit == null) 
        {
            return false;
        }

        return visibleByFaction.TryGetValue(observerFactionID, out HashSet<UnitController> visibleUnits) && visibleUnits.Contains(unit);
    }
    


    //======================================
    //
    // Unit Removal- If unit becomes invisible
    //
    //======================================

    public void RemoveDetectedUnit(UnitController unit, string factionID) 
    {
        if (unit == null) 
        {
            return;
        }
        if (!detectedByFaction.TryGetValue(factionID, out HashSet<UnitController> detectedUnits))
        {
            return;
        }

        if (detectedUnits.Contains(unit)) 
        {
            detectedUnits.Remove(unit);
        }
    }

    //======================================
    //
    //  Unit Removal- if unit enters fog
    //
    //======================================

    public void RemoveUndetectedUnit() 
    {
        if (!detectedByFaction.TryGetValue("playerFaction", out HashSet<UnitController> detectedUnits))
        {
            return;
        }

        detectedUnits.RemoveWhere(unit => unit == null || fogController.GetFogState(unit.currentNode) == FogState.DARK);
    }


    //======================================
    //
    //  Detection Query- Checks if there's a unit in front
    //
    //======================================


    public bool isDetected(UnitController unit, string observerFactionID) 
    {
        if (unit == null) 
        {
            return false;
        }

        if (!detectedByFaction.TryGetValue(observerFactionID, out HashSet<UnitController> detectedUnits))
        {
            return false;
        }

        return detectedUnits.Contains(unit);
    }
}
