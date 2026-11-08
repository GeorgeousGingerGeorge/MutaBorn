using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Play Mode only. Draws trap hit tiles without adding colliders or altering gameplay.
[RequireComponent(typeof(WorldTrap))]
public sealed class WorldTrapHitAreaDebug : MonoBehaviour
{
    [Header("Play Mode Hit Area Preview")]
    [SerializeField] private bool showInGame = true;
    [SerializeField] private bool showWhenDisarmed = true;
    [SerializeField] private Color armedColor = new Color(1f, 0.15f, 0.1f, 0.35f);
    [SerializeField] private Color disarmedColor = new Color(0.4f, 0.4f, 0.4f, 0.25f);
    [SerializeField, Range(0.2f, 1f)] private float tileFill = 0.9f;
    [SerializeField] private float heightAboveNode = 0.6f;
    [SerializeField, Min(0.05f)] private float refreshSeconds = 0.2f;

    private readonly List<GridNode> nodes = new List<GridNode>();
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int> triangles = new List<int>();
    private readonly List<Vector2> uvs = new List<Vector2>();

    private WorldTrap trap;
    private GameObject overlay;
    private Mesh mesh;
    private Material material;
    private float nextRefresh;

    private void Awake()
    {
        trap = GetComponent<WorldTrap>();

    }

    private void Update()
    {
        bool visible = showInGame && (trap.IsArmed || showWhenDisarmed);
        if (!visible)
        {
            if (overlay != null)
            {
                overlay.SetActive(false);

            }

            return;

        }

        if (Time.unscaledTime < nextRefresh)
        {

            return;

        }

        nextRefresh = Time.unscaledTime + refreshSeconds;
        trap.GetHitAreaNodes(nodes);
        if (nodes.Count == 0 || !EnsureOverlay())
        {
            if (overlay != null)
            {
                overlay.SetActive(false);

            }

            return;

        }

        overlay.SetActive(true);
        Color tint = trap.IsArmed ? armedColor : disarmedColor;
        material.color = tint;
        BuildMesh();

    }

    private bool EnsureOverlay()
    {
        if (overlay != null)
        {

            return true;

        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");

        }

        if (shader == null)
        {
            Debug.LogWarning("Trap debug overlay needs a transparent shader.", this);

            return false;

        }

        overlay = new GameObject("Trap Hit Tiles (Debug Only)");
        overlay.layer = 2; // Ignore Raycast
        overlay.transform.SetParent(transform, false);
        overlay.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        MeshFilter filter = overlay.AddComponent<MeshFilter>();
        MeshRenderer renderer = overlay.AddComponent<MeshRenderer>();
        mesh = new Mesh { name = "Trap Hit Tile Preview" };
        mesh.MarkDynamic();
        filter.sharedMesh = mesh;
        material = new Material(shader) { name = "Trap Hit Tile Debug Material" };
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return true;

    }

    private void BuildMesh()
    {
        GridManager grid = FindAnyObjectByType<GridManager>();
        if (grid == null)
        {

            return;

        }

        float half = grid.GridCellSize * tileFill * 0.5f;
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        foreach (GridNode node in nodes)
        {
            Vector3 center = node.cellWorldPosition + Vector3.up * heightAboveNode;
            int index = vertices.Count;
            vertices.Add(overlay.transform.InverseTransformPoint(center + new Vector3(-half, 0f, -half)));
            vertices.Add(overlay.transform.InverseTransformPoint(center + new Vector3(-half, 0f, half)));
            vertices.Add(overlay.transform.InverseTransformPoint(center + new Vector3(half, 0f, half)));
            vertices.Add(overlay.transform.InverseTransformPoint(center + new Vector3(half, 0f, -half)));
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
            uvs.Add(Vector2.zero);
            uvs.Add(Vector2.up);
            uvs.Add(Vector2.one);
            uvs.Add(Vector2.right);

        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

    }

    private void OnDestroy()
    {
        if (mesh != null)
        {
            Destroy(mesh);

        }

        if (material != null)
        {
            Destroy(material);

        }

    }

}
