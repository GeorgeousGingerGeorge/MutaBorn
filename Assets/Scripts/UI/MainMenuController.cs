using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class MainMenuController : MonoBehaviour
{
    public Button testLevelButton;
    public Button optionsButton;
    public Button quitButton;
    public Button creditsButton;
    public Button  creditsBackButton; // <-- Found on the credits panel
    public GameObject creditsPanel;
    public GameObject canvasMainMenu; 
    public Button controlsButton;
    public Button controlsBackButton;
    public GameObject controlsPanel;
    public GameObject optionsPanel;
    public Button optionsBackButton;
    public Button loadButton;
    public string hubName;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        testLevelButton.onClick.AddListener(TestLevel);
        optionsButton.onClick.AddListener(OpenOptions);
        quitButton.onClick.AddListener(QuitGame);
        creditsButton.onClick.AddListener(OpenCredits);
        creditsBackButton.onClick.AddListener(CloseCredits);

        if (controlsButton != null)
        {
            controlsButton.onClick.AddListener(OpenControls);

        }

        if (controlsBackButton != null)
        {
            controlsBackButton.onClick.AddListener(CloseControls);

        }

        if (optionsBackButton != null)
        {
            optionsBackButton.onClick.AddListener(CloseOptions);
        }

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(LoadSavedMission);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void TestLevel()
    {
        AudioManager.Instance.PlaySound("Button Click");
        SceneManager.LoadScene(hubName);
    }

    void OpenOptions()
    {
        AudioManager.Instance.PlaySound("Button Click");

        Debug.Log("Options Selected");
        optionsPanel.SetActive(true);
        canvasMainMenu.SetActive(false);
    }

    void CloseOptions()
    {
        AudioManager.Instance.PlaySound("Button Click");

        Debug.Log("Options Menu Closing");
        optionsPanel.SetActive(false);
        canvasMainMenu?.SetActive(true);
    }

    void QuitGame()
    {
        AudioManager.Instance.PlaySound("Button Click");

        Debug.Log("Shutting Down... Goooodbye :(");
        Application.Quit();
    }

    void OpenCredits()
    {
        AudioManager.Instance.PlaySound("Button Click");

        Debug.Log("Credits Selected");
        creditsPanel.SetActive(true);
        canvasMainMenu.SetActive(false);
    }

    void CloseCredits()
    {
        AudioManager.Instance.PlaySound("Button Click");

        Debug.Log("Credits Panel Closing");
        creditsPanel.SetActive(false);
        canvasMainMenu.SetActive(true);
    }

    void OpenControls()
    {
        if (controlsPanel == null || canvasMainMenu == null)
        {
            Debug.LogWarning("Main Menu Controls panel or menu panel is not assigned.", this);

            return;

        }

        AudioManager.Instance.PlaySound("Button Click");

        controlsPanel.SetActive(true);
        canvasMainMenu.SetActive(false);

    }

    void CloseControls()
    {
        if (controlsPanel == null || canvasMainMenu == null)
        {
            Debug.LogWarning("Main Menu Controls panel or menu panel is not assigned.", this);

            return;

        }

        AudioManager.Instance.PlaySound("Button Click");

        controlsPanel.SetActive(false);
        canvasMainMenu.SetActive(true);

    }


    public void LoadSavedMission()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved))
        {
            Debug.LogError("[Save/Load] No valid mission save found!");
            return;
        }

        if (string.IsNullOrWhiteSpace(saved.sceneName) || !Application.CanStreamedLevelBeLoaded(saved.sceneName))
        {
            Debug.LogError($"[Save/Load] Saved mission scene is unavailable: {saved.sceneName}");
            return;
        }

        if (GameDataBaseLoader.Instance == null || GameDataBaseLoader.Instance.raceData == null)
        {
            Debug.LogError("[Save/Load] Game database is unavailable.");
            return;
        }

        // Restore the persistent roster before the mission scene initializes.
        UnitRoster roster = UnitRoster.Instance;
        bool createdRoster = false;

        if (roster == null)
        {
            GameObject rosterObject = new GameObject("Restored UnitRoster");
            roster = rosterObject.AddComponent<UnitRoster>();
            createdRoster = true;
        }

        if (roster == null || !roster.TryRestoreSavedRoster(saved.rosterState))
        {
            Debug.LogError("[Save/Load] Failed to restore the persistent roster.");

            if (createdRoster && roster != null)
            {
                Destroy(roster.gameObject);
            }

            return;
        }

        // The mission can now access the restored roster during initialization.
        SaveManager.PendingRestore = saved;

        Debug.Log($"[Save/Load] Roster restored. Loading saved mission: {saved.sceneName}");

        SceneManager.LoadScene(saved.sceneName);
    }

}
