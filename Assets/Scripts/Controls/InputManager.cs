using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InputManager : MonoBehaviour
{
    [Header("InputActions")]
    [SerializeField] public InputActionReference mouseLeftAction;
    [SerializeField] public InputActionReference returnToMainMenu;

    public void OnEnable() 
    {
        mouseLeftAction.action.Enable();
        returnToMainMenu.action.Enable();
    }
    public void OnDisable() 
    {
        mouseLeftAction.action.Disable();
        returnToMainMenu.action.Disable();
    }

    public bool OnLeftClick()   
    {
        return mouseLeftAction.action.WasPressedThisFrame();
    }

    public bool onReturnToMenu()
    {
        return returnToMainMenu.action.WasPressedThisFrame();
    }

    private void Update()
    {
        if (onReturnToMenu())
        {
            Debug.Log("Return to menu key pressed");
            SceneManager.LoadScene("Main Menu Sand Box");
        }
    }
}