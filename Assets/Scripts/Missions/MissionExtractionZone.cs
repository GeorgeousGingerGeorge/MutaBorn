using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class MissionExtractionZone : MonoBehaviour
{
    [Header("Extraction Zone")]
    [Tooltip("Match this to the Extraction objective template's Target Key.")]
    [SerializeField] private string zoneId = "evac";

    [Header("Scene View")]
    [Tooltip("Editor-only visualization. The extraction area has no visible mesh in Game view.")]
    [SerializeField] private Color gizmoColor = new Color(0.15f, 0.95f, 1f, 0.25f);

    [Header("In-Game Outline")]
    [SerializeField] private bool showOutlineInGame = true;
    [SerializeField] private Color outlineColor = new Color(0.2f, 0.95f, 1f, 0.7f);
    [SerializeField, Min(0.01f)] private float outlineWidth = 0.07f;

    private BoxCollider zoneCollider;
    private Material outlineMaterial;

    public string ZoneId => zoneId;

    private void Awake()
    {
        zoneCollider = GetComponent<BoxCollider>();

    }

    private void Start()
    {
        if (showOutlineInGame)
        {
            CreateOutline();

        }

    }

    private void CreateOutline()
    {
        if (zoneCollider == null)
        {
            return;

        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        }

        if (shader == null)
        {
            Debug.LogWarning("Extraction zone outline shader unavailable; the invisible zone still functions.", this);

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

        LineRenderer outline = gameObject.AddComponent<LineRenderer>();
        outline.sharedMaterial = outlineMaterial;
        outline.useWorldSpace = false;
        outline.loop = true;
        outline.positionCount = 4;
        outline.startColor = outlineColor;
        outline.endColor = outlineColor;
        outline.startWidth = outlineWidth;
        outline.endWidth = outlineWidth;
        outline.alignment = LineAlignment.View;
        outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outline.receiveShadows = false;

        Vector3 center = zoneCollider.center;
        Vector3 half = zoneCollider.size * 0.5f;
        float y = center.y - half.y + 0.07f;

        outline.SetPosition(0, new Vector3(center.x - half.x, y, center.z - half.z));
        outline.SetPosition(1, new Vector3(center.x + half.x, y, center.z - half.z));
        outline.SetPosition(2, new Vector3(center.x + half.x, y, center.z + half.z));
        outline.SetPosition(3, new Vector3(center.x - half.x, y, center.z + half.z));

    }

    private void OnDestroy()
    {
        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);

        }

    }

    public bool MatchesZoneId(string requiredId)
    {
        return string.IsNullOrWhiteSpace(requiredId) ||
               string.Equals(zoneId?.Trim(), requiredId.Trim(), System.StringComparison.OrdinalIgnoreCase);

    }

    public bool Contains(UnitController unit)
    {
        if (unit == null || !unit.isActiveAndEnabled ||
            unit.data == null || unit.data.currentHealth <= 0)
        {
            return false;

        }

        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<BoxCollider>();

        }

        if (zoneCollider == null)
        {
            return false;

        }

        // Evaluate the center of the unit in the zone's own coordinate space.
        // No Rigidbody or physics trigger event is needed for this mission objective.
        Vector3 localPoint = transform.InverseTransformPoint(unit.transform.position);
        Vector3 offset = localPoint - zoneCollider.center;
        Vector3 halfSize = zoneCollider.size * 0.5f;

        return Mathf.Abs(offset.x) <= halfSize.x &&
               Mathf.Abs(offset.y) <= halfSize.y &&
               Mathf.Abs(offset.z) <= halfSize.z;

    }

    private void OnDrawGizmos()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider == null)
        {
            return;

        }

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(collider.center, collider.size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
        Gizmos.DrawWireCube(collider.center, collider.size);

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;

    }

    private void OnValidate()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;

        }

    }

}
