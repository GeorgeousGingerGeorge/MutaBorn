using UnityEngine;

public class VisibilityManager : MonoBehaviour
{
    [SerializeField] private FogOfWarManager fogController;
    [SerializeField] private DetectionSystem detection;
    [SerializeField] private StealthSystem stealth;
    


    public void UpdateVisibility() 
    {
        if (fogController == null || detection == null) 
        {
            Debug.LogWarning("Visibility Manager: Missing Dependencies");
            return; 
        }

        fogController.UpdateFriendlyUnitVisibility();
        detection.UpdateDetection();
        
        
    }
    public void UpdateAICurrentDetection() 
    {
        detection.UpdateAIDetection("raiderFaction");
        detection.UpdateAIDetection("civilians");
    }

    public void OnInvisibiltyStarted(UnitController unit, string observerFactionID) 
    {
        if (unit == null || stealth == null || detection == null) 
        {
            return;
        }

        if (stealth.IsUnitInvisible(unit)) 
        {
            detection.RemoveDetectedUnit(unit, observerFactionID);
        }
        
    }

    public void OnInvisibilityEnded() 
    {

        UpdateVisibility();
    }
}
