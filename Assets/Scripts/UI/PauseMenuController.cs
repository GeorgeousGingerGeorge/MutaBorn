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
    [SerializeField] private GameObject controlsMenu;
    private bool isPaused = false;

    private System.Collections.IEnumerator Start()
    {
        MissionSaveData pending = SaveManager.PendingRestore;

        if (pending == null)
        {
            yield break;
        }

        Debug.Log($"[Save/Load] Pending restore: {pending.sceneName} | Current scene: {SceneManager.GetActiveScene().name}");

        // Wait until the mission objectives have finished initializing.
        int framesWaited = 0;

        while (MissionObjectiveManager.Instance != null && MissionObjectiveManager.Instance.Status == MissionStatus.NotStarted && framesWaited < 120)
        {
            framesWaited++;
            yield return null;
        }

        if (MissionObjectiveManager.Instance == null || MissionObjectiveManager.Instance.Status != MissionStatus.InProgress)
        {
            Debug.LogError("[Save/Load] Mission objectives were not ready for restoration.");
            yield break;
        }


        if (TryApplyMissionSave(pending))
        {
            SaveManager.PendingRestore = null;
            Debug.Log("[Save/Load] Automatic mission restoration completed successfully!");
        }
        else
        {
            Debug.LogError("[Save/Load] Automatic mission restoration failed.");
        }
    }

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
        controlsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }

    public void SaveGame()
    {
        bool success = SaveManager.SaveCurrentMission();

        Debug.Log(success ? "Mission save test successful!!! :D" : "Mission save test failed!! :(");
    }


    public void LoadGame()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved))
        {
            Debug.LogError("[Save/Load] No valid mission save found!");
            return;
        }

        TryApplyMissionSave(saved);
    }

    private bool TryApplyMissionSave(MissionSaveData saved)
    {
        if (saved == null || !string.Equals(saved.sceneName, SceneManager.GetActiveScene().name, System.StringComparison.Ordinal))
        {
            Debug.LogError("[Save/Load] Saved mission does not match the current scene.");
            return false;
        }

        TurnManager turnManager = TurnManager.Instance;
        GridManager gridManager = FindAnyObjectByType<GridManager>();
        MissionObjectiveManager objectiveManager = MissionObjectiveManager.Instance;

        if (turnManager == null || gridManager == null || objectiveManager == null || saved.turnState == null || saved.objectiveState == null || turnManager.PlayerFaction == null)
        {
            Debug.LogError("[Save/Load] Required mission restoration components are unavailable.");
            return false;
        }

        if (!turnManager.IsPlayerTurn || !string.Equals(saved.turnState.activeFactionId, turnManager.PlayerFaction.Id, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("[Save/Load] Mission restoration currently supports player turns only.");
            return false;
        }

        if (!MissionSaveRestore.TryRestoreAllPositions(saved, gridManager))
        {
            Debug.LogError("[Save/Load] Unit restoration failed.");
            return false;
        }

        if (!turnManager.TryRestoreTurnState(saved.turnState))
        {
            Debug.LogError("[Save/Load] Turn restoration failed. Mission may be partially restored.");
            return false;
        }

        if (!objectiveManager.TryRestoreObjectiveState(saved.objectiveState))
        {
            Debug.LogError("[Save/Load] Objective restoration failed. Mission may be partially restored.");
            return false;
        }

        Debug.Log($"[Save/Load] Mission restored successfully! Round: {turnManager.Round} | Active Faction: {turnManager.ActiveFaction.Id}");

        return true;
    }

    public void ControlsButton()
    {
        pauseMenu.SetActive(false);
        controlsMenu.SetActive(true);
    }


    public void QuitGame()
    {
        Debug.Log("Gooooodbyeeeee :(");
        Application.Quit();
    }
}
