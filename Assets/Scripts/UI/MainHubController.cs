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
        startMissionButton.onClick.AddListener(TestLevel);
    }


    void TestLevel()
    {
        Debug.Log("Mission Starting....");
        SceneManager.LoadScene("Main Test Scene");
    }
}
