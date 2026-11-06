using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainHubController : MonoBehaviour
{
    public Button startMissionButton;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("MainHubController initialized");
        if (startMissionButton != null)
        {
            startMissionButton.onClick.AddListener(TestLevel);

        }
    }


    void TestLevel()
    {
        CrewMissionMenu crewMenu = FindAnyObjectByType<CrewMissionMenu>(FindObjectsInactive.Include);
        if (crewMenu != null)
        {
            crewMenu.OpenMissionSelect();

            return;

        }

        // Existing test-only Hub scenes without a Crew menu still work.
        Debug.LogWarning("No CrewMissionMenu found; using legacy direct test-scene route.", this);
        SceneManager.LoadScene("Main Test Scene");
    }
}
