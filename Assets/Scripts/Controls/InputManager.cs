using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;


public class InputManager : MonoBehaviour
{
    [Header("InputActions")]
    [SerializeField] public InputActionReference mouseLeftAction;
    [SerializeField] public InputActionReference returnToMainMenu;
    [SerializeField] public InputActionReference floorUpAction;
    [SerializeField] public InputActionReference floorDownAction;

    public void OnEnable() 
    {
        SetActionEnabled(mouseLeftAction, true);
        SetActionEnabled(returnToMainMenu, true);
        SetActionEnabled(floorUpAction, true);
        SetActionEnabled(floorDownAction, true);

    }
    public void OnDisable() 
    {
        SetActionEnabled(mouseLeftAction, false);
        SetActionEnabled(returnToMainMenu, false);
        SetActionEnabled(floorUpAction, false);
        SetActionEnabled(floorDownAction, false);

    }

    private static void SetActionEnabled(InputActionReference reference, bool enabled)
    {
        if (reference == null || reference.action == null)
        {
            return;

        }

        if (enabled)
        {
            reference.action.Enable();

        }
        else
        {
            reference.action.Disable();

        }

    }

    private static bool WasPressed(InputActionReference reference)
    {
        return reference != null && reference.action != null && reference.action.WasPressedThisFrame();

    }

    public int GetUnitCycleDirection()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame)
        {
            return 0;
        }

        // Do not cycle units while typing into a text field.
        GameObject focused = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject : null;
        if (focused != null && (focused.GetComponent<InputField>() != null || focused.GetComponent<TMP_InputField>() != null))
        {
            return 0;
        }

        bool reverse = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

        return reverse ? -1 : 1;

    }
    public int GetFloorChangeDirection()
    {
        GameObject focused = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        if (focused != null && (focused.GetComponent<InputField>() != null || focused.GetComponent<TMP_InputField>() != null))
        {
            return 0;
        }

        if (WasPressed(floorUpAction))
        {
            return 1;
        }

        if (WasPressed(floorDownAction))
        {
            return -1;
        }
        return 0;
    }

    public bool OnLeftClick()   
    {
        return WasPressed(mouseLeftAction);
    }

    public bool onReturnToMenu()
    {
        return WasPressed(returnToMainMenu);
    }
}