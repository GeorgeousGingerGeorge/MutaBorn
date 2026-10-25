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
    private Func<RaycastHit, bool> acceptsHit;
    private Action targetingCancelled;
    private Func<bool> canContinue;
    private SelectionManager selectionManager;
    private Material generatedMaterial;
    private bool madeRangeRing;

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
        bool showRangeIndicator = true)
    {
        if (!isActiveAndEnabled || requester == null || source == null ||
            source.data == null || onTargetSelected == null)
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
        targetSelected = onTargetSelected;
        acceptsHit = targetFilter;
        targetingCancelled = onCancelled;
        canContinue = stillAllowed;
        startedOnFrame = Time.frameCount;
        isTargeting = true;

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
                "Left-Click a Target | Esc to Cancel";

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

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;

        }

        Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(
            ray, 1000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

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
        SetVisuals(false);

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

        if (Instance == this)
        {
            Instance = null;

        }

    }

}
