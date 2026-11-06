using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Floating world-space presentation only; mission progression stays in MissionObjectiveManager.
[DisallowMultipleComponent]
public sealed class MissionObjectiveWorldMarkerUI : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private Image background;
    [SerializeField] private Image accentStrip;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private Image pointerDiamond;
    [SerializeField] private LineRenderer tetherLine;

    [Header("Colors")]
    [SerializeField] private Color primaryColor = new Color(0.35f, 0.80f, 1f, 1f);
    [SerializeField] private Color optionalColor = new Color(1f, 0.79f, 0.42f, 1f);

    private Component target;
    private UnitController targetUnit;
    private Renderer targetRenderer;
    private MissionExtractionZone extractionZone;
    private MissionObjectiveMarkerAnchor customAnchor;
    private Camera targetCamera;
    private CanvasGroup canvasGroup;
    private Material tetherMaterial;
    private Color markerColor;
    private float heightOffset;
    private float canvasScale;
    private float bobAmplitude;
    private float bobSpeed;
    private float bobPhase;
    private float forwardOffset;
    private float tetherWidth;
    private bool showTether;
    private bool initialized;

    public void Configure(Component newTarget, string title, bool optional, Sprite icon,
        float height, float scale, float amplitude, float speed,
        float cameraOffset, bool showLine, float lineWidth)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

        }

        if (target != newTarget || !initialized)
        {
            target = newTarget;
            targetUnit = newTarget != null ? newTarget.GetComponentInParent<UnitController>() : null;
            extractionZone = newTarget as MissionExtractionZone;
            customAnchor = newTarget != null ? newTarget.GetComponentInParent<MissionObjectiveMarkerAnchor>() : null;
            targetRenderer = ResolveVisualRenderer();
            bobPhase = newTarget != null ? (newTarget.GetEntityId().GetHashCode() & 255) * 0.025f : 0f;
            initialized = true;

        }

        heightOffset = Mathf.Max(0f, height);
        canvasScale = Mathf.Max(0.0001f, scale);
        bobAmplitude = Mathf.Max(0f, amplitude);
        bobSpeed = Mathf.Max(0f, speed);
        forwardOffset = Mathf.Max(0f, cameraOffset);
        tetherWidth = Mathf.Max(0.005f, lineWidth);
        showTether = showLine;
        markerColor = optional ? optionalColor : primaryColor;

        if (background != null)
        {
            background.color = new Color(0.035f, 0.055f, 0.085f, optional ? 0.88f : 0.93f);

        }

        if (accentStrip != null)
        {
            accentStrip.color = markerColor;

        }

        if (pointerDiamond != null)
        {
            pointerDiamond.color = markerColor;

        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.color = Color.white;

        }

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(title) ? "Objective" : title;

        }

        if (categoryText != null)
        {
            categoryText.text = optional ? "OPTIONAL" : "PRIMARY";
            categoryText.color = markerColor;

        }

        // Health bars use world-space Canvas sorting order 100 in this project.
        // The objective card should render on top of those.
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 150;

        }

        ConfigureTether();
        UpdatePosition();

    }

    private Renderer ResolveVisualRenderer()
    {
        if (targetUnit != null)
        {
            SkinnedMeshRenderer model = targetUnit.GetComponentInChildren<SkinnedMeshRenderer>();
            if (model != null)
            {
                return model;

            }

            return targetUnit.GetComponentInChildren<MeshRenderer>();

        }

        if (target == null)
        {
            return null;

        }

        Renderer direct = target.GetComponent<Renderer>();
        return direct != null ? direct : target.GetComponentInChildren<MeshRenderer>();

    }

    private void LateUpdate()
    {
        UpdatePosition();

    }

    private void UpdatePosition()
    {
        if (target == null)
        {
            if (tetherLine != null)
            {
                tetherLine.enabled = false;

            }

            return;

        }

        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
        {
            targetCamera = Camera.main;

        }

        if (targetRenderer == null)
        {
            // Race models can be instantiated after the combat unit root.
            targetRenderer = ResolveVisualRenderer();

        }

        Vector3 anchorPoint;
        Vector3 bannerPosition;
        if (extractionZone != null)
        {
            if (!extractionZone.TryGetWorldMarkerPosition(out anchorPoint))
            {
                if (tetherLine != null)
                {
                    tetherLine.enabled = false;

                }

                return;

            }

            bannerPosition = anchorPoint + Vector3.up * (heightOffset + HalfBannerHeight());

        }
        else if (targetUnit != null)
        {
            Vector3 root = targetUnit.transform.position;
            float top = targetRenderer != null ? targetRenderer.bounds.max.y : root.y + 2f;
            anchorPoint = targetRenderer != null
                ? new Vector3(targetRenderer.bounds.center.x,
                    targetRenderer.bounds.center.y + targetRenderer.bounds.extents.y * 0.5f,
                    targetRenderer.bounds.center.z)
                : root + Vector3.up * 1.6f;
            bannerPosition = new Vector3(root.x, top + heightOffset + HalfBannerHeight(), root.z);

        }
        else
        {
            Vector3 root = target.transform.position;
            float top = targetRenderer != null ? targetRenderer.bounds.max.y : root.y;
            anchorPoint = targetRenderer != null ? targetRenderer.bounds.center : root;
            bannerPosition = new Vector3(root.x, top + heightOffset + HalfBannerHeight(), root.z);

        }

        if (customAnchor != null)
        {
            // The optional custom anchor sets the banner location explicitly.
            bannerPosition = customAnchor.WorldPosition;

        }

        bannerPosition += Vector3.up * (Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobAmplitude);
        if (targetCamera != null)
        {
            Vector3 towardCamera = targetCamera.transform.position - bannerPosition;
            if (towardCamera.sqrMagnitude > 0.0001f)
            {
                bannerPosition += towardCamera.normalized * forwardOffset;

            }

            transform.rotation = targetCamera.transform.rotation;

        }

        transform.position = bannerPosition;
        transform.localScale = Vector3.one * canvasScale;
        UpdateTether(anchorPoint);

    }

    private float HalfBannerHeight()
    {
        RectTransform rect = transform as RectTransform;
        return rect != null ? rect.rect.height * canvasScale * 0.5f : 0.7f;

    }

    private void ConfigureTether()
    {
        if (!showTether)
        {
            if (tetherLine != null)
            {
                tetherLine.enabled = false;

            }

            return;

        }

        if (tetherLine == null)
        {
            Transform child = transform.Find("Objective Tether");
            if (child != null)
            {
                tetherLine = child.GetComponent<LineRenderer>();

            }

            if (tetherLine == null)
            {
                GameObject tether = new GameObject("Objective Tether", typeof(LineRenderer));
                tether.transform.SetParent(transform, false);
                tetherLine = tether.GetComponent<LineRenderer>();

            }

        }

        if (tetherMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            }

            if (shader == null)
            {
                Debug.LogWarning("Objective tether shader unavailable; banner still works.", this);
                tetherLine.enabled = false;
                return;

            }

            tetherMaterial = new Material(shader);
            tetherMaterial.name = "Objective Tether Material (Runtime)";
            tetherLine.sharedMaterial = tetherMaterial;

        }

        if (tetherMaterial.HasProperty("_Color"))
        {
            tetherMaterial.SetColor("_Color", markerColor);

        }

        if (tetherMaterial.HasProperty("_BaseColor"))
        {
            tetherMaterial.SetColor("_BaseColor", markerColor);

        }

        tetherLine.useWorldSpace = true;
        tetherLine.positionCount = 2;
        tetherLine.startWidth = tetherWidth;
        tetherLine.endWidth = tetherWidth * 0.65f;
        tetherLine.alignment = LineAlignment.View;
        tetherLine.startColor = markerColor;
        tetherLine.endColor = new Color(markerColor.r, markerColor.g, markerColor.b, 0.7f);
        tetherLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tetherLine.receiveShadows = false;
        tetherLine.sortingOrder = 149;
        tetherLine.enabled = true;

    }

    private void UpdateTether(Vector3 anchorPoint)
    {
        if (!showTether || tetherLine == null || !tetherLine.enabled)
        {
            return;

        }

        RectTransform rect = transform as RectTransform;
        float halfHeight = rect != null ? rect.rect.height * 0.5f : 175f;
        // The start is the bottom of the billboard; the other end stays on the unit / zone.
        Vector3 start = transform.TransformPoint(new Vector3(0f, -halfHeight - 8f, 0f));
        tetherLine.SetPosition(0, start);
        tetherLine.SetPosition(1, anchorPoint);

    }

    private void OnDestroy()
    {
        if (tetherMaterial != null)
        {
            Destroy(tetherMaterial);

        }

    }
}
