using UnityEngine;

// Animates the editable circle meshes inside the shared impact prefab.
public sealed class ElementImpactRing : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float duration = 0.38f;
    [SerializeField, Min(0.1f)] private float finalDiameter = 3f;
    [SerializeField, Range(0f, 1f)] private float opacity = 0.8f;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock properties;
    private Color tint = Color.white;
    private float elapsed;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        properties = new MaterialPropertyBlock();

    }

    private void OnEnable()
    {
        elapsed = 0f;
        transform.localScale = Vector3.one * 0.1f;

    }

    public void SetColor(Color value)
    {
        tint = value;
        UpdateVisual();

    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        float scale = Mathf.Lerp(0.1f, finalDiameter, 1f - (1f - progress) * (1f - progress));
        transform.localScale = Vector3.one * scale;
        UpdateVisual();

    }

    private void UpdateVisual()
    {
        if (meshRenderer == null)
        {
            return;

        }

        float alpha = opacity * (1f - Mathf.Clamp01(elapsed / duration));
        meshRenderer.enabled = alpha > 0f;
        properties.SetColor("_Color", new Color(tint.r, tint.g, tint.b, alpha));
        meshRenderer.SetPropertyBlock(properties);

    }
}
