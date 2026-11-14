using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq.Expressions;



public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Generation Profile")]
    [SerializeField] private LevelGenerationProfile generationProfile;

    [Header("Generation Limit")]
    [SerializeField, Min(1)] private int allocationAttempts = 25;

    private System.Random random;

    public GeneratedLevelLayout generatedLayout { get; private set; }

    public int CurrentSeed { get; private set; }

    public List<ChunkPlacementCandidate> placementCandidates = new List<ChunkPlacementCandidate>();

    public float gridCellSize = 0f;
    
    //Starts Generation

    public bool TryGenerateInitialLayout(float GridCellSize,out GeneratedLevelLayout layout) 
    {
        layout = null;

        if (GridCellSize <= 0f) 
        {
            return false;
        }
        
        gridCellSize = GridCellSize;
        
        placementCandidates.Clear();
        
        if (!ValidateProfile()) 
        {
            return false;
        }

        CurrentSeed = generationProfile.RandomizedSeed ? unchecked((int)DateTime.UtcNow.Ticks) : generationProfile.Seed;

        random = new System.Random(CurrentSeed);

        generatedLayout = new GeneratedLevelLayout { seed = CurrentSeed };
        placementCandidates.Clear();

        int targetChunk = random.Next(generationProfile.MinimumChunks, generationProfile.MaximumChunks + 1);

  

        if (!TryCreateChunk(generationProfile.EntranceChunk, Vector2Int.zero, out ChunkInstance entranceChunk)) 
        {
            Debug.Log($"Failed to create starting Chunk");
            Debug.LogError("[Generation Failure] Entrance creation failed.");
            return false;
        }
        if (!FitsWithinLevelBounds(entranceChunk)) 
        {
            Debug.Log("Instance: EntranceChunk exceed the max level dimensions");
            return false;
        }

        generatedLayout.chunks.Add(entranceChunk);

        if (!RegisterChunkSockets(entranceChunk, gridCellSize)) 
        {
            return false;
        }

        RegisterPlacementCandidates(entranceChunk);

        //Creates OptionalChunks

        int additionalChunkCount = targetChunk - 2;


        for(int currentChunk = 0; currentChunk < additionalChunkCount; currentChunk++) 
        {
            if (!TryCreateAdditionalChunks( out ChunkInstance additionalChunk)) 
            {
                int currentChunkCount = generatedLayout.chunks.Count;

                //Go to Evacuation Chunk
                int finalChunkCount = currentChunkCount + 1;

                if (finalChunkCount >= generationProfile.MinimumChunks) 
                {
                    break;
                }
                Debug.Log($"Failed to create additionalChunk {currentChunk + 1}");
                        Debug.LogError(
               $"[Generation Failure] Additional chunk failed. " +
               $"Placed chunks: {generatedLayout.chunks.Count}, " +
               $"Target chunks: {targetChunk}, Seed: {CurrentSeed}"
           );
                return false;
            }

           
            
        }


        ChunkInstance evacuationChunk = null;
        List<ChunkPlacementCandidate> attemptedEvacuationCandidates = new List<ChunkPlacementCandidate>();

        while(TrySelectPlacementCandidate(attemptedEvacuationCandidates, out ChunkPlacementCandidate evacuationCandidate)) 
        {
          

            attemptedEvacuationCandidates.Add(evacuationCandidate);

            if (evacuationCandidate.sourceSocket.ownerChunk == entranceChunk)
            {
                continue;
            }

            if (TryPlaceChunkAtSocket(generationProfile.EvacuationChunk, evacuationCandidate, out evacuationChunk)) 
            {
                break;
            }
        }

        if (evacuationChunk == null) 
        {
            Debug.LogError(
       $"[Generation Failure] Evacuation placement failed. " +
       $"Placed chunks: {generatedLayout.chunks.Count}, " +
       $"Seed: {CurrentSeed}"
   );

            return false;
        }
       
       

        layout = generatedLayout;
        Debug.Log($"Initial chunk Created Successfully. Seed: {CurrentSeed}");
      

        return true;

    }

 
    //Creates ChunkInstance
    private bool TryCreateChunk(ChunkDefinition definition, Vector2Int chunkOrigin, out ChunkInstance chunk) 
    {
        chunk = null;

        if (definition == null) 
        {
            return false;
        }

        if(!TrySelectPrefab(definition, out PrefabDefinition selectPrefab, out Vector2Int prefabFootprint, out Vector2Int prefabOffset)) 
        {
           return false;
        }

           
        float generationWeight = Mathf.Lerp(definition.MinimumWeight, definition.MaximumWeight, (float)random.NextDouble());

        chunk = new ChunkInstance
        {
            chunkDefinition = definition,
            prefabDefinition = selectPrefab,

            chunkGridOrigin = chunkOrigin,
            allocatedDimensions = prefabFootprint,
            prefabGridOffset = prefabOffset,

            prefabInstance = null,

            floors = new List<ChunkFloorInstance>(),

            generationWeight = generationWeight,

            sockets = new List<ChunkSocketInstance>(),
                
         };
            return true;
        

    
    }

 
    //Goes through all available prefabs for this chunk definition and determines if its correct for it
    private bool TrySelectPrefab(ChunkDefinition definition, 
        out PrefabDefinition selectPrefab, out Vector2Int selectedFootprint, out Vector2Int selectedOffset)
    {
        selectPrefab = null;
        selectedOffset = Vector2Int.zero;
        selectedFootprint = Vector2Int.zero;

        List<PrefabDefinition> candidates = new List<PrefabDefinition>();
        List<Vector2Int> footprints = new List<Vector2Int>();
        List<Vector2Int> offsets = new List<Vector2Int>();

        foreach (PrefabDefinition prefab in definition.AllowedPrefabs)
        {
            if (prefab == null || prefab.Prefab == null) 
            {
                continue;
            }

            if (prefab.Floors == null || prefab.FloorCount < definition.MinimumFloors || prefab.FloorCount > definition.MaximumFloors) 
            {
                continue;
            }

            if (prefab.SelectionWeight <= 0f)
            {
                continue;
            }

            if (!TryGetPrefabFootPrint(prefab,  out Vector2Int footprint, out Vector2Int offset)) 
            {
                continue;
            }


            bool dimensionsValid = footprint.x >= definition.MinimumDimensions.x && footprint.y >= definition.MinimumDimensions.y &&
                footprint.x <= definition.MaximumDimensions.x && footprint.y <= definition.MaximumDimensions.y;

            if (!dimensionsValid) 
            {
                continue;
            }
            candidates.Add(prefab);
            footprints.Add(footprint);
            offsets.Add(offset);
        }

        if (candidates.Count == 0) 
        {
            return false;
        }

        float totalWeight = 0f;

        foreach(PrefabDefinition candidate in candidates) 
        {
            totalWeight += candidate.SelectionWeight;
        }

        float selection = (float)random.NextDouble() * totalWeight;
        int selectedIndex = 0;

        for (int i = 0; i < candidates.Count; i++) 
        {
            selection -= candidates[i].SelectionWeight;

            if (selection <= 0f) 
            {
                selectedIndex = i;
                break;
            }
        }

        selectPrefab = candidates[selectedIndex];
        selectedFootprint = footprints[selectedIndex];
        selectedOffset = offsets[selectedIndex];

        return true;
    }

    private bool TrySelectPlacementCandidate(List<ChunkPlacementCandidate> attemptedCandidate, out ChunkPlacementCandidate selectedCandidate) 
    {
        selectedCandidate = null;

        List<ChunkPlacementCandidate> available = new List<ChunkPlacementCandidate>();

        float totalWeight = 0f;

        foreach(ChunkPlacementCandidate candidate in placementCandidates) 
        {
            if (candidate == null || !candidate.IsAvailable || attemptedCandidate.Contains(candidate)) 
            {
                continue;
            }

            available.Add(candidate);
            totalWeight += candidate.currentWeight;


        }

        if (available.Count == 0) 
        {
            return false;
        }

        //selects uniformly from available sockets
        if (totalWeight <= 0f) 
        {
            selectedCandidate = available[random.Next(0, available.Count)];
            return true;
        }

        float selection = (float)random.NextDouble() * totalWeight;

        foreach (ChunkPlacementCandidate candidate in available) 
        {
            selection -= candidate.currentWeight;

            if (selection <= 0f) 
            {
                selectedCandidate = candidate;
                return true;
            }

        }

        selectedCandidate = available[available.Count - 1];
        return true;
    }

    //Verify Prefab Floor 
    public bool TryGetPrefabFootPrint(PrefabDefinition prefab, out Vector2Int footPrint, out Vector2Int prefabOffset) 
    {
        footPrint = Vector2Int.zero;
        prefabOffset = Vector2Int.zero;

        Debug.Log(
    $"[Footprint] Prefab: {prefab.PrefabID} | " +
    $"Floor Count: {prefab.Floors.Count}"
);
        if (prefab == null || prefab.Floors == null || prefab.FloorCount == 0) 
        {
            return false;
        }

        int minimumX = 0;
        int minimumZ = 0;

        int maximumX = 0;
        int maximumZ = 0;

        bool firstFloor = true;

        foreach (PrefabFloorDefinition floor in prefab.Floors) 
        {
            if (floor == null || floor.GridSize.x <= 0 || floor.GridSize.y <= 0)
            {
                return false;            
            }


            if (!TryGetFloorGridOffSet(floor, gridCellSize, out Vector2Int floorOffset)) 
            {
                return false;
            }
            Debug.Log(
    $"[Footprint Debug] Prefab: {prefab.PrefabID}\n" +
    $"Floor ID: {floor.LocalFloorID}\n" +
    $"Grid Size: {floor.GridSize}\n" +
    $"Local Position: {floor.LocalPosition}\n" +
    $"Calculated Offset: {floorOffset}"
);

            int floorMinX = floorOffset.x;
            int floorMinZ = floorOffset.y;

            int floorMaxX = floorOffset.x + floor.GridSize.x;
            int floorMaxZ = floorOffset.y + floor.GridSize.y;

            if (firstFloor) 
            {
                minimumX = floorMinX;
                minimumZ = floorMinZ;
                maximumX = floorMaxX;
                maximumZ = floorMaxZ;

                firstFloor = false;
            }
            else 
            {
                minimumX = Mathf.Min(minimumX, floorMinX);
                minimumZ = Mathf.Min(minimumZ, floorMinZ);
                maximumX = Mathf.Max(maximumX, floorMaxX);
                maximumZ = Mathf.Max(maximumZ, floorMaxZ);
            }

        }

        footPrint = new Vector2Int(maximumX - minimumX, maximumZ - minimumZ);
        prefabOffset = new Vector2Int(-minimumX, -minimumZ);

        Debug.Log(
       $"PREFAB FOOTPRINT: {prefab.PrefabID}\n" +
       $"Footprint: {footPrint}\n" +
       $"Prefab Offset: {prefabOffset}"
   );

        return footPrint.x > 0 && footPrint.y > 0;
    }
    private bool TryGetCompatibleConnectionType(ChunkSocketInstance sourceSocket, PrefabSocket targetSocket, out SocketConnectionType compatibleTypes) 
    {
        compatibleTypes = SocketConnectionType.None;

        if (sourceSocket == null || sourceSocket.isConnected) 
        {
            return false;
        }

        SocketConnectionType horizontalTypes = SocketConnectionType.DoorWay | SocketConnectionType.OpenPassage| SocketConnectionType.PathWay| SocketConnectionType.Special;
        //socket must face each other

        if(targetSocket.direction != -sourceSocket.globalDirection) 
        {
            return false;
        }

        //Compares the two sockets of the current chunk and the target chunk's 
        compatibleTypes = sourceSocket.AllowedConnections & targetSocket.allowedConnections & horizontalTypes;

        return compatibleTypes != SocketConnectionType.None;
    }
    private bool TryCalculateChunkOriginForSocket(ChunkSocketInstance sourceSocket, PrefabSocket candidateSocket, PrefabFloorDefinition candidateFloor,
        Vector2Int candidatePrefabOffset, float gridCellSize, out Vector2Int chunkOrigin)
    {
        chunkOrigin = Vector2Int.zero;

        if (sourceSocket == null || candidateFloor == null || gridCellSize <= 0f) 
        {
            return false;
        }

        Vector2Int targetSocketPosition = sourceSocket.globalGridPosition + sourceSocket.globalDirection;

        if(!TryGetFloorGridOffSet(candidateFloor, gridCellSize, out Vector2Int offset)) 
        {
            Debug.Log($"Could not receive offset");
            return false;
        }

        Vector2Int candidateSocketLocalPosition = offset + candidateSocket.gridPosition;

        Vector2Int prefabOrigin = targetSocketPosition - candidateSocketLocalPosition;

        chunkOrigin = prefabOrigin - candidatePrefabOffset;

        return true;
    }
    private void RegisterPlacementCandidates(ChunkInstance chunk) 
    {
        float nextWeight = Mathf.Max(0f, chunk.generationWeight - chunk.chunkDefinition.InfluenceStrength);

        foreach(ChunkSocketInstance socket in chunk.sockets) 
        {
            if(socket == null || socket.isConnected || socket.AllowedConnections == SocketConnectionType.None) 
            {
                continue;
            }
            bool alreadyRegistered = false;


            foreach (ChunkPlacementCandidate existing in placementCandidates) 
            {
                if(existing.sourceSocket == socket) 
                {
                    alreadyRegistered = true;
                    break;
                }
            }

            if (alreadyRegistered) 
            {
                continue;
            }

            placementCandidates.Add(new ChunkPlacementCandidate(socket, nextWeight));
        }
    }
    private bool RegisterChunkSockets(ChunkInstance chunk, float gridCellSize) 
    {
        if(chunk == null || chunk.prefabDefinition == null) 
        {
            return false;
        }

        chunk.sockets.Clear();

        List<PrefabSocket> prefabSockets = chunk.prefabDefinition.GetPotentialSockets();

        foreach(PrefabSocket prefabSocket in prefabSockets) 
        {
            PrefabFloorDefinition socketFloor = null;

            foreach(PrefabFloorDefinition floor in chunk.prefabDefinition.Floors) 
            {
                if (floor.LocalFloorID == prefabSocket.localFloorID)
                {
                    socketFloor = floor;
                    break;
                }
             
            }

            if (socketFloor == null) 
            {
                Debug.Log($"Missing Floor, {prefabSocket.localFloorID} in {chunk.prefabDefinition.PrefabID}");
                return false;
            }

            if(!TryGetFloorGridOffSet(socketFloor, gridCellSize, out Vector2Int floorGridOffset)) 
            {
                Debug.Log($"Floor {socketFloor.LocalFloorID} has a position");
                return false;
            }
            //Get the Origin of the prefab inside the chunk
            Vector2Int prefabOrigin = chunk.chunkGridOrigin + chunk.prefabGridOffset;

            //Calculate socket position

            Vector2Int globalGridPosition = prefabOrigin + floorGridOffset + prefabSocket.gridPosition;

            ChunkSocketInstance socket = new ChunkSocketInstance
            {
                ownerChunk = chunk,
                socketIndex = chunk.sockets.Count,
                sourceSocket = prefabSocket,

                localFloorID = prefabSocket.localFloorID,
                globalFloorID = -1,
                floorElevation = socketFloor.LocalPosition.y,

                globalGridPosition = globalGridPosition,
                globalDirection = prefabSocket.direction,

                isConnected = false,
                connectedAS = SocketConnectionType.None,
                connectedSocket = null
            };

            chunk.sockets.Add(socket);
        }

       

        return true;
    }
    private bool ValidateProfile()
    {
        if (generationProfile == null)
        {

            Debug.Log($"Missing LevelGenerationProfile");
            return false;
        }

        if (generationProfile.EntranceChunk == null)
        {
            Debug.Log($"LevelGenerationProfile has no startingChunk", this);
            return false;

        }
        if (generationProfile.EvacuationChunk == null)
        {
            Debug.Log($"LevelGenerationProfile has no exitingChunk", this);
            return false;
        }

        if (generationProfile.MinimumChunks < 2 || generationProfile.MaximumChunks < generationProfile.MinimumChunks)
        {
            Debug.Log($"Invalid Maximum Level Dimensions", this);

            return false;
        }

        if (generationProfile.MaximumChunks > 2 && (generationProfile.AvailableChunks == null || generationProfile.AvailableChunks.Count == 0))
        {
            Debug.Log("Invalid Chunk Generation limits");
            return false;
        }

        return true;
    }
    private bool TryPlaceChunkAtSocket(ChunkDefinition definition, ChunkPlacementCandidate sourceCandidate, out ChunkInstance placedChunk) 
    {
        placedChunk = null;

        if (definition == null || generatedLayout == null || sourceCandidate == null || !sourceCandidate.IsAvailable) 
        {
            return false;
        }

        ChunkSocketInstance sourceSocket = sourceCandidate.sourceSocket;

        for (int attempt = 0; attempt < allocationAttempts; attempt++) 
        {
            if(!TryCreateChunk(definition, Vector2Int.zero, out ChunkInstance candidateChunk)) 
            {
                return false;
            }
            List<PrefabSocket> potentialSockets = candidateChunk.prefabDefinition.GetPotentialSockets();

            for(int currSocket = 0; currSocket < potentialSockets.Count; currSocket++) 
            {
                PrefabSocket candidateSocket = potentialSockets[currSocket];

                PrefabFloorDefinition candidateFloor = null; //finds floor belonging to this socket

                foreach(PrefabFloorDefinition floor in candidateChunk.prefabDefinition.Floors) 
                {
                    if (floor.LocalFloorID == candidateSocket.localFloorID) 
                    {
                        candidateFloor = floor;
                        break;
                    }
                }


                if (candidateFloor == null) 
                {
                    continue;
                }

                if (Mathf.Abs(sourceSocket.floorElevation - candidateFloor.LocalPosition.y) > 0.05f) 
                {
                    continue;
                }

                if (!TryGetCompatibleConnectionType(sourceSocket, candidateSocket, out SocketConnectionType compatibleTypes)) 
                {
                    continue;
                }

                SocketConnectionType connectionType = SocketConnectionType.None;

                if ((compatibleTypes & SocketConnectionType.DoorWay) != 0)
                {
                    connectionType = SocketConnectionType.DoorWay;
                }

                else if ((compatibleTypes & SocketConnectionType.OpenPassage) != 0)
                {
                    connectionType = SocketConnectionType.OpenPassage;
                }
                else if ((compatibleTypes & SocketConnectionType.PathWay) != 0) 
                {
                    connectionType = SocketConnectionType.PathWay;
                }
                else if ((compatibleTypes & SocketConnectionType.Special) != 0) 
                {
                    connectionType = SocketConnectionType.Special;
                }

                if(connectionType == SocketConnectionType.None) 
                {
                    continue;
                }

                if (!TryCalculateChunkOriginForSocket(sourceSocket, candidateSocket, candidateFloor, candidateChunk.prefabGridOffset,
                    gridCellSize, out Vector2Int chunkOrigin)) 
                {

                    continue;
                }
               

                candidateChunk.chunkGridOrigin = chunkOrigin;

                //Check level Boundaries;


                if (!FitsWithinLevelBounds(candidateChunk)) 
                {
                    continue;
                }

                
                bool overlaps = false;

                Vector2Int candidateOrigin = candidateChunk.chunkGridOrigin;
                Vector2Int candidateSize = candidateChunk.allocatedDimensions;

                foreach (ChunkInstance existingChunk in generatedLayout.chunks) 
                {
                    Vector2Int existingOrigin = existingChunk.chunkGridOrigin;
                    Vector2Int existingSize = existingChunk.allocatedDimensions;

                    bool overLapsX = candidateOrigin.x < existingOrigin.x + existingSize.x &&
                        candidateOrigin.x + candidateSize.x > existingOrigin.x;

                    bool overLapsZ = candidateOrigin.y < existingOrigin.y + existingSize.y &&
                        candidateOrigin.y + candidateSize.y > existingOrigin.y;

                    if (overLapsX && overLapsZ) 
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (overlaps) 
                {
                    continue;
                }

                if(!RegisterChunkSockets(candidateChunk, gridCellSize)) 
                {
                    continue;
                }

                if (currSocket >= candidateChunk.sockets.Count) 
                {
                    continue;
                }

                ChunkSocketInstance targetSocket = candidateChunk.sockets[currSocket];

                Vector2Int expectedPosition = sourceSocket.globalGridPosition + sourceSocket.globalDirection;

                if(targetSocket.globalGridPosition != expectedPosition) 
                {
                    continue;
                }

                //Make Connection

                sourceSocket.isConnected = true;
                targetSocket.isConnected = true;

                sourceSocket.connectedAS = connectionType;
                targetSocket.connectedAS = connectionType;

                sourceSocket.connectedSocket = targetSocket;
                targetSocket.connectedSocket = sourceSocket;

                sourceCandidate.Placed();

                generatedLayout.chunks.Add(candidateChunk);

                if(definition != generationProfile.EvacuationChunk)
                { 
                    RegisterPlacementCandidates(candidateChunk); 
                
                }

                placedChunk = candidateChunk;

                return true;


            }

           
        }



        //No Compatible, valid placement;
        return false ;
    }
    private bool TryCreateAdditionalChunks(out ChunkInstance chunk) 
    {
        chunk = null;
        if (generationProfile.AvailableChunks == null || generationProfile.AvailableChunks.Count == 0) 
        {
            return false;
        }

        List<ChunkDefinition> candidates = new List<ChunkDefinition>();

        foreach (ChunkDefinition definition in generationProfile.AvailableChunks) 
        {
            if (definition != null) 
            {
                candidates.Add(definition);
            }
        }

        for (int randomdefinition = candidates.Count - 1; randomdefinition > 0; randomdefinition--) 
        {
            int nextdefinition = random.Next(0, randomdefinition + 1);

            (candidates[randomdefinition], candidates[nextdefinition]) = (candidates[nextdefinition], candidates[randomdefinition]);
        }
        List<ChunkPlacementCandidate> attemptedCandidates = new List<ChunkPlacementCandidate>();

        while(TrySelectPlacementCandidate(attemptedCandidates, out ChunkPlacementCandidate sourceCandidate)) 
        {
            attemptedCandidates.Add(sourceCandidate);

            foreach (ChunkDefinition definition in candidates) 
            {
                if(TryPlaceChunkAtSocket(definition, sourceCandidate, out chunk)) 
                {
                    return true;
                }
            }
          
        }

        return false;
    }
    
    private bool TryGetFloorGridOffSet(PrefabFloorDefinition floor, float gridCellSize, out Vector2Int offSet) 
    {
        offSet = Vector2Int.zero;

        float x = floor.LocalPosition.x / gridCellSize;
        float z = floor.LocalPosition.z / gridCellSize;

        int gridX = Mathf.RoundToInt(x);
        int gridZ = Mathf.RoundToInt(z);

        if(Mathf.Abs(x - gridX) > 0.001f || Mathf.Abs(z - gridZ) > 0.001f) 
        {
            return false;
        }

        offSet = new Vector2Int(gridX, gridZ);

        return true;
    }

 
 
  
    private bool FitsWithinLevelBounds(ChunkInstance chunk)
    {
        Vector2Int maximum = generationProfile.MaximumLevelDimension;

        Vector2Int origin = chunk.chunkGridOrigin;
        Vector2Int allocation = chunk.allocatedDimensions;

        Vector2Int end = origin + allocation;

        bool fits =
            origin.x >= 0 &&
            origin.y >= 0 &&
            end.x <= maximum.x &&
            end.y <= maximum.y;


        return fits;
    }
    [ContextMenu("Generate Initial Layout Test")]
    private void GenerateInitialLayoutTest() 
    {
        if (!TryGenerateInitialLayout(gridCellSize,out GeneratedLevelLayout layout)) 
        {
            return;
        }

        ChunkInstance chunk = layout.chunks[0];

        Debug.Log(
            $"Initial Layout Test\n" +
            $"Seed: {layout.seed}\n" +
            $"Chunk: {chunk.chunkDefinition.ChunkID}\n" +
            $"Prefab: {chunk.prefabDefinition.PrefabID}\n" +
            $"Allocation: {chunk.allocatedDimensions}\n" +
            $"Origin: {chunk.chunkGridOrigin}\n", this);
    }
}
