using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.Overlays;

[System.Serializable]
public struct ConnectorsBetweenLevels 
{
    [Header("FloorConnector Object")]
    [SerializeField]public GameObject connector;
    [Header("CurrentWorldOrientation")]
    [SerializeField] public Vector3 worldPosition;
    [SerializeField] public Quaternion worldRotation;
 
    
}

public class GridManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private Level_Design currentLevel;
    [Header("Adjust Tile Size")]
    [SerializeField] public float gridCellSize = 2f;
    [SerializeField, Min(0f)] private float maxStepHeight = 0.5f;

    [Header("Floor Mapping Information")]

    [SerializeField, Min(0f)]
    private float floorHeightTolerance = 0.05f;
    private const int FirstGlobalFloorID = 1;

    [Header("Procedural Generation Configuration")]
    [SerializeField] private ProceduralLevelGenerator proceduralLevelGenerator;
    [SerializeField] private LevelAssembler levelAssembler;
    public float GridCellSize => gridCellSize;

    [Header("CurrentGrid Information")]
    private Dictionary<Vector2Int, GridNode> grid = new Dictionary<Vector2Int, GridNode>();

    [Header("CurrentGrid Information- Generating a grid (No LevelPrefab)")]

    [SerializeField]public GameObject gridCellPrefab;
    [SerializeField] public GameObject obstaclePrefab;
    [SerializeField] public GameObject wallPrefab;

    public List<FloorIdentification> floorGenerator = new List<FloorIdentification>();
    [SerializeField]public Dictionary<int,List<ConnectorsBetweenLevels>> stairGenerator = new Dictionary<int,List<ConnectorsBetweenLevels>>();

    [Header("CurrentGrid Information- Grid Hierarchy")]
    private GameObject mainGrid;
    private GameObject level;
    private GameObject floorNumber;

    [Header("CurrentGrid Information- Grids for Each Floor")]
    private Dictionary<int, Dictionary<Vector2Int, GridNode>> gridFloors = new Dictionary<int, Dictionary<Vector2Int, GridNode>>();
    public int activeFloorID { get; private set; }

    [Header("CurrentGrid Information- Tile Lookup by Node")]
    private Dictionary<GridNode, TileController> tileLookup = new Dictionary<GridNode, TileController>();


    public void Awake()
    {
        BuildLevel();
        Vector3 positionGrid = new Vector3(2f, 1.26f, 2f);
       
        if (currentLevel == null)
        {
            foreach (TileController tile in tileLookup.Values) 
            {
                tile.SetFogState(FogState.CLEAR);
            }


        
        }


    }

    public void BuildLevel()
    {
        //Checks if currentLevel and the levelPrefab exists, if it doesn't it would switch to the other generator

        if (proceduralLevelGenerator != null && levelAssembler != null) 
        {
            BuildFloor_Procedural();
        }
        else if (currentLevel != null && currentLevel.levelPrefab != null)
        {
            mainGrid = new GameObject($"{currentLevel.levelName}_Grid");
            mainGrid.transform.position = Vector3.zero;

            floorNumber = Instantiate(currentLevel.levelPrefab, Vector3.zero, Quaternion.identity, mainGrid.transform);
            floorNumber.transform.SetParent(mainGrid.transform, false);


            foreach (PlacedFloor floorData in currentLevel.placedFloors)
            {
                BuildFloor_Prefab(floorData.floorIndex);
            }
        }
        else
        {
            mainGrid = new GameObject($"Generated_Grid");
            mainGrid.transform.position = Vector3.zero;

            level = new GameObject($"level");
            level.transform.position = Vector3.zero;
            level.transform.SetParent(mainGrid.transform, false);

            foreach (FloorIdentification floorData in floorGenerator)
            {
                BuildFloor_Generated(floorData);
            }
        }

        ConnectFloors();
        InitializeObstacles();

        // A placed crew area takes over ONLY the player's roster placeholders.
        // Unmodified sandbox scenes continue to use their original unit setup.
        MissionCrewSpawnArea crewArea = FindAnyObjectByType<MissionCrewSpawnArea>();
        if (crewArea != null && crewArea.isActiveAndEnabled)
        {
            crewArea.PrepareDeployment(this);

        }

        // Spawn scene-owned faction units separately from the permanent player roster.
        // This happens before the usual initialization/occupancy registration pass.
        MissionFactionSpawnArea[] factionAreas = FindObjectsByType<MissionFactionSpawnArea>();
        foreach (MissionFactionSpawnArea factionArea in factionAreas)
        {
            if (factionArea != null && factionArea.isActiveAndEnabled)
            {
                factionArea.PrepareDeployment(this);

            }

        }

        InitializeUnits();

        if (gridFloors.Count > 0)
        {
            int lowestFloor = int.MaxValue;

            foreach (int floorID in gridFloors.Keys)
            {
                lowestFloor = Mathf.Min(lowestFloor, floorID);
            }

            activeFloorID = lowestFloor;
        }
    }

    

    public void BuildFloor_Generated(FloorIdentification data)   // Build's procedurally generated grid
    {
        CreateGrid_Level_Generated(data); 
    }
    public void BuildFloor_Prefab(FloorIdentification data)  //Builds a prefab Grid
    {
        CreateGrid_Level_Prefab(data);
    }
    public void BuildFloor_Procedural()
    {
        if (!proceduralLevelGenerator.TryGenerateInitialLayout(out GeneratedLevelLayout layout))
        {
            return;
        }

        mainGrid = new GameObject("Procedural Grid");

        if (!levelAssembler.TryAssemble(layout, mainGrid.transform, gridCellSize)) 
        {
            Debug.Log($"Failed to assemble generated level");
            return;
        }

        if (!ResolveGlobalFloorMappings(layout)) 
        {
            Debug.Log($"Failed to resolve global floor mappings");
            return;
        }

        if (!CreateGrid_FromLayout(layout)) 
        {
            Debug.Log("Failed to create a grid from layout");
            return;
        }
        
    }

    //used for when we procedurally generate tiles later 
    public void CreateGrid_Level_Generated(FloorIdentification floor) 
    {
        floorNumber = new GameObject($"Floor_{floor.GetFloorID()}");
        floorNumber.transform.position = Vector3.zero;
        floorNumber.transform.SetParent(level.transform, false);

        for (int gridX = 0; gridX < floor.GetGridSize().x; gridX++) 
        {
            GameObject floorSlice = new GameObject($"FloorSlice_{gridX}");
            floorSlice.transform.SetParent(floorNumber.transform, false);
            for (int gridZ = 0; gridZ < floor.GetGridSize().y; gridZ++) 
            {

                Vector3 worldPos = floor.GetWorldPosition();
                worldPos = worldPos + new Vector3((gridX) * gridCellSize, 0f, (gridZ) * gridCellSize);
              

                Vector2Int coords = new Vector2Int(gridX, gridZ);
                GridNode currentNode = new GridNode(coords, floor.GetFloorID(), worldPos);
                currentNode.SetTerrain(TerrainType.OpenGround);
               
                GameObject tilePrefab = Instantiate(gridCellPrefab, worldPos, Quaternion.identity, floorSlice.transform);
                tilePrefab.transform.SetParent(floorSlice.transform, false);
               

                if (tilePrefab.GetComponentInParent<TileController>()) 
                {
                    TileController currentTile = tilePrefab.GetComponentInParent<TileController>();
                    currentTile.Initialize(currentNode);
                    RegisterTile(currentTile);


                }

                grid.Add(coords, currentNode);
            }
        }
        GameObject stairNumber = new GameObject($"Stair_{floor.GetFloorID()}");
        stairNumber.transform.SetParent(floorNumber.transform, false);

        if(stairGenerator.TryGetValue(floor.GetFloorID(), out List<ConnectorsBetweenLevels> stairPerFloor))
        {
       
            for (int thisStair = 0; thisStair < stairPerFloor.Count; thisStair++)
            {
                Vector3 worldPos = stairPerFloor[thisStair].worldPosition;
                Quaternion worldRotation = stairPerFloor[thisStair].worldRotation;

                stairPerFloor[thisStair].connector.GetComponent<FloorConnector>().SetWorldPosition(worldPos);
                GameObject stair = Instantiate(stairPerFloor[thisStair].connector, worldPos, worldRotation, stairNumber.transform);
                stair.transform.SetParent(floorNumber.transform, false);
                

            }
        }

        SetUpConnections(grid);
        gridFloors.Add(floor.GetFloorID(), grid);
        grid = new Dictionary<Vector2Int, GridNode>();
    }
    //used for when we create level using a prefab
    public void CreateGrid_Level_Prefab(FloorIdentification floor)
    {
        Transform thisFloor = floorNumber.transform.Find($"Floor_{floor.GetFloorID()}");

        int gridX = 0;
        int gridZ = 0;

        foreach (Transform floorSlice in thisFloor)
        {
            foreach (Transform sliceObject in floorSlice)
            {
                if (!sliceObject.GetComponentInParent<TileController>())
                {
                    continue;
                }
                if (gridZ == floor.GetGridSize().y)
                {
                    gridZ = 0;
                }
                Vector3 worldPos = sliceObject.position;


                Vector2Int coords = new Vector2Int(gridX, gridZ);
                GridNode currentNode = new GridNode(coords, floor.GetFloorID(), worldPos);

                //Add Components to the Tile(sliceObject) that we are manipulating in the grid


                if (sliceObject.GetComponentInParent<TileController>())
                {
                    TileController currentTile = sliceObject.GetComponentInParent<TileController>();
                    currentTile.Initialize(currentNode);
                    RegisterTile(currentTile);
                    currentNode.SetTerrain(currentTile.terrainType);

                }

                //Adds the node to the grid
                grid.Add(coords, currentNode);

                gridZ++;

            }
            gridX++;

        }

        //We need to set up connections for all the nodes in the grid
        SetUpConnections(grid);
        gridFloors.Add(floor.GetFloorID(), grid);
        grid = new Dictionary<Vector2Int, GridNode>(); //Since grid has been added, we declare a new grid
    }
    

    //Sets Up Connections between all the nodes in the Grid (8-Directions)
    public void SetUpConnections(Dictionary<Vector2Int, GridNode> currentGrid) 
    {
        int xValue = 1;
        int yValue = 1;
        Vector2Int[] directions =
        {
            Vector2Int.up,
            new Vector2Int(xValue,yValue), //Upper-Right
            Vector2Int.right,
            new Vector2Int(xValue,-yValue), //Lower-Right
            Vector2Int.down,
            new Vector2Int(-xValue,-yValue), //Lower-Left
            Vector2Int.left,
            new Vector2Int(-xValue,yValue) //Upper-Left

        };

        foreach (KeyValuePair<Vector2Int, GridNode> pair in currentGrid) //Pulls the pair from the grid
        {
            foreach (Vector2Int direction in directions) //pulls the directions we want from the grid
            {
                Vector2Int neighborPosition = pair.Value.coords + direction; //Stores the offset position of the current Node

                if (currentGrid.TryGetValue(neighborPosition, out GridNode neighbor) && Mathf.Abs(pair.Value.cellWorldPosition.y - neighbor.cellWorldPosition.y) <= maxStepHeight) //Checks if the offset is in the grid
                {
                    pair.Value.AddConnections(neighbor); //establish the connection to the node if it receives a neighbor node
                }
            }
        }   
    }
    public bool CreateGrid_FromLayout(GeneratedLevelLayout layout)
    {
        grid.Clear();
        gridFloors.Clear();
        tileLookup.Clear();

        foreach (ChunkInstance chunk in layout.chunks) 
        {
            if (chunk == null || chunk.floors == null) 
            {
                Debug.Log("Invalid Chunk/ChunkFloor");
                return false;
            }

            foreach(ChunkFloorInstance floor in chunk.floors) 
            {
                if (floor == null || floor.cells == null || floor.globalFloorID < FirstGlobalFloorID) 
                {
                    Debug.Log("Chunk Floor Info: Invalid");
                    return false;
                }
               
                if (!gridFloors.TryGetValue(floor.globalFloorID, out Dictionary<Vector2Int, GridNode> grid)) 
                {
                    grid = new Dictionary<Vector2Int, GridNode>();

                    gridFloors.Add(floor.globalFloorID, grid);

                }

                foreach (GridCellDescriptor cell in floor.cells) 
                {
                    if (cell == null || cell.tile == null) 
                    {
                        Debug.Log("Grid Cell(tile) or TileController is bad");
                        return false;
                    }

                    if (cell.globalFloorID != floor.globalFloorID) 
                    {
                        Debug.Log("Grid Cell global floorID does not match parent");
                        return false;
                    }
                    //Prevents a duplicate
                    if (grid.ContainsKey(cell.globalCoordinates)) 
                    {
                        return false;
                    }

                    //Creating the actual gridNode
                    GridNode currentNode = new GridNode(cell.globalCoordinates, cell.globalFloorID, cell.worldPosition);

                    cell.tile.Initialize(currentNode);
                    RegisterTile(cell.tile);

                    grid.Add(cell.globalCoordinates, currentNode);

                }
            }
        }

        foreach(Dictionary<Vector2Int, GridNode> floorGrid in gridFloors.Values) 
        {
            SetUpConnections(floorGrid);
        }

        return true;
    }
    public void RegisterTile(TileController tile)
    {
        if (tile == null || tile.node == null)
            return;

        tileLookup[tile.node] = tile;
        
    }
    public void ConnectFloors() 
    {
        FloorConnector[] connections = FindObjectsByType<FloorConnector>();

        foreach (FloorConnector connection in connections) 
        {
            connection.Initialize(this);
            
        }
    }
    public void InitializeUnits() 
    {
        UnitController[] units = FindObjectsByType<UnitController>();

        foreach (UnitController unit in units)
        {
            if (!unit.TryInitializeUnitData())
            {
                // Never initialize or register an unbound roster placeholder.
                Debug.LogError($"Unable to initialize unit '{unit.name}'. Disabling scene unit.", unit);
                unit.gameObject.SetActive(false);

                continue;

            }

            unit.InitializeNode();

        }
    }
    public void InitializeObstacles() 
    {
        ObstacleController[] obstacles = FindObjectsByType<ObstacleController>();

        foreach (ObstacleController obstacle in obstacles) 
        {
            obstacle.InitializeNodes();
        }
    }

    //Getters- NodeFromGridPosition, NodeFrom-WorldPosition, Grid itself
    public GridNode GetNodeFromGridPosition(int floorID, Vector2Int gridPosition) 
    {
        GridNode current = null;

        if (gridFloors.TryGetValue(floorID, out Dictionary<Vector2Int, GridNode> thisGrid)) 
        {
            if (thisGrid.TryGetValue(gridPosition, out GridNode thisNode)) 
            {
                current = thisNode; 
            }
        }

        if (current != null) 
        {
           return current;
        }

        return null;
    }
    public GridNode GetNodeFromClosestWorldPosition(Vector3 worldPosition) //Receives the object worldPosition 
    {

        GridNode closestNode = null; 
        float closestDistance = float.MaxValue;

        foreach (Dictionary<Vector2Int, GridNode> floor in gridFloors.Values) //Loops over the gridFloors
        {
            foreach (GridNode node in floor.Values) //pullse Nodes from the grid 
            {
                float distance = Vector3.Distance(worldPosition, node.cellWorldPosition); //Checks the distance of the received worldpos

                if (distance < closestDistance) 
                {
                    closestDistance = distance;
                    closestNode = node;
                }
            }
        }

        return closestNode;
    }
    public Dictionary<Vector2Int, GridNode> GetGrid(int ID) 
    {
        Dictionary<Vector2Int, GridNode> thisGrid = null;
        if (gridFloors.TryGetValue(ID, out Dictionary<Vector2Int, GridNode> currentGrid)) 
        {
            thisGrid = currentGrid;
        }

        return thisGrid; 
    }
    public IEnumerable<int> GetFloorIDS() 
    {
        return gridFloors.Keys;
    }
    public TileController FindTile(GridNode node)
    {
        if (node == null)
            return null;

        tileLookup.TryGetValue(node, out TileController tile);
        return tile;
    }
    public IEnumerable<TileController> GetTiles() 
    {
        return tileLookup.Values;
    }

    public void SetActiveFloor(int floorID)
    {
        if (gridFloors.ContainsKey(floorID))
        {
            activeFloorID = floorID;
        }
    }
    private bool ResolveGlobalFloorMappings(GeneratedLevelLayout layout)
    {
        if (layout == null || layout.chunks == null)
        {
            Debug.Log("Cannot resolve floor mapping");
            return false;
        }

        List<float> floorHeights = new List<float>();

        //Pass 1
        foreach (ChunkInstance chunk in layout.chunks)
        {
            if (chunk == null || chunk.floors == null)
            {
                return false;
            }

            if (chunk.globalFloorMappings == null)
            {
                chunk.globalFloorMappings = new Dictionary<int, int>();
            }

            chunk.globalFloorMappings.Clear();

            foreach (ChunkFloorInstance floor in chunk.floors)
            {
                if (floor == null || floor.floorTransform == null)
                {
                    return false;
                }

                floorHeights.Add(floor.worldPosition.y);
            }
        }

        //Sort Elevations from lowerst to highest

        floorHeights.Sort();

        //Remove elevations that represent the represent the same global floor
        List<float> globalFloorHeights = new List<float>();

        foreach (float height in floorHeights)
        {
            if (globalFloorHeights.Count == 0 ||
                (Mathf.Abs(height - globalFloorHeights[globalFloorHeights.Count - 1]) > floorHeightTolerance))
            {
                globalFloorHeights.Add(height);
            }
        }

        //Second Pass: GlobalFloorIDS and Populating the chunks in dictionary
        foreach (ChunkInstance chunk in layout.chunks)
        {
            foreach (ChunkFloorInstance floor in chunk.floors)
            {
                int matchingIndex = -1;
                float closestDifference = float.MaxValue;

                for (int i = 0; i < globalFloorHeights.Count; i++)
                {
                    float difference = Mathf.Abs(floor.worldPosition.y - globalFloorHeights[i]);

                    if (difference <= floorHeightTolerance && difference < closestDifference)
                    {
                        closestDifference = difference;
                        matchingIndex = i;
                    }
                }

                if (matchingIndex == -1)
                {
                    Debug.Log($"Couldn't resolve change from local floor {floor.localFloorID} to global floor.");
                    return false;
                }

                int globalFloorID = FirstGlobalFloorID + matchingIndex;

                if (chunk.globalFloorMappings.ContainsKey(floor.localFloorID))
                {
                    Debug.Log($"Dublicate local floor ID: {floor.localFloorID}");
                    return false;
                }

                chunk.globalFloorMappings.Add(floor.localFloorID, globalFloorID);

                floor.globalFloorID = globalFloorID;

                foreach (GridCellDescriptor cell in floor.cells)
                {
                    cell.globalFloorID = globalFloorID;
                }
            }
        }

        Debug.Log($"Resolved {globalFloorHeights.Count} global floors");

        return true;
    }


    public void ChangeActiveFloor(int direction)
    {
        if (direction == 0)
        {
            return;
        }

        int nextFloor = activeFloorID;

        foreach (int floorID in gridFloors.Keys)
        {
            if (direction > 0 && floorID > activeFloorID && (nextFloor == activeFloorID || floorID < nextFloor))
            {
                nextFloor = floorID;
            }
            else if (direction < 0 && floorID < activeFloorID && (nextFloor == activeFloorID || floorID > nextFloor))
            {
                nextFloor = floorID;
            }
        }

        if (nextFloor != activeFloorID)
        {
            SetActiveFloor(nextFloor);
        }
    }
}
