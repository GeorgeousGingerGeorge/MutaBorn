using System.Collections.Generic;
using UnityEngine;

public class ChunkPrefabTest : MonoBehaviour
{
    [Header("Test References")]
    [SerializeField] private ChunkPrefab authoring;
    [SerializeField] private PrefabDefinition prefabDefinition;

    [Header("Grid Settings")]
    [SerializeField] private float gridCellSize = 2f;

    [ContextMenu("Run Chunk Prefab Test")]
    public void RunTest()
    {
        if (authoring == null || prefabDefinition == null)
        {
            Debug.LogError("Missing test references.", this);
            return;
        }

        Debug.Log("========== CHUNK PREFAB TEST ==========");

        bool valid = authoring.TryCollectPrefabData(
            prefabDefinition,
            gridCellSize,
            out List<PrefabFloorData> floors
        );

        int totalTiles = 0;

        foreach(PrefabFloorData floor in floors)
        {
            if (floor.definition == null)
                continue;

            int expectedTiles =
                floor.definition.GridSize.x *
                floor.definition.GridSize.y;

            int actualTiles = floor.tiles.Count;

            totalTiles += actualTiles;

            HashSet<Vector2Int> coordinates = new();

            bool hasDuplicateCoordinates = false;

            foreach (PrefabTileData tile in floor.tiles)
            {
                if (!coordinates.Add(tile.localCoordinates))
                {
                    hasDuplicateCoordinates = true;

                    Debug.LogError(
                        $"Duplicate coordinate {tile.localCoordinates} " +
                        $"on Floor {floor.definition.LocalFloorID}."
                    );
                }
            }

            Debug.Log(
                $"Floor {floor.definition.LocalFloorID}\n" +
                $"Expected dimensions: {floor.definition.GridSize}\n" +
                $"Expected tiles: {expectedTiles}\n" +
                $"Collected tiles: {actualTiles}\n" +
                $"Duplicate coordinates: {hasDuplicateCoordinates}"
            );

            foreach (PrefabTileData tile in floor.tiles)
            {
                Debug.Log(
                    $"Tile: {tile.tileController.name} | " +
                    $"Local Grid Position: {tile.localCoordinates}",
                    tile.tileController
                );
            }
        }

        Debug.Log(
            $"========== TEST RESULT: {(valid ? "VALID" : "FAILED")} ==========\n" +
            $"Floors collected: {floors.Count}\n" +
            $"Total tiles collected: {totalTiles}"
        );
    }
}
