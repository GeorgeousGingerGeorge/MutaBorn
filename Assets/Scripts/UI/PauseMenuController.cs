using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] string MainMenuScene = "";
    [SerializeField] string HubScene = "";

    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject quitMenu;
    [SerializeField] private GameObject quitGame;
    [SerializeField] private GameObject saveButton;
    [SerializeField] private GameObject loadButton;

    private bool isPaused = false;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    private void PauseGame()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void ReturnToHub()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(HubScene);
    }

    public void OpenQuitMenu()
    {
        pauseMenu.SetActive(false);
        quitMenu.SetActive(true);
    }

    public void OptionsMenu()
    {
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void BackToPauseMenu()
    {
        optionsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }

    public void SaveGame()
    {
        bool success = SaveManager.SaveCurrentMission();

        Debug.Log(success ? "Mission save test successful!!! :D" : "Mission save test failed!! :(");
    }

    public void LoadGame()
    {
        Debug.Log("Game Loadedd :D Have fun!");
        if (!SaveManager.TryLoadMission(out MissionSaveData saved))
        {
            Debug.LogError("No valid mission save found!");
            return;
        }

        TurnManager turnManager = TurnManager.Instance;
        GridManager gridManager = FindAnyObjectByType<GridManager>();

        MissionObjectiveManager objectiveManager = MissionObjectiveManager.Instance;

        if (objectiveManager == null || saved.objectiveState == null)
        {
            Debug.LogError("Cannot restore mission: Objective state or manager missing.");
            return;
        }

        if (turnManager == null || gridManager == null || saved.turnState == null || turnManager.PlayerFaction == null)
        {
            Debug.LogError("Missing required mission restoration components!");
            return;
        }

        if (!turnManager.IsPlayerTurn || !string.Equals(saved.turnState.activeFactionId, turnManager.PlayerFaction.Id, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("Mission restoration currently supports player turns only.");
            return;
        }

        // Restore all saved unit states.
        if (!MissionSaveRestore.TryRestoreAllPositions(saved, gridManager))
        {
            Debug.LogError("Mission restoration failed during unit restoration!");
            return;
        }

        // Restore saved turn progression.
        if (!turnManager.TryRestoreTurnState(saved.turnState))
        {
            Debug.LogError("Turn restoration failed! Unit states were restored, but the mission is only partially restored.");
            return;
        }
        // Restore objective progression
        if (!objectiveManager.TryRestoreObjectiveState(saved.objectiveState))
        {
            Debug.LogError("Objective restoration failed! Unit and turn states were restored, but the mission is only partially restored.");
            return;
        }

        Debug.Log($"Mission Snapshot Restored Successfully! Round: {turnManager.Round} | Active Faction: {turnManager.ActiveFaction.Id}");
    }

    public void QuitGame()
    {
        Debug.Log("Gooooodbyeeeee :(");
        Application.Quit();
    }
}
