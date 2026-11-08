using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(ObstacleController))]
public class DoorController : MonoBehaviour
{
    [Header("Door Setup")]
    [SerializeField] private Transform visual;
    [SerializeField] private Collider physicalCollider;
    [SerializeField] private bool startsLocked;
    [SerializeField] private float openAngle = 90f;

    [Header("Closing Safety Area (Local Units)")]
    [SerializeField, Min(0.1f)] private float halfDepth = 0.35f;
    [SerializeField, Min(0.1f)] private float halfWidth = 1.05f;
    [SerializeField, Min(0.1f)] private float halfHeight = 1.5f;

    [Header("On Successful State Changes")]
    [SerializeField] private UnityEvent onOpened = new UnityEvent();
    [SerializeField] private UnityEvent onClosed = new UnityEvent();
    [SerializeField] private UnityEvent onLocked = new UnityEvent();
    [SerializeField] private UnityEvent onUnlocked = new UnityEvent();

    private ObstacleController obstacle;
    private Quaternion closedRotation;
    private bool isOpen;
    private bool isLocked;

    public bool IsOpen => isOpen;
    public bool IsLocked => isLocked;
    public bool CanClose => isOpen && obstacle != null && !DoorwayOccupied();
    public ObstacleController Obstacle => obstacle;

    private void Awake()
    {
        obstacle = GetComponent<ObstacleController>();
        isLocked = startsLocked;
        closedRotation = visual != null ? visual.localRotation : Quaternion.identity;

    }

    // Void commands are visible in UnityEvent Inspector connections.
    public void Open() { TryOpen(); }
    public void Close() { TryClose(); }
    public void Toggle() { TryToggle(); }
    public void Lock() { TryLock(); }
    public void Unlock() { TryUnlock(); }

    public bool TryOpen()
    {
        if (isOpen || isLocked || obstacle == null || !obstacle.IsBlocking || !obstacle.TrySetBlocking(false))
        {

            return false;

        }

        isOpen = true;
        UpdateVisual();
        onOpened.Invoke();

        return true;

    }

    public bool TryClose()
    {
        if (!isOpen || obstacle == null || DoorwayOccupied() || !obstacle.TrySetBlocking(true))
        {

            return false;

        }

        isOpen = false;
        UpdateVisual();
        onClosed.Invoke();

        return true;

    }

    public bool TryToggle()
    {

        return isOpen ? TryClose() : TryOpen();

    }

    public bool TryLock()
    {
        if (isLocked || isOpen)
        {

            return false;

        }

        isLocked = true;
        onLocked.Invoke();

        return true;

    }

    public bool TryUnlock()
    {
        if (!isLocked)
        {

            return false;

        }

        isLocked = false;
        onUnlocked.Invoke();

        return true;

    }

    private void UpdateVisual()
    {
        if (visual != null)
        {
            visual.localRotation = closedRotation * Quaternion.Euler(0f, isOpen ? openAngle : 0f, 0f);

        }

        if (physicalCollider != null)
        {
            physicalCollider.enabled = !isOpen;

        }

    }

    private bool DoorwayOccupied()
    {
        Occupancy occupancy = obstacle.CurrentOccupancy;
        if (occupancy == null)
        {

            return true;

        }

        foreach (UnitController unit in occupancy.GetUnits())
        {
            if (unit == null)
            {

                continue;

            }

            Vector3 local = transform.InverseTransformPoint(unit.transform.position);
            if (Mathf.Abs(local.x) <= halfDepth &&
                Mathf.Abs(local.y) <= halfHeight &&
                Mathf.Abs(local.z) <= halfWidth)
            {

                return true;

            }

        }

        return false;

    }

}
