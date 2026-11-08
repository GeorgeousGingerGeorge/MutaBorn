using UnityEngine;
using UnityEngine.Events;

public class WorldSwitchController : MonoBehaviour
{
    [Header("Starting State")]
    [SerializeField] private bool startsOn;

    [Header("Visual")]
    [SerializeField] private Transform handlePivot;
    [SerializeField] private Vector3 onRotationEuler = new Vector3(40f, 0f, 0f);

    [Header("On State Changed")]
    [SerializeField] private UnityEvent onActivated = new UnityEvent();
    [SerializeField] private UnityEvent onDeactivated = new UnityEvent();

    private Quaternion offRotation;
    private bool isOn;
    private bool changingState;

    public bool IsOn => isOn;
    public bool CanToggle => !changingState;

    private void Awake()
    {
        isOn = startsOn;
        offRotation = handlePivot != null ? handlePivot.localRotation : Quaternion.identity;
        UpdateVisual();

    }

    // Receiver methods for UnityEvent connections (nearby or remote).
    public void Activate()
    {
        TryActivate();

    }

    public void Deactivate()
    {
        TryDeactivate();

    }

    public void Toggle()
    {
        TryToggle();

    }

    public bool TryActivate()
    {

        return TrySetState(true);

    }

    public bool TryDeactivate()
    {

        return TrySetState(false);

    }

    public bool TryToggle()
    {

        return TrySetState(!isOn);

    }

    private bool TrySetState(bool newState)
    {
        if (changingState || isOn == newState)
        {

            return false;

        }

        changingState = true;
        isOn = newState;
        UpdateVisual();

        try
        {
            if (isOn)
            {
                onActivated.Invoke();

            }
            else
            {
                onDeactivated.Invoke();

            }

        }
        finally
        {
            changingState = false;

        }

        return true;

    }

    private void UpdateVisual()
    {
        if (handlePivot != null)
        {
            handlePivot.localRotation = offRotation * Quaternion.Euler(isOn ? onRotationEuler : Vector3.zero);

        }

    }

}
