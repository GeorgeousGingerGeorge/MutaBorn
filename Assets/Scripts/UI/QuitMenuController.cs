using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitMenuController : MonoBehaviour
{
    public void QuitToMainMenu()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("Main Menu Sand Box");
    }

    public void QuitToDesktop()
    {
        Application.Quit();
    }
}
