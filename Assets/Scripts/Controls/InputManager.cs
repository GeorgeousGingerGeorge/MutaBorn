using UnityEngine;
using UnityEngine.InputSystem;

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

    public bool OnLeftClick() 
    {
        return mouseLeftAction.action.WasPressedThisFrame();
    }
}
