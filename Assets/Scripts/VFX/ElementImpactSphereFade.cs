using UnityEngine;

// Editable expanding sphere for area impacts. Color stays elemental until the late fade.
public sealed class ElementImpactSphereFade : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float duration = 1.1f;
    [SerializeField, Min(0.01f)] private float growSeconds = 0.18f;
    [SerializeField, Min(0.1f)] private float finalDiameter = 2f;
    [SerializeField, Range(0f, 1f)] private float startOpacity = 0.2f;
    [SerializeField, Range(0f, 1f)] private float fadeStart = 0.5f;
    [SerializeField, Range(0f, 1f)] private float grayStart = 0.78f;
    [SerializeField] private Color fadeTarget = new Color(0.22f, 0.22f, 0.22f, 0f);

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock properties;
    private Color tint = Color.white;
    private float elapsed;

    public float Duration => duration;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        properties = new MaterialPropertyBlock();

    }

    private void OnEnable()
    {
        elapsed = 0f;
        transform.localScale = Vector3.one * 0.1f;
        UpdateVisual();

    }

    public void SetColor(Color value)
    {
        tint = value;
        UpdateVisual();

    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float growth = Mathf.Clamp01(elapsed / growSeconds);
        float eased = 1f - (1f - growth) * (1f - growth);
        transform.localScale = Vector3.one * Mathf.Lerp(0.1f, finalDiameter, eased);
        UpdateVisual();

    }

    private void UpdateVisual()
    {
        if (meshRenderer == null)
        {
            return;

        }

        float progress = Mathf.Clamp01(elapsed / duration);
        float fade = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(fadeStart, 1f, progress));
        float gray = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(grayStart, 1f, progress));
        Color current = Color.Lerp(tint, fadeTarget, gray);
        current.a = startOpacity * (1f - fade);
        meshRenderer.enabled = current.a > 0.001f;
        properties.SetColor("_BaseColor", current);
        properties.SetColor("_Color", current);
        meshRenderer.SetPropertyBlock(properties);

    }
}
