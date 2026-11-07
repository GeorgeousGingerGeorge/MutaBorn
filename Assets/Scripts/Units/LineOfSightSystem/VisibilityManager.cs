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

    public void OnInvisibiltyStarted(UnitController unit) 
    {
        if (unit == null || stealth == null || detection == null) 
        {
            return;
        }

        if (stealth.IsUnitInvisible(unit)) 
        {
            detection.RemoveDetectedUnit(unit);
        }
        
    }

    public void OnInvisibilityEnded() 
    {

        UpdateVisibility();
    }
}
