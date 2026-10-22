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
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        testLevelButton.onClick.AddListener(TestLevel);
        optionsButton.onClick.AddListener(OpenOptions);
        quitButton.onClick.AddListener(QuitGame);
        creditsButton.onClick.AddListener(OpenCredits);
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
        Application.Quit();
    }

    void OpenCredits()
    {
        Debug.Log("Credits Selected");
    }
}
