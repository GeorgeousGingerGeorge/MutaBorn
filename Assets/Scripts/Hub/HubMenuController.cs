using UnityEngine;
using UnityEngine.SceneManagement;

public class HubMenuController : MonoBehaviour
{
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu Sand Box");
    }
}
