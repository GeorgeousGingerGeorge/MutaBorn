using System;
using System.Collections.Generic;
using UnityEngine;

// A tile-based extraction region. The in-game outline is visual only.
// MissionObjectiveManager continues to use MatchesZoneId and Contains unchanged.
public sealed class MissionExtractionZone : MonoBehaviour
{
    [Header("Extraction Objective")]
    [Tooltip("Match the extraction objective's Target Key; typically 'evac'.")]
    [SerializeField] private string zoneId = "evac";

    [Header("Grid Area (same convention as mission spawn areas)")]
    [Tooltip("-1 selects the closest grid floor using this marker's height (useful for older scenes). Set an explicit floor ID for new levels.")]
    [SerializeField] private int floorId = 1;
    [Tooltip("How many tiles the zone covers, starting at the nearest tile to this marker on the selected floor.")]
    [SerializeField] private Vector2Int areaSizeInTiles = new Vector2Int(3, 3);
    [Tooltip("Only count a large unit when its complete tile footprint is inside the zone.")]
    [SerializeField] private bool requireEntireFootprint = true;

    [Header("Visual Border (does not affect detection)")]
    [SerializeField] private bool showOutlineInGame = true;
    [SerializeField] private Color outlineColor = new Color(0.2f, 0.95f, 1f, 0.7f);
    [SerializeField, Min(0.01f)] private float outlineWidth = 0.07f;
    [Tooltip("Distance above the grid tile surface, in world units.")]
    [SerializeField, Min(0f)] private float outlineHeight = 0.09f;

    [Header("Scene View")]
    [SerializeField] private Color gizmoColor = new Color(0.15f, 0.95f, 1f, 0.25f);
    [SerializeField, Min(0.1f)] private float gizmoCellSize = 2f;

    private GridManager gridManager;
    private Dictionary<Vector2Int, GridNode> floorNodes;
    private GridNode anchorNode;
    private int resolvedFloorId = -1;
    private Vector2Int anchorCoords;
    private LineRenderer outlineRenderer;
    private Material outlineMaterial;
    private bool warnedAboutGrid;
    private bool hasResolvedArea;
    private Vector3 lastMarkerPosition;
    private int lastRequestedFloorId = int.MinValue;
    private Vector2Int lastAreaSize;

    public string ZoneId => zoneId;

    // Read-only anchor for mission banners: center of the configured tile rectangle,
    // at the visible tile surface, regardless of this GameObject's Y position.
    public bool TryGetWorldMarkerPosition(out Vector3 position)
    {
        position = transform.position;
        if (!TryResolveArea() || anchorNode == null)
        {
            return false;

        }

        Vector2Int size = GetAreaSize();
        Vector2Int sample = anchorCoords + new Vector2Int((size.x - 1) / 2, (size.y - 1) / 2);
        float halfX = (size.x - 1) * gridManager.GridCellSize * 0.5f;
        float halfZ = (size.y - 1) * gridManager.GridCellSize * 0.5f;
        position = anchorNode.cellWorldPosition + new Vector3(halfX, 0f, halfZ);
        position.y = SurfaceHeight(sample);

        return true;

    }

    private void Start()
    {
        if (TryResolveArea() && showOutlineInGame)
        {
            CreateOutline();

        }

    }

    public bool MatchesZoneId(string requiredId)
    {
        return string.IsNullOrWhiteSpace(requiredId) ||
               string.Equals(zoneId?.Trim(), requiredId.Trim(), StringComparison.OrdinalIgnoreCase);

    }

    public bool Contains(UnitController unit)
    {
        if (unit == null || !unit.isActiveAndEnabled || unit.data == null || unit.data.currentHealth <= 0)
        {
            return false;

        }

        if (!TryResolveArea())
        {
            return false;

        }

        // The unit's registered GRID node is authoritative, not the height of its
        // transform, model, selection circle, collider or animation root.
        GridNode node = unit.currentNode;
        // A unit not registered to the grid is not safely inside any extraction tile.
        // Its transform alone is not proof that it occupies a real tile.

        if (node == null || node.floorID != resolvedFloorId)
        {
            return false;

        }

        Vector2Int size = GetAreaSize();
        Vector2Int footprint = requireEntireFootprint
            ? new Vector2Int(Mathf.Max(1, unit.footPrint.x), Mathf.Max(1, unit.footPrint.y))
            : Vector2Int.one;

        for (int x = 0; x < footprint.x; x++)
        {
            for (int z = 0; z < footprint.y; z++)
            {
                Vector2Int coords = node.coords + new Vector2Int(x, z);
                if (coords.x < anchorCoords.x || coords.y < anchorCoords.y ||
                    coords.x >= anchorCoords.x + size.x || coords.y >= anchorCoords.y + size.y ||
                    !floorNodes.ContainsKey(coords))
                {
                    return false;

                }

            }

        }

        return true;

    }

    private bool TryResolveArea()
    {
        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();

        }

        if (gridManager == null)
        {
            WarnMissingGridOnce("No GridManager found");

            return false;

        }

        if (hasResolvedArea && floorNodes != null &&
            lastMarkerPosition == transform.position && lastRequestedFloorId == floorId &&
            lastAreaSize == GetAreaSize())
        {
            return true;

        }

        int selectedFloor = floorId;
        if (selectedFloor < 0)
        {
            // Compatibility for old Extraction Zone instances that were placed
            // by height instead of configuring a floor. Only choosing the floor
            // uses Y; unit membership NEVER uses collider height.
            GridNode nearest = gridManager.GetNodeFromClosestWorldPosition(transform.position);
            if (nearest == null)
            {
                WarnMissingGridOnce("No grid nodes available to detect a floor");

                return false;

            }

            selectedFloor = nearest.floorID;

        }

        Dictionary<Vector2Int, GridNode> selectedNodes = gridManager.GetGrid(selectedFloor);
        if (selectedNodes == null || selectedNodes.Count == 0)
        {
            WarnMissingGridOnce($"Floor {selectedFloor} has no grid nodes");

            return false;

        }

        // If the zone is placed above/below a tile, still pick the intended XZ
        // tile on the chosen floor. The marker's Y never moves the rectangle.
        GridNode nearestHorizontal = FindNearestHorizontalNode(selectedNodes, transform.position);
        if (nearestHorizontal == null)
        {
            return false;

        }

        bool changed = resolvedFloorId != selectedFloor || anchorNode != nearestHorizontal ||
                       floorNodes != selectedNodes || lastAreaSize != GetAreaSize();
        resolvedFloorId = selectedFloor;
        floorNodes = selectedNodes;
        anchorNode = nearestHorizontal;
        anchorCoords = anchorNode.coords;
        hasResolvedArea = true;
        lastMarkerPosition = transform.position;
        lastRequestedFloorId = floorId;
        lastAreaSize = GetAreaSize();

        if (changed && outlineRenderer != null)
        {
            UpdateOutlinePositions();

        }

        return true;

    }

    private static GridNode FindNearestHorizontalNode(Dictionary<Vector2Int, GridNode> nodes, Vector3 worldPosition)
    {
        GridNode closest = null;
        float shortestDistance = float.MaxValue;
        foreach (GridNode node in nodes.Values)
        {
            Vector3 delta = node.cellWorldPosition - worldPosition;
            float distance = delta.x * delta.x + delta.z * delta.z;
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closest = node;

            }

        }

        return closest;

    }

    private Vector2Int GetAreaSize()
    {
        return new Vector2Int(Mathf.Max(1, areaSizeInTiles.x), Mathf.Max(1, areaSizeInTiles.y));

    }

    private void WarnMissingGridOnce(string problem)
    {
        if (warnedAboutGrid)
        {
            return;

        }

        warnedAboutGrid = true;
        Debug.LogWarning($"[ExtractionZone] '{name}' cannot resolve its area: {problem}. Extraction will not count until the grid exists.", this);

    }

    private void CreateOutline()
    {
        if (outlineRenderer == null)
        {
            outlineRenderer = GetComponent<LineRenderer>();
            if (outlineRenderer == null)
            {
                outlineRenderer = gameObject.AddComponent<LineRenderer>();

            }

        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        }

        if (shader == null)
        {
            Debug.LogWarning("No extraction-zone outline shader available. Grid-based extraction will still work.", this);

            return;

        }

        outlineMaterial = new Material(shader);
        if (outlineMaterial.HasProperty("_Color"))
        {
            outlineMaterial.SetColor("_Color", outlineColor);

        }

        if (outlineMaterial.HasProperty("_BaseColor"))
        {
            outlineMaterial.SetColor("_BaseColor", outlineColor);

        }

        outlineRenderer.sharedMaterial = outlineMaterial;
        outlineRenderer.useWorldSpace = true;
        outlineRenderer.loop = true;
        outlineRenderer.positionCount = 4;
        outlineRenderer.startColor = outlineColor;
        outlineRenderer.endColor = outlineColor;
        outlineRenderer.startWidth = outlineWidth;
        outlineRenderer.endWidth = outlineWidth;
        outlineRenderer.alignment = LineAlignment.View;
        outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.sortingOrder = 5;
        UpdateOutlinePositions();

    }

    private void UpdateOutlinePositions()
    {
        if (outlineRenderer == null || anchorNode == null || gridManager == null)
        {
            return;

        }

        Vector2Int size = GetAreaSize();
        float halfCell = gridManager.GridCellSize * 0.5f;
        Vector3 start = anchorNode.cellWorldPosition;
        float x0 = start.x - halfCell;
        float z0 = start.z - halfCell;
        float x1 = start.x + (size.x - 1) * gridManager.GridCellSize + halfCell;
        float z1 = start.z + (size.y - 1) * gridManager.GridCellSize + halfCell;

        // Obtain each corner's surface height from its actual grid node when
        // available. Render height does not participate in Contains().
        Vector3 a = new Vector3(x0, SurfaceHeight(anchorCoords) + outlineHeight, z0);
        Vector3 b = new Vector3(x1, SurfaceHeight(anchorCoords + new Vector2Int(size.x - 1, 0)) + outlineHeight, z0);
        Vector3 c = new Vector3(x1, SurfaceHeight(anchorCoords + size - Vector2Int.one) + outlineHeight, z1);
        Vector3 d = new Vector3(x0, SurfaceHeight(anchorCoords + new Vector2Int(0, size.y - 1)) + outlineHeight, z1);

        outlineRenderer.SetPosition(0, a);
        outlineRenderer.SetPosition(1, b);
        outlineRenderer.SetPosition(2, c);
        outlineRenderer.SetPosition(3, d);

    }

    private float SurfaceHeight(Vector2Int coords)
    {
        if (floorNodes != null && floorNodes.TryGetValue(coords, out GridNode node))
        {
            // GridNode.cellWorldPosition is the tile's transform/pivot, not
            // necessarily its visible top. For example, a 0.5-high cube
            // centered on the node has a top surface 0.25 units above it.
            TileController tile = gridManager != null ? gridManager.FindTile(node) : null;
            if (tile != null)
            {
                // Prefer the renderer ON the tile root, not child decorations
                // such as trees, overlays, crates or tall scenery.
                Renderer surfaceRenderer = tile.GetComponent<Renderer>();
                if (surfaceRenderer != null)
                {
                    return surfaceRenderer.bounds.max.y;

                }

                Collider surfaceCollider = tile.GetComponent<Collider>();
                if (surfaceCollider != null && surfaceCollider.enabled)
                {
                    return surfaceCollider.bounds.max.y;

                }

            }

            return node.cellWorldPosition.y;

        }

        return anchorNode != null ? anchorNode.cellWorldPosition.y : transform.position.y;

    }

    private void OnDrawGizmosSelected()
    {
        Vector2Int size = GetAreaSize();
        float cellSize = Mathf.Max(0.1f, gizmoCellSize);
        Vector3 origin = transform.position;
        if (Application.isPlaying && TryResolveArea() && anchorNode != null)
        {
            origin = anchorNode.cellWorldPosition;
            cellSize = gridManager.GridCellSize;

        }

        Color oldColor = Gizmos.color;
        Gizmos.color = gizmoColor;
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector3 center = origin + new Vector3(x * cellSize, outlineHeight, z * cellSize);
                if (Application.isPlaying && floorNodes != null &&
                    floorNodes.TryGetValue(anchorCoords + new Vector2Int(x, z), out GridNode node))
                {
                    center = node.cellWorldPosition;
                    center.y = SurfaceHeight(node.coords) + outlineHeight;

                }

                Gizmos.DrawWireCube(center, new Vector3(cellSize, 0.05f, cellSize));

            }

        }

        Gizmos.color = oldColor;

    }
    public void ResolveFloorID(ChunkInstance ownerChunk)
    {
        if (!ownerChunk.globalFloorMappings.TryGetValue(floorId, out int resolvedFloorID))
        {
            return;
        }

        floorId = resolvedFloorID;
    }
    private void OnValidate()
    {
        areaSizeInTiles.x = Mathf.Max(1, areaSizeInTiles.x);
        areaSizeInTiles.y = Mathf.Max(1, areaSizeInTiles.y);

    }

    private void OnDestroy()
    {
        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);

        }

    }
}
