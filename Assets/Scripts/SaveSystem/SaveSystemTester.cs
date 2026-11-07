using UnityEngine;

public class SaveSystemTester : MonoBehaviour
{
    [ContextMenu("Test / Save Mission")]
    private void TestSaveMission()
    {
        bool success = SaveManager.SaveCurrentMission();

        Debug.Log(success ? "Mission save test successful!!! :D" : "Mission save test failed!! :(");
    }
}