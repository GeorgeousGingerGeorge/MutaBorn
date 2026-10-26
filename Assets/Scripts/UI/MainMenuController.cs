using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        testLevelButton.onClick.AddListener(TestLevel);
        optionsButton.onClick.AddListener(OpenOptions);
        quitButton.onClick.AddListener(QuitGame);
        creditsButton.onClick.AddListener(OpenCredits);
        creditsBackButton.onClick.AddListener(CloseCredits);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void TestLevel()
    {
        SceneManager.LoadScene("Main Test Scene");
    }

    void OpenOptions()
    {
        Debug.Log("Options Selected");
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
}
