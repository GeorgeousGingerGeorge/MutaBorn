using System.Collections.Generic;
using MutaBorn.Gameplay;
using UnityEngine;

// One prefab-authored square per actual affected grid tile. No changes to trap targeting.
[RequireComponent(typeof(WorldTrap))]
public sealed class WorldTrapAreaVisual : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField, Range(0f, 1f)] private float opacity = 0.25f;
    [SerializeField] private float surfaceHeight = 0.28f;

    private readonly List<GridNode> nodes = new List<GridNode>();
    private WorldTrap trap;
    private GameObject tiles;

    private void Awake()
    {
        trap = GetComponent<WorldTrap>();

    }

    private void Start()
    {
        BuildTiles();

    }

    private void Update()
    {
        if (tiles == null)
        {
            BuildTiles();

        }

        if (tiles != null)
        {
            tiles.SetActive(trap.IsRevealed && trap.IsArmed);

        }

    }

    private void BuildTiles()
    {
        if (tilePrefab == null || trap == null)
        {
            return;

        }

        GridManager grid = FindAnyObjectByType<GridManager>();
        if (grid == null)
        {
            return;

        }

        trap.GetHitAreaNodes(nodes);
        if (nodes.Count == 0)
        {
            return;

        }

        tiles = new GameObject("Hazard Area Tiles");
        tiles.transform.SetParent(transform, false);
        tiles.SetActive(false);

        Color tint = ElementVfxColors.For(trap.TrapDamageType);
        tint.a = opacity;
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        properties.SetColor("_Color", tint);
        properties.SetColor("_BaseColor", tint);

        foreach (GridNode node in nodes)
        {
            GameObject tile = Instantiate(tilePrefab,
                node.cellWorldPosition + Vector3.up * surfaceHeight,
                Quaternion.identity, tiles.transform);
            tile.name = $"Hazard Tile {node.coords.x},{node.coords.y}";
            tile.transform.localScale = new Vector3(grid.GridCellSize, 1f, grid.GridCellSize);
            Renderer renderer = tile.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.SetPropertyBlock(properties);

            }

        }

        tiles.SetActive(trap.IsRevealed && trap.IsArmed);

    }
}
