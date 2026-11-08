using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum TileTriggerMode
{
    WhileOccupied,
    OneShot

}

// Automatic grid trigger. It does not require a collider or an Action Menu action.
public class WorldTileTrigger : MonoBehaviour
{
    [Header("Grid Area")]
    [Tooltip("-1 uses the nearest grid floor. Set an explicit floor ID for stacked floors.")]
    [SerializeField] private int floorId = -1;
    [Tooltip("Rectangle starts at this object's nearest tile and extends in +X and +Z.")]
    [SerializeField] private Vector2Int areaSizeInTiles = Vector2Int.one;

    [Header("Trigger")]
    [SerializeField] private TileTriggerMode mode = TileTriggerMode.WhileOccupied;

    [Header("Visual (Optional)")]
    [SerializeField] private Transform plateVisual;
    [SerializeField] private Vector3 pressedOffset = new Vector3(0f, -0.07f, 0f);

    [Header("State Events")]
    [SerializeField] private UnityEvent onActivated = new UnityEvent();
    [SerializeField] private UnityEvent onDeactivated = new UnityEvent();

    [Header("Scene View")]
    [SerializeField, Min(0.1f)] private float gizmoCellSize = 2f;

    private readonly HashSet<UnitController> occupants = new HashSet<UnitController>();
    private GridNode anchorNode;
    private int resolvedFloorId;
    private Vector3 unpressedPosition;
    private bool initialized;
    private bool activated;

    public bool IsActivated => activated;

    private void Awake()
    {
        if (plateVisual != null)
        {
            unpressedPosition = plateVisual.localPosition;

        }

    }

    private void OnEnable()
    {
        UnitController.GridNodeChanged += OnUnitNodeChanged;
        if (initialized)
        {
            ScanExistingUnits();

        }

    }

    private void Start()
    {
        if (TryResolveArea())
        {
            initialized = true;
            ScanExistingUnits();

        }
        else
        {
            Debug.LogWarning($"Tile trigger '{name}' could not find its grid area.", this);

        }

    }

    private void OnDisable()
    {
        UnitController.GridNodeChanged -= OnUnitNodeChanged;
        occupants.Clear();

    }

    private bool TryResolveArea()
    {
        GridManager gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager == null)
        {

            return false;

        }

        int chosenFloor = floorId;
        if (chosenFloor < 0)
        {
            GridNode nearest = gridManager.GetNodeFromClosestWorldPosition(transform.position);
            if (nearest == null)
            {

                return false;

            }

            chosenFloor = nearest.floorID;

        }

        Dictionary<Vector2Int, GridNode> nodes = gridManager.GetGrid(chosenFloor);
        if (nodes == null || nodes.Count == 0)
        {

            return false;

        }

        float bestDistance = float.MaxValue;
        foreach (GridNode node in nodes.Values)
        {
            Vector2 delta = new Vector2(node.cellWorldPosition.x - transform.position.x,
                node.cellWorldPosition.z - transform.position.z);
            float distance = delta.sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                anchorNode = node;

            }

        }

        resolvedFloorId = chosenFloor;

        return anchorNode != null;

    }

    private bool Contains(UnitController unit, GridNode node)
    {
        if (node == null || unit == null || node.floorID != resolvedFloorId)
        {

            return false;

        }

        Vector2Int origin = anchorNode.coords;
        Vector2Int footprint = unit.footPrint;
        int width = Mathf.Max(1, areaSizeInTiles.x);
        int depth = Mathf.Max(1, areaSizeInTiles.y);

        return node.coords.x < origin.x + width &&
               node.coords.x + Mathf.Max(1, footprint.x) > origin.x &&
               node.coords.y < origin.y + depth &&
               node.coords.y + Mathf.Max(1, footprint.y) > origin.y;

    }

    private void ScanExistingUnits()
    {
        occupants.Clear();
        foreach (UnitController unit in FindObjectsByType<UnitController>())
        {
            if (unit.isActiveAndEnabled && Contains(unit, unit.currentNode))
            {
                occupants.Add(unit);

            }

        }

        RefreshState();

    }

    private void OnUnitNodeChanged(UnitController unit, GridNode previous, GridNode current)
    {
        if (!initialized || unit == null)
        {

            return;

        }

        bool isInside = unit.isActiveAndEnabled && Contains(unit, current);
        if (isInside)
        {
            occupants.Add(unit);

        }
        else
        {
            occupants.Remove(unit);

        }

        RefreshState();

    }

    private void RefreshState()
    {
        bool shouldActivate = occupants.Count > 0;
        if (shouldActivate && !activated)
        {
            activated = true;
            UpdateVisual();
            onActivated.Invoke();

        }
        else if (!shouldActivate && activated && mode == TileTriggerMode.WhileOccupied)
        {
            activated = false;
            UpdateVisual();
            onDeactivated.Invoke();

        }

    }

    // A one-shot trigger can be rearmed remotely or from another UnityEvent.
    public void ResetTrigger()
    {
        if (mode != TileTriggerMode.OneShot || !activated)
        {

            return;

        }

        activated = false;
        UpdateVisual();
        onDeactivated.Invoke();

    }

    private void UpdateVisual()
    {
        if (plateVisual != null)
        {
            plateVisual.localPosition = unpressedPosition + (activated ? pressedOffset : Vector3.zero);

        }

    }

    private void OnDrawGizmosSelected()
    {
        float width = Mathf.Max(1, areaSizeInTiles.x) * gizmoCellSize;
        float depth = Mathf.Max(1, areaSizeInTiles.y) * gizmoCellSize;
        Vector3 center = transform.position + new Vector3((width - gizmoCellSize) / 2f, 0f,
            (depth - gizmoCellSize) / 2f);
        Gizmos.color = new Color(0.25f, 0.9f, 0.65f, 0.85f);
        Gizmos.DrawWireCube(center, new Vector3(width, 0.08f, depth));

    }

}
