using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

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
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void TestLevel()
    {
        SceneManager.LoadScene("Main Hub Integration Preview");
    }

    void OpenOptions()
    {
        Debug.Log("Options Selected");
        optionsPanel.SetActive(true);
        canvasMainMenu.SetActive(false);
    }

    void CloseOptions()
    {
        Debug.Log("Options Menu Closing");
        optionsPanel.SetActive(false);
        canvasMainMenu?.SetActive(true);
    }

    void QuitGame()
    {
        Debug.Log("Shutting Down... Goooodbye :(");
        Application.Quit();
    }

    void OpenCredits()
    {
        Debug.Log("Credits Selected");
        creditsPanel.SetActive(true);
        canvasMainMenu.SetActive(false);
    }

    void CloseCredits()
    {
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

        controlsPanel.SetActive(false);
        canvasMainMenu.SetActive(true);

    }
}
