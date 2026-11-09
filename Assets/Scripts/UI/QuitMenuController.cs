using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitMenuController : MonoBehaviour
{

    [SerializeField] private GameObject pauseMenu;

    public void QuitToMainMenu()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("Main Menu Sand Box");
    }

    public void QuitToDesktop()
    {
        Application.Quit();
    }

    public void CancelQuit()
    {
        gameObject.SetActive(false);
        pauseMenu.SetActive(true);
    }
}
