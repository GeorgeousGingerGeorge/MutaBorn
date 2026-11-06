using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class MissionFactionSpawnGroup
{
    [Tooltip("A working mission UnitController prefab set to LegacyScene; race, starting gear and mutations come from its Inspector defaults.")]
    public UnitController unitPrefab;
    [Min(0)] public int count = 1;

}

// Place in a mission scene to spawn enemy/neutral/civilian units.
// Player-owned units must continue using MissionCrewSpawnArea + UnitRoster.
public sealed class MissionFactionSpawnArea : MonoBehaviour
{
    [Header("Mission faction (not a roster source)")]
    [SerializeField] private FactionData faction;
    [Header("Spawn rectangle")]
    [SerializeField] private int floorId = 1;
    [SerializeField] private Vector2Int areaSizeInTiles = new Vector2Int(4, 4);
    [SerializeField, Min(0.1f)] private float gizmoCellSize = 2f;
    [Header("Mission units to create")]
    [SerializeField] private List<MissionFactionSpawnGroup> groups = new List<MissionFactionSpawnGroup>();

    private bool prepared;

    public void PrepareDeployment(GridManager gridManager)
    {
        if (prepared)
        {
            return;

        }

        prepared = true;
        if (faction == null || gridManager == null)
        {
            Debug.LogError("[FactionSpawn] Assign a FactionData and a GridManager.", this);

            return;

        }

        Occupancy occupancy = FindAnyObjectByType<Occupancy>();
        Dictionary<Vector2Int, GridNode> floor = gridManager.GetGrid(floorId);
        if (occupancy == null || floor == null || floor.Count == 0)
        {
            Debug.LogError($"[FactionSpawn] No Occupancy or grid nodes on floor {floorId}.", this);

            return;

        }

        GridNode start = MissionSpawnAreaTiles.FindNearestNode(floor, transform.position);
        if (start == null)
        {
            Debug.LogError("[FactionSpawn] Area is not near a valid tile on its floor.", this);

            return;

        }

        int width = Mathf.Max(1, areaSizeInTiles.x);
        int depth = Mathf.Max(1, areaSizeInTiles.y);
        List<GridNode> candidates = MissionSpawnAreaTiles.ShuffledCandidates(floor, start.coords, width, depth);
        HashSet<Vector2Int> reserved = new HashSet<Vector2Int>();
        MissionSpawnAreaTiles.ReserveSceneUnits(floor, gridManager, reserved);

        int placed = 0;
        foreach (MissionFactionSpawnGroup group in groups)
        {
            if (group == null || group.count <= 0)
            {
                continue;

            }

            if (group.unitPrefab == null)
            {
                Debug.LogWarning("[FactionSpawn] Skipped a group with no Unit Prefab.", this);

                continue;

            }

            if (group.unitPrefab.InitializationMode != UnitInitializationMode.LegacyScene ||
                group.unitPrefab.GetComponent<UnitFaction>() == null)
            {
                Debug.LogError($"[FactionSpawn] '{group.unitPrefab.name}' must be LegacyScene and have a UnitFaction component.", this);

                continue;

            }

            for (int i = 0; i < group.count; i++)
            {
                GridNode chosen = null;
                foreach (GridNode node in candidates)
                {
                    if (!MissionSpawnAreaTiles.FitsEntirelyInsideArea(node, group.unitPrefab.footPrint,
                        start.coords, width, depth, floor) ||
                        !MissionSpawnAreaTiles.IsFootprintFree(node, group.unitPrefab.footPrint, reserved) ||
                        !occupancy.IsFootPrintMovable(node, group.unitPrefab))
                    {
                        continue;

                    }

                    chosen = node;

                    break;

                }

                if (chosen == null)
                {
                    Debug.LogWarning($"[FactionSpawn] Not enough free tiles for {group.unitPrefab.name} in '{name}'. Skipped remaining units.", this);

                    break;

                }

                // Instantiate only once we have a valid tile. The standard
                // GridManager.InitializeUnits() performs LegacyScene initialization
                // and race-model creation AFTER all spawn areas are prepared.
                UnitController unit = Instantiate(group.unitPrefab, chosen.cellWorldPosition,
                    group.unitPrefab.transform.rotation, transform.parent);
                unit.GetComponent<UnitFaction>().SetFaction(faction);
                if (group.count > 1)
                {
                    unit.unitName = $"{group.unitPrefab.unitName} {i + 1}";
                    unit.gameObject.name = unit.unitName;

                }

                MissionSpawnAreaTiles.ReserveFootprint(chosen, unit.footPrint, reserved);
                placed++;

            }

        }

        Debug.Log($"[FactionSpawn] Placed {placed} {faction.DisplayName} mission units on floor {floorId}.", this);

    }

    private void OnDrawGizmosSelected()
    {
        Vector2Int size = new Vector2Int(Mathf.Max(1, areaSizeInTiles.x), Mathf.Max(1, areaSizeInTiles.y));
        Gizmos.color = faction != null ? faction.Color : new Color(1f, 0.4f, 0.2f, 0.8f);
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Gizmos.DrawWireCube(transform.position +
                    new Vector3(x * gizmoCellSize, 0.05f, z * gizmoCellSize),
                    new Vector3(gizmoCellSize, 0.12f, gizmoCellSize));

            }

        }

    }

}
