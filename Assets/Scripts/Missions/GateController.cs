using UnityEngine;
using UnityEngine.Events;

public class GateController : MonoBehaviour
{
    [Header("Gate Sections (One DoorController Per Grid Edge)")]
    [SerializeField] private DoorController[] sections;

    [Header("Gate Visual")]
    [SerializeField] private Transform panel;
    [SerializeField] private Collider panelCollider;
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 2.25f, 0f);
    [SerializeField] private bool startsLocked;

    [Header("On Successful State Changes")]
    [SerializeField] private UnityEvent onOpened = new UnityEvent();
    [SerializeField] private UnityEvent onClosed = new UnityEvent();
    [SerializeField] private UnityEvent onLocked = new UnityEvent();
    [SerializeField] private UnityEvent onUnlocked = new UnityEvent();

    private Vector3 closedPosition;
    private bool isLocked;
    private bool changingState;

    public bool IsLocked => isLocked;

    public bool IsOpen
    {
        get
        {
            if (sections == null || sections.Length == 0)
            {

                return false;

            }

            foreach (DoorController section in sections)
            {
                if (section == null || !section.IsOpen)
                {

                    return false;

                }

            }

            return true;

        }

    }

    private void Awake()
    {
        isLocked = startsLocked;
        closedPosition = panel != null ? panel.localPosition : Vector3.zero;
        UpdateVisual();

    }

    public void Open() { TryOpen(); }
    public void Close() { TryClose(); }
    public void Toggle() { TryToggle(); }
    public void Lock() { TryLock(); }
    public void Unlock() { TryUnlock(); }

    public bool TryOpen()
    {
        if (changingState || isLocked || sections == null || sections.Length == 0 || IsOpen)
        {

            return false;

        }

        foreach (DoorController section in sections)
        {
            if (section == null || section.IsOpen || section.IsLocked ||
                section.Obstacle == null || !section.Obstacle.IsBlocking)
            {

                return false;

            }

        }

        changingState = true;
        try
        {
            int opened = 0;
            foreach (DoorController section in sections)
            {
                if (!section.TryOpen())
                {
                    for (int i = opened - 1; i >= 0; i--)
                    {
                        sections[i].TryClose();

                    }

                    return false;

                }

                opened++;

            }

            UpdateVisual();
            onOpened.Invoke();

            return true;

        }
        finally
        {
            changingState = false;

        }

    }

    public bool TryClose()
    {
        if (changingState || !IsOpen)
        {

            return false;

        }

        // Every section must be safe to close before moving any of them.
        foreach (DoorController section in sections)
        {
            if (!section.CanClose)
            {

                return false;

            }

        }

        changingState = true;
        try
        {
            int closed = 0;
            foreach (DoorController section in sections)
            {
                if (!section.TryClose())
                {
                    for (int i = closed - 1; i >= 0; i--)
                    {
                        sections[i].TryOpen();

                    }

                    return false;

                }

                closed++;

            }

            UpdateVisual();
            onClosed.Invoke();

            return true;

        }
        finally
        {
            changingState = false;

        }

    }

    public bool TryToggle()
    {

        return IsOpen ? TryClose() : TryOpen();

    }

    public bool TryLock()
    {
        if (changingState || isLocked || IsOpen)
        {

            return false;

        }

        isLocked = true;
        onLocked.Invoke();

        return true;

    }

    public bool TryUnlock()
    {
        if (changingState || !isLocked)
        {

            return false;

        }

        isLocked = false;
        onUnlocked.Invoke();

        return true;

    }

    public bool IsAdjacentTo(GridNode node)
    {
        if (node == null || sections == null)
        {

            return false;

        }

        foreach (DoorController section in sections)
        {
            ObstacleController edge = section != null ? section.Obstacle : null;
            if (edge != null && (edge.firstnode == node || edge.secondnode == node))
            {

                return true;

            }

        }

        return false;

    }

    private void UpdateVisual()
    {
        if (panel != null)
        {
            panel.localPosition = closedPosition + (IsOpen ? openOffset : Vector3.zero);

        }

        if (panelCollider != null)
        {
            panelCollider.enabled = !IsOpen;

        }

    }

}
