using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InputManager : MonoBehaviour
{
    [Header("InputActions")]
    [SerializeField] public InputActionReference mouseLeftAction;


    public void OnEnable() 
    {
        mouseLeftAction.action.Enable();
    }
    public void OnDisable() 
    {
        mouseLeftAction.action.Disable();
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
        if (focused != null &&
            (focused.GetComponent<InputField>() != null ||
             focused.GetComponent<TMP_InputField>() != null))
        {
            return 0;

        }

        bool reverse = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        return reverse ? -1 : 1;

    }

    public bool OnLeftClick()   
    {
        return mouseLeftAction.action.WasPressedThisFrame();
    }
}
