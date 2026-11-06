using System;
using System.Collections.Generic;
using UnityEngine;

// Place this prefab on a mission tile. Its floor ID and rectangular size
// define the only permitted initial deployment tiles. Does not save mid-mission state.
public sealed class MissionCrewSpawnArea : MonoBehaviour
{
    [Header("Area starts at the nearest tile on this floor")]
    [SerializeField] private int floorId = 1;
    [SerializeField] private Vector2Int areaSizeInTiles = new Vector2Int(4, 4);
    [SerializeField, Min(0.1f)] private float gizmoCellSize = 2f;

    [Header("Shared player combat unit prefab (recommended)")]
    [Tooltip("When set, instantiate one fresh UnitController wrapper for every selected crew member. It creates its race model when bound to UnitData. Without this prefab, legacy scene-wrapper fallback remains available.")]
    [SerializeField] private UnitController extraUnitPrefab;

    public void PrepareDeployment(GridManager gridManager)
    {
        UnitRoster roster = UnitRoster.Instance;
        Occupancy occupancy = FindAnyObjectByType<Occupancy>();
        FogOfWarManager fogController = FindAnyObjectByType<FogOfWarManager>();
        Dictionary<Vector2Int, GridNode> floor = gridManager == null ? null : gridManager.GetGrid(floorId);
        if (roster == null || occupancy == null || floor == null || floor.Count == 0)
        {
            Debug.LogError("[CrewSpawn] Missing roster, occupancy or floor grid. Legacy units remain unchanged.", this);

            return;

        }

        List<UnitController> available = new List<UnitController>();
        HashSet<Vector2Int> reserved = new HashSet<Vector2Int>();
        UnitController[] sceneUnits = FindObjectsByType<UnitController>(FindObjectsInactive.Include);
        foreach (UnitController unit in sceneUnits)
        {
            if (unit == null || !unit.gameObject.activeInHierarchy)
            {
                continue;

            }

            if (unit.InitializationMode != UnitInitializationMode.LegacyScene)
            {
                available.Add(unit);

            }
            else
            {
                GridNode anchor = MissionSpawnAreaTiles.FindNearestNode(floor, unit.transform.position);
                if (anchor != null &&
                    Mathf.Abs(anchor.cellWorldPosition.y - unit.transform.position.y) <= gridManager.GridCellSize)
                {
                    MissionSpawnAreaTiles.ReserveFootprint(anchor, unit.footPrint, reserved);

                }

            }

        }

        // This area is opt-in. Scene roster placeholders become reusable
        // deployment wrappers; none will generate a new recruit or spawn twice.
        foreach (UnitController unit in available)
        {
            unit.gameObject.SetActive(false);

        }

        GridNode start = MissionSpawnAreaTiles.FindNearestNode(floor, transform.position);
        if (start == null)
        {
            Debug.LogError("[CrewSpawn] No valid anchor tile on configured floor.", this);

            return;

        }

        int width = Mathf.Max(1, areaSizeInTiles.x);
        int depth = Mathf.Max(1, areaSizeInTiles.y);
        List<GridNode> possible = new List<GridNode>();
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector2Int coords = start.coords + new Vector2Int(x, z);
                if (floor.TryGetValue(coords, out GridNode node))
                {
                    possible.Add(node);

                }

            }

        }

        // Random order means each invalid or occupied tile is skipped and
        // retried without the danger of an infinite random retry loop.
        for (int i = possible.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            GridNode swap = possible[i];
            possible[i] = possible[j];
            possible[j] = swap;

        }

        int placed = 0;
        for (int slot = 0; slot < roster.UnlockedCrewSlots; slot++)
        {
            if (!roster.TryGetDeploymentUnit(slot, out UnitData data))
            {
                continue;

            }

            // Prefer a shared combat prefab. With this assigned, no scene-placed
            // player wrapper is needed: the bound UnitData determines the race model.
            UnitController wrapper = null;
            bool spawnedNew = false;
            if (extraUnitPrefab != null)
            {
                wrapper = Instantiate(extraUnitPrefab, transform.parent);
                spawnedNew = true;

            }
            else
            {
                wrapper = TakeMatchingWrapper(available, data);

            }

            if (wrapper == null)
            {
                UnitController source = null;
                if (source == null && sceneUnits.Length > 0)
                {
                    // Prefer an existing real mission-unit configuration, not
                    // the minimal generic Player prefab with missing components.
                    foreach (UnitController candidate in sceneUnits)
                    {
                        if (candidate != null && candidate.InitializationMode != UnitInitializationMode.LegacyScene)
                        {
                            source = candidate;

                            break;

                        }

                    }

                }

                if (source != null)
                {
                    wrapper = Instantiate(source, transform.parent);
                    spawnedNew = true;

                }

            }

            if (wrapper == null)
            {
                Debug.LogError($"[CrewSpawn] No unit wrapper for crew slot {slot}. Assign Extra Unit Prefab.", this);

                continue;

            }

            bool found = false;
            foreach (GridNode node in possible)
            {
                if (!MissionSpawnAreaTiles.FitsEntirelyInsideArea(node, wrapper.footPrint, start.coords, width, depth, floor) ||
                    !MissionSpawnAreaTiles.IsFootprintFree(node, wrapper.footPrint, reserved) ||
                    !occupancy.IsFootPrintMovable(node, wrapper))
                {
                    continue;

                }

                wrapper.ConfigureCrewDeployment(slot);
                wrapper.transform.position = node.cellWorldPosition;
                MissionSpawnAreaTiles.ReserveFootprint(node, wrapper.footPrint, reserved);
                wrapper.gameObject.SetActive(true);
                found = true;
                placed++;

                break;

            }

            if (!found)
            {
                if (spawnedNew)
                {
                    Destroy(wrapper.gameObject);

                }
                else
                {
                    wrapper.gameObject.SetActive(false);

                }

                Debug.LogWarning($"[CrewSpawn] No unoccupied valid tile for {data.characterName} (slot {slot}); unit NOT deployed.", this);

            }

        }

        Debug.Log($"[CrewSpawn] Placed {placed} selected roster units in {width}x{depth} area on floor {floorId}.", this);

    }

    private static UnitController TakeMatchingWrapper(List<UnitController> pool, UnitData data)
    {
        if (pool.Count == 0)
        {
            return null;

        }

        int chosen = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            if (string.Equals(pool[i].raceName.ToString(), data.raceName, StringComparison.OrdinalIgnoreCase))
            {
                chosen = i;

                break;

            }

        }

        UnitController wrapper = pool[chosen];
        pool.RemoveAt(chosen);

        return wrapper;

    }

    private void OnDrawGizmosSelected()
    {
        Vector2Int size = new Vector2Int(Mathf.Max(1, areaSizeInTiles.x), Mathf.Max(1, areaSizeInTiles.y));
        Gizmos.color = new Color(0.15f, 0.95f, 0.45f, 0.75f);
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Gizmos.DrawWireCube(transform.position + new Vector3(x * gizmoCellSize, 0.05f, z * gizmoCellSize),
                    new Vector3(gizmoCellSize, 0.12f, gizmoCellSize));

            }

        }

    }

}
