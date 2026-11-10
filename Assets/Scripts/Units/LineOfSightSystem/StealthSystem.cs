using MutaBorn.Gameplay;
using System.Collections.Generic;
using UnityEngine;
public enum ConcealmentType 
{
    Invisibility, //Current Implementation
    Smoke, //Future Implementation
    Camouflage, 
    Hiding, //Current Implementation
    Crouching //Current Implementation
}
public class StealthSystem : MonoBehaviour
{
    [SerializeField] private LineOfSight lineOfSight;
    private HashSet<UnitController> hidingUnits = new HashSet<UnitController>();



    public bool CanBeDetected(UnitController observer, UnitController target) 
    {
        if (observer == null || target == null) 
        {
            return false;
        }

        if (!lineOfSight.CanSee(observer, target)) 
        {
            return false;
        }

        float chance = CalculateDetectionChance(observer, target);


        return Random.Range(0f, 100f) < chance; 

      
    }
    public float CalculateDetectionChance(UnitController observer, UnitController target) 
    {

        if (observer == null || target == null) 
        {
            return 0f; 
        }

        if (IsInvisible(target)) 
        {
            return 0f; 
        }

        float perception = observer.data.totalPerception;
        float detectionChance = perception * 2f;

        if (IsCrouching(target)) 
        {
            detectionChance -= 15f;
        }

        if (IsHiding(target)) 
        {
            detectionChance -= 30f;
        }

        if (IsCamouflage(target)) 
        {
            detectionChance -= 25f;
        }

        if (IsInSmoke(target)) 
        {
            detectionChance -= 40f;
        }


        return Mathf.Clamp(detectionChance, 0f, 100f);
    }
    public bool ShouldRemoveDetection(UnitController target) 
    {
        if (target == null) 
        {
            return true;
        }

        return IsInvisible(target);
    }
    //Cannot be Targeted by direct attacks, but can still be hit by area of effect
    private bool IsInvisible(UnitController target) 
    {

        if (target == null) 
        {
            return false;
        }

        UnitStatusController targetStatus = target.GetComponent<UnitStatusController>();

        if (targetStatus == null) 
        {
            return false;
        }

        return targetStatus.HasControl(MutaBorn.Gameplay.StatusControlFlags.Invisible);
    }
    public bool IsUnitInvisible(UnitController unit) => IsInvisible(unit);

    //Visibility will be Obstructed, however can still be hit by area of effects and direct attacks
    //Accuracy will be reduced 
    private bool IsInSmoke(UnitController unit) 
    {
        //Checks if unit is inside smoke
        return false; 
    }

    //Harder To detect
    private bool IsCamouflage(UnitController unit) 
    {
        //Checks if it has active camouflage
        return false;
    }
    //Concealed until discovered or exposed
    private bool IsHiding(UnitController unit) 
    {
        //Checks if the unit is actively hiding
        return hidingUnits.Contains(unit);
    }

    //Reduces Exposure, but does not automatically conceals
    private bool IsCovered(UnitController unit) 
    {
        return false;
    }

    private bool IsCrouching(UnitController unit) 
    {
        
        if (unit == null) 
        {
            return false;
        }

        return unit.currentStance == UnitStance.Crouching;
    }

    public void StartHiding(UnitController unit) 
    {
        if (unit == null) 
        {
            return;
        }
        hidingUnits.Add(unit);
    }
    public void StopHiding(UnitController unit) 
    {
        if (unit == null) 
        {
            return;
        }

        
        hidingUnits.Remove(unit);
    }
    
}
