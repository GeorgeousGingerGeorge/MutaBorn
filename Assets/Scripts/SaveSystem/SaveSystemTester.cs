using UnityEngine;

public class SaveSystemTester : MonoBehaviour
{
    [ContextMenu("Test / Save Mission")]
    private void TestSaveMission()
    {
        bool success = SaveManager.SaveCurrentMission();

        Debug.Log(success ? "Mission save test successful!!! :D" : "Mission save test failed!! :(");
    }

    [ContextMenu("Test / Load Mission")]
    private void TestLoadMission()
    {
        if (SaveManager.TryLoadMission(out MissionSaveData data))
        {
            Debug.Log($"Mission loaded successfully!! LET'S RUMBLE!!!!"); // debugger message to console
            Debug.Log($"Scene: {data.sceneName}"); // loaded scene
            Debug.Log($"Saved Units: {data.units?.Count ?? 0}"); // total units counted when attempting to LOAD the save file
            Debug.Log($"Save Time: {data.saveTimeUTC}"); // time stamp of the SAVED FILE
        }
        else
        {
            Debug.LogError("Mission load test failed! Maybe... Try that again.");
        }
    }
}