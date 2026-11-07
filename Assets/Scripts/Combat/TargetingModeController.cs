using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-20)]
public sealed class TargetingModeController : MonoBehaviour
{
    public static TargetingModeController Instance { get; private set; }
    
    public static bool IsTargeting => Instance != null && Instance.isTargeting;
    public static bool BlocksSelection => Instance != null &&
        (Instance.isTargeting || Instance.lastTargetingEndFrame == Time.frameCount);

    [Header("Targeting HUD (optional)")]
    [Tooltip("Assign a small banner under the Canvas, not the entire Action Menu.")]
    [SerializeField] private GameObject targetingBanner;
    [SerializeField] private TMP_Text targetingMessage;
    [SerializeField] private Button cancelButton;

    [Header("World-Space Range Ring (prototype)")]
    [Tooltip("An optional LineRenderer. If missing, one is created at runtime.")]
    [SerializeField] private LineRenderer rangeRing;
    [SerializeField] private Material rangeRingMaterial;
    [SerializeField, Range(24, 128)] private int ringSegments = 72;
    [SerializeField, Min(0.01f)] private float ringWidth = 0.12f;
    [SerializeField] private float ringHeightOffset = 0.15f;
    [SerializeField] private Color ringColor = new Color(1f, 0.20f, 0.16f, 0.95f);

    private bool isTargeting;
    private UnityEngine.Object owner;
    private UnitController sourceUnit;
    private float range;
    private bool showRangeIndicator = true;
    private int startedOnFrame;
    private int lastTargetingEndFrame = -1;
    private Action<RaycastHit> targetSelected;
    private Action<Vector3, int> groundPointSelected;
    private Func<RaycastHit, bool> acceptsHit;
    private Func<Vector3, int, bool> groundTargetFilter;
    private Action targetingCancelled;
    private Func<bool> canContinue;
    private SelectionManager selectionManager;
    private Material generatedMaterial;
    private bool madeRangeRing;
    private LineRenderer explosionRing;
    private Material explosionMaterial;
    private float explosionRadiusWorld;
    private bool groundTargeting;
    private int requiredFloor;
    private bool hasHoverTile;
    private Vector3 hoveredGroundPoint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;

    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one TargetingModeController should exist in a scene.", this);
            enabled = false;
            return;

        }

        Instance = this;
        selectionManager = FindAnyObjectByType<SelectionManager>();

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CancelTargeting);

        }

        PrepareRing();
        SetVisuals(false);

    }

    public bool TryBeginTargeting(
        UnityEngine.Object requester,
        UnitController source,
        string displayName,
        float maximumRange,
        Action<RaycastHit> onTargetSelected,
        Func<RaycastHit, bool> targetFilter = null,
        Action onCancelled = null,
        Func<bool> stillAllowed = null,
        bool showRangeIndicator = true,
        bool allowGroundTargeting = false,
        float explosionPreviewRadiusWorld = 0f,
        int landingFloorId = -1,
        Action<Vector3, int> onGroundPointSelected = null,
        Func<Vector3,int, bool> groundTargetFilter = null)
    {
        if (!isActiveAndEnabled || requester == null || source == null ||
            source.data == null || (onTargetSelected == null && onGroundPointSelected == null))
        {
            return false;

        }

        if (isTargeting)
        {
            CancelTargeting();

        }

        owner = requester;
        sourceUnit = source;
        range = Mathf.Max(0f, maximumRange);
        this.showRangeIndicator = showRangeIndicator;
        groundTargeting = allowGroundTargeting;
        explosionRadiusWorld = Mathf.Max(0f, explosionPreviewRadiusWorld);
        requiredFloor = landingFloorId;
        hasHoverTile = false;
        groundPointSelected = onGroundPointSelected;
        targetSelected = onTargetSelected;
        acceptsHit = targetFilter;
        targetingCancelled = onCancelled;
        canContinue = stillAllowed;
        startedOnFrame = Time.frameCount;
        isTargeting = true;
        this.groundTargetFilter = groundTargetFilter;

        if (selectionManager == null)
        {
            selectionManager = FindAnyObjectByType<SelectionManager>();

        }

        // Avoid moving to a previously clicked tile while selecting a target.
        if (selectionManager != null)
        {
            selectionManager.ClearDestination();

        }

        source.GetComponent<UnitMovements>()?.ClearMovementRange();

        if (targetingMessage != null)
        {
            targetingMessage.text =
                $"TARGETING: {(displayName ?? "CHOOSE TARGET").ToUpperInvariant()}\n" +
                (showRangeIndicator ? $"RANGE : {range:0.#}\n\n" : "RANGE : ANY UNIT\n\n") +
                (groundTargeting ? "Left-Click Anywhere In Range | Esc to Cancel" :
                 "Left-Click a Target | Esc to Cancel");

        }

        SetVisuals(true);
        return true;

    }

    private void Update()
    {
        if (!isTargeting)
        {
            return;

        }

        if (owner == null || sourceUnit == null || !sourceUnit.isActiveAndEnabled ||
            sourceUnit.data == null || sourceUnit.data.currentHealth <= 0 ||
            (canContinue != null && !canContinue()))
        {
            CancelTargeting();
            return;

        }

        DrawRangeRing();
        UpdateExplosionPreview();

        if ((Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            CancelTargeting();
            return;

        }

        // The click opening the Action Menu ability cannot also choose its target.
        if (Time.frameCount == startedOnFrame || Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;

        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;

        }

        if (groundTargeting && groundPointSelected != null)
        {
            if (TryGetGroundPoint(out Vector3 impactPoint, out int floorId) &&
                (groundTargetFilter == null || groundTargetFilter(impactPoint,floorId))
                /*&& IsWithinThrowRange(impactPoint)*/)
            {
                groundPointSelected.Invoke(impactPoint, floorId);

            }
            else
            {
                Debug.Log("Explosion location must be on the allowed floor and inside throw range.", this);

            }

            return;

        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;

        }

        Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(
            ray, 1000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Ground attacks prefer the tile under a unit rather than its hitbox.
        // This lets explosions land under a standing character.
        if (groundTargeting)
        {
            foreach (RaycastHit hit in hits)
            {
                TileController tile = hit.collider != null ?
                    hit.collider.GetComponentInParent<TileController>() : null;
                if (tile != null && tile.node != null &&
                    (requiredFloor < 0 || tile.node.floorID == requiredFloor) &&
                    (acceptsHit == null || acceptsHit(hit)))
                {
                    targetSelected?.Invoke(hit);

                    return;

                }

            }

            Debug.Log("Choose a tile on the allowed floor, or cancel targeting.", this);

            return;

        }

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && (acceptsHit == null || acceptsHit(hit)))
            {
                targetSelected?.Invoke(hit);
                return;

            }

        }

        Debug.Log("Select a valid target, or right-click/Esc to cancel.", this);

    }

    public void CancelTargeting()
    {
        StopTargetingInternal(notifyCancelled: true);

    }

    public void CompleteTargeting(UnityEngine.Object requester)
    {
        if (isTargeting && owner == requester)
        {
            StopTargetingInternal(notifyCancelled: false);

        }

    }

    private void StopTargetingInternal(bool notifyCancelled, bool restoreMovement = true)
    {
        if (!isTargeting)
        {
            return;

        }

        UnitController previousSource = sourceUnit;
        Action cancelled = targetingCancelled;

        isTargeting = false;
        lastTargetingEndFrame = Time.frameCount;
        owner = null;
        sourceUnit = null;
        targetSelected = null;
        acceptsHit = null;
        targetingCancelled = null;
        canContinue = null;
        groundTargeting = false;
        groundPointSelected = null;
        hasHoverTile = false;
        SetVisuals(false);
        groundTargetFilter = null;

        if (restoreMovement && previousSource != null && previousSource.isActiveAndEnabled &&
            previousSource.data != null && previousSource.data.currentHealth > 0 &&
            selectionManager != null && selectionManager.selectedUnit == previousSource &&
            TurnManager.Instance != null && TurnManager.Instance.IsPlayerTurn)
        {
            previousSource.GetComponent<UnitMovements>()?.ShowMovementRange();

        }

        if (notifyCancelled)
        {
            cancelled?.Invoke();

        }

    }

    private void PrepareRing()
    {
        if (rangeRing == null)
        {
            GameObject ringObject = new GameObject("TargetingRangeRing (Runtime)");
            ringObject.transform.SetParent(transform, false);
            rangeRing = ringObject.AddComponent<LineRenderer>();
            madeRangeRing = true;

        }

        rangeRing.useWorldSpace = true;
        rangeRing.loop = true;
        rangeRing.widthMultiplier = ringWidth;
        rangeRing.positionCount = ringSegments;
        rangeRing.startColor = ringColor;
        rangeRing.endColor = ringColor;
        rangeRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rangeRing.receiveShadows = false;

        if (rangeRingMaterial != null)
        {
            rangeRing.sharedMaterial = rangeRingMaterial;

        }
        else if (madeRangeRing)
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");

            }

            if (shader != null)
            {
                generatedMaterial = new Material(shader);
                generatedMaterial.color = ringColor;
                rangeRing.sharedMaterial = generatedMaterial;

            }
            else
            {
                Debug.LogWarning("Targeting range ring needs a material. Assign one in the Inspector.", this);

            }

        }

        rangeRing.enabled = false;
        PrepareExplosionRing();

    }

    private void PrepareExplosionRing()
    {
        GameObject preview = new GameObject("ExplosionRadiusPreview (Runtime)");
        preview.transform.SetParent(transform, false);
        explosionRing = preview.AddComponent<LineRenderer>();
        explosionRing.useWorldSpace = true;
        explosionRing.loop = true;
        explosionRing.positionCount = ringSegments;
        explosionRing.widthMultiplier = ringWidth;
        explosionRing.startColor = new Color(1f, 0.62f, 0.12f, 1f);
        explosionRing.endColor = explosionRing.startColor;
        explosionRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        explosionRing.receiveShadows = false;

        if (rangeRing != null && rangeRing.sharedMaterial != null)
        {
            explosionMaterial = new Material(rangeRing.sharedMaterial);
            explosionMaterial.color = Color.white;
            explosionRing.sharedMaterial = explosionMaterial;

        }

        explosionRing.enabled = false;

    }

    private void UpdateExplosionPreview()
    {
        if (!groundTargeting || explosionRing == null)
        {
            return;

        }

        hasHoverTile = TryGetGroundPoint(out hoveredGroundPoint, out int floorId) /*&&
            IsWithinThrowRange(hoveredGroundPoint)*/;
        explosionRing.enabled = hasHoverTile;

        if (hasHoverTile && groundTargetFilter != null) 
        {
            hasHoverTile = groundTargetFilter(hoveredGroundPoint, floorId);
        }
        explosionRing.enabled = hasHoverTile;

        if (!hasHoverTile)
        {
            return;

        }

        Vector3 center = hoveredGroundPoint + Vector3.up * ringHeightOffset;
        for (int i = 0; i < ringSegments; i++)
        {
            float theta = i * (Mathf.PI * 2f / ringSegments);
            Vector3 offset = new Vector3(Mathf.Cos(theta) * explosionRadiusWorld,
                0f, Mathf.Sin(theta) * explosionRadiusWorld);
            explosionRing.SetPosition(i, center + offset);

        }

    }

    // Use the exact mouse hit location, never the center of a grid cell.
    // When the cursor is over a tile seam, intersect the current floor plane.
    private bool TryGetGroundPoint(out Vector3 position, out int floorId)
    {
        position = Vector3.zero;
        floorId = -1;
        if (Camera.main == null || Mouse.current == null || sourceUnit == null ||
            sourceUnit.currentNode == null)
        {
            return false;

        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        Debug.Log($"Required floor: {requiredFloor}");
        foreach (RaycastHit hit in hits)
        {
            TileController tile = hit.collider != null ?
                hit.collider.GetComponentInParent<TileController>() : null;
            if (tile != null && tile.node != null &&
                (requiredFloor < 0 || tile.node.floorID == requiredFloor))
            {
                    Debug.Log(
         $"Ray hit tile on floor {tile.node.floorID} at {hit.point}"
     );
                position = hit.point;
                floorId = tile.node.floorID;

                return true;

            }

        }

        // Only guess the floor for same-floor throws. Cross-floor throws need
        // an actual tile hit so we know which level should receive the blast.
        if (requiredFloor < 0)
        {
            return false;

        }

        Plane floorPlane = new Plane(Vector3.up, sourceUnit.currentNode.cellWorldPosition);
        if (!floorPlane.Raycast(ray, out float distance) || distance < 0f)
        {
            return false;

        }

        position = ray.GetPoint(distance);
        floorId = requiredFloor;

        return true;

    }

    private bool IsWithinThrowRange(Vector3 point)
    {
        if (sourceUnit == null)
        {
            return false;

        }

        Vector3 delta = point - sourceUnit.transform.position;
        delta.y = 0f;

        return delta.sqrMagnitude <= range * range + 0.0001f;

    }

    private void SetVisuals(bool active)
    {
        if (targetingBanner != null)
        {
            targetingBanner.SetActive(active);

        }

        if (rangeRing != null)
        {
            rangeRing.enabled = active && showRangeIndicator;

        }

        if (explosionRing != null)
        {
            explosionRing.enabled = active && groundTargeting && hasHoverTile;

        }

        if (active)
        {
            DrawRangeRing();

        }

    }

    private void DrawRangeRing()
    {
        if (!showRangeIndicator || rangeRing == null || sourceUnit == null)
        {
            return;

        }

        Vector3 center = sourceUnit.transform.position + Vector3.up * ringHeightOffset;

        for (int i = 0; i < ringSegments; i++)
        {
            float theta = i * (Mathf.PI * 2f / ringSegments);
            Vector3 offset = new Vector3(Mathf.Cos(theta) * range, 0f, Mathf.Sin(theta) * range);
            rangeRing.SetPosition(i, center + offset);

        }

    }

    private void OnDisable()
    {
        if (isTargeting)
        {
            StopTargetingInternal(notifyCancelled: true, restoreMovement: false);

        }

    }

    private void OnDestroy()
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(CancelTargeting);

        }

        if (generatedMaterial != null)
        {
            Destroy(generatedMaterial);

        }

        if (explosionMaterial != null)
        {
            Destroy(explosionMaterial);

        }

        if (Instance == this)
        {
            Instance = null;

        }

    }

}
