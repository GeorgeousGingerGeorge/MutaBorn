
using System.Collections.Generic;
using UnityEngine;

public class LevelAssembler : MonoBehaviour
{


    public bool TryAssemble(GeneratedLevelLayout layout, Transform rootTransform, float gridCellSize)
    {
        if (layout == null || rootTransform == null)
        {
            Debug.Log("Generated Level Layout is null. ");
            return false;
        }

        foreach (ChunkInstance chunk in layout.chunks)
        {
            if (!TryInstantiateChunk(chunk, rootTransform, gridCellSize))
            {
                Debug.Log($"Failed to assemble chunk: {chunk.chunkDefinition.ChunkID}");
                return false;
            }
        }

        return true;
    }

    private bool TryInstantiateChunk(ChunkInstance chunk, Transform generatedLevelRoot, float gridCellSize)
    {
        if (chunk == null || chunk.prefabDefinition == null || chunk.prefabDefinition.Prefab == null || generatedLevelRoot == null || gridCellSize <= 0)
        {
            Debug.Log($"Chunk Does not exist");
            return false;
        }

        Vector2 prefabGridOrigin = chunk.chunkGridOrigin + chunk.prefabGridOffset;

        Vector3 localPosition = new Vector3(prefabGridOrigin.x * gridCellSize, 0f, prefabGridOrigin.y * gridCellSize);
        Vector3 worldPosition = generatedLevelRoot.TransformPoint(localPosition);

        Quaternion rotation = generatedLevelRoot.rotation;

        GameObject instance = Instantiate(chunk.prefabDefinition.Prefab, worldPosition, rotation, generatedLevelRoot);
        MissionCrewSpawnArea[] spawnAreas =
         instance.GetComponentsInChildren<MissionCrewSpawnArea>(true);

        foreach (MissionCrewSpawnArea spawnArea in spawnAreas)
        {
            Debug.Log(
                $"Spawn Area: {spawnArea.name}\n" +
                $"Is Child Of Chunk: {spawnArea.transform.IsChildOf(instance.transform)}\n" +
                $"Local Position: {spawnArea.transform.localPosition}\n" +
                $"World Position: {spawnArea.transform.position}",
                spawnArea
            );
        }

        chunk.prefabInstance = instance;

        ChunkPrefab assignedChunk = instance.GetComponent<ChunkPrefab>();

        if (assignedChunk == null)
        {
            Debug.Log($"Prefab {instance.name} missing ChunkPrefab Component", instance);
            Destroy(instance);

            chunk.prefabInstance = null;
            return false;

        }

        if (!assignedChunk.TryCollectPrefabData(chunk.prefabDefinition, gridCellSize, out List<PrefabFloorData> collectedFloors))
        {
            Debug.Log($"Failed to extract the prefab's info ->  {instance.name}", instance);
            Destroy(instance);
            chunk.prefabInstance = null;
            return false;
        }

        PopulateChunkFloors(chunk, collectedFloors, gridCellSize, generatedLevelRoot);
        if (!PopulateSocketWorldPositions(chunk)) 
        {
            Destroy(instance);
            chunk.prefabInstance = null;
            return false;
        }

        return true;
    }

    private void PopulateChunkFloors(ChunkInstance chunk, List<PrefabFloorData> floors, float gridCellSize, Transform generatedLevelRoot)
    {
        chunk.floors.Clear();

        foreach (PrefabFloorData floorData in floors)
        {

            if (floorData == null || floorData.definition == null)
            {
                Debug.Log($"Invalid PrefabFloorData");
                continue;
            }

            ChunkFloorInstance floor = new ChunkFloorInstance
            {
                sourceDefinition = floorData.definition,
                localFloorID = floorData.definition.LocalFloorID,
                globalFloorID = -1,
                worldPosition = floorData.floorTransform.position,
                gridSize = floorData.definition.GridSize,
                floorTransform = floorData.floorTransform,
                cells = new List<GridCellDescriptor>()

            };
            foreach (PrefabTileData tiledata in floorData.tiles)
            {

                if (tiledata.tileController == null || tiledata.tileTransform == null)
                {
                    continue;
                }


                Vector3 tileWorldPosition = tiledata.tileTransform.position;
                Vector3 levelLocalPosition = generatedLevelRoot.InverseTransformPoint(tileWorldPosition);
                Vector2Int globalCoordinates = new Vector2Int(Mathf.RoundToInt(levelLocalPosition.x / gridCellSize), Mathf.RoundToInt(levelLocalPosition.z / gridCellSize));

                GridCellDescriptor cell = new GridCellDescriptor
                {
                    globalFloorID = -1,
                    localCoordinates = tiledata.localCoordinates,
                    globalCoordinates = globalCoordinates,
                    worldPosition = tileWorldPosition,
                    tile = tiledata.tileController
                };

                floor.cells.Add(cell);
            }
            chunk.floors.Add(floor);
        }
    }
    private bool PopulateSocketWorldPositions(ChunkInstance chunk) 
    {
        foreach(ChunkSocketInstance socket in chunk.sockets) 
        {

            ChunkFloorInstance socketFloor = null;

            foreach(ChunkFloorInstance floor in chunk.floors) 
            {
                if (floor.localFloorID == socket.localFloorID) 
                {
                    socketFloor = floor;
                    break;
                }
            }

            if (socketFloor == null) 
            {
                return false;
            }

            GridCellDescriptor socketCell = null;

            foreach (GridCellDescriptor cell in socketFloor.cells) 
            {
                if (cell.localCoordinates == socket.sourceSocket.gridPosition) 
                {
                    socketCell = cell;

                    break;
                }
            }
            if(socketCell == null) 
            {
                return false;
            }

            socket.worldPosition = socketCell.worldPosition;
        }

        return true;
    }
}

   