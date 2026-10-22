using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct ConnectorsBetweenLevels 
{
    [Header("FloorConnector Object")]
    [SerializeField]public GameObject connector;
    [SerializeField] public FloorConnector stairObject;
    [Header("CurrentWorldOrientation")]
    [SerializeField] public Vector3 worldPosition;
    [SerializeField] public Quaternion worldRotation;
    [Header("LowerFloorNodes")]
    [SerializeField] public int lowerFloorId;
    [SerializeField] public Vector2Int lowerGridPosition;
    [Header("UpperFloorNodes")]
    [SerializeField] public int upperFloorId;
    [SerializeField] public Vector2Int upperGridPosition;

    
    
}
public class GridManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private Level_Design currentLevel;
    [Header("Adjust Tile Size")]
    [SerializeField] private float gridCellSize = 2f;

    [Header("CurrentGrid Information")]
    private Dictionary<Vector2Int, GridNode> grid = new Dictionary<Vector2Int, GridNode>();

    [Header("CurrentGrid Information- Generating a grid (No LevelPrefab)")]

    [SerializeField]public GameObject gridCellPrefab;
    [SerializeField] public GameObject obstaclePrefab; 
    public List<FloorIdentification> floorGenerator = new List<FloorIdentification>();
    [SerializeField]public Dictionary<int,List<ConnectorsBetweenLevels>> stairGenerator = new Dictionary<int,List<ConnectorsBetweenLevels>>();

    [Header("CurrentGrid Information- Grid Hierarchy")]
    private GameObject mainGrid;
    private GameObject level;
    private GameObject floorNumber;

    [Header("CurrentGrid Information- Grids for Each Floor")]
    private Dictionary<int, Dictionary<Vector2Int, GridNode>> gridFloors = new Dictionary<int, Dictionary<Vector2Int, GridNode>>();

    [Header("CurrentGrid Information- Tile Lookup by Node")]
    private Dictionary<GridNode, TileController> tileLookup = new Dictionary<GridNode, TileController>();


    public void Start()
    {
        BuildLevel();
        Vector3 positionGrid = new Vector3(8f, 1f, 6f);
        if(currentLevel == null)
        {  
            GameObject current = Instantiate(obstaclePrefab, positionGrid, Quaternion.identity, level.transform);
            current.GetComponent<ObstacleController>().InitializeNodes();
        }
    }

    public void BuildLevel()
    {
        //Checks if currentLevel and the levelPrefab exists, if it doesn't it would switch to the other generator
        if (currentLevel != null && currentLevel.levelPrefab != null)
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
    }

    

    public void BuildFloor_Generated(FloorIdentification data)   // Build's procedurally generated grid
    {
        CreateGrid_Level_Generated(data); 
    }
    public void BuildFloor_Prefab(FloorIdentification data)  //Builds a prefab Grid
    {
        CreateGrid_Level_Prefab(data); 
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
                int lowerFloor = stairPerFloor[thisStair].lowerFloorId;
                Vector2Int lowerGrid = stairPerFloor[thisStair].lowerGridPosition;
                int upperFloor = stairPerFloor[thisStair].upperFloorId;
                Vector2Int upperGrid = stairPerFloor[thisStair].upperGridPosition;

                stairPerFloor[thisStair].stairObject.SetWorldPosition(worldPos);
                stairPerFloor[thisStair].stairObject.SetLowerFloor(lowerFloor);
                stairPerFloor[thisStair].stairObject.SetLowerGridNode(lowerGrid);
                stairPerFloor[thisStair].stairObject.SetUpperFloor(upperFloor);
                stairPerFloor[thisStair].stairObject.SetUpperGridNode(upperGrid);

                GameObject stair = Instantiate(stairPerFloor[thisStair].connector, worldPos, worldRotation, stairNumber.transform);
                stair.transform.SetParent(floorNumber.transform, false);
                stair.GetComponent<ObstacleController>().InitializeNodes();

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
      


        foreach (Transform floorSlice in thisFloor) 
        {
            foreach (Transform sliceObject in floorSlice) 
            {
               
                Vector3 worldPos = sliceObject.position;


                int gridX = Mathf.FloorToInt(worldPos.x / gridCellSize);
                int gridZ = Mathf.FloorToInt(worldPos.z / gridCellSize);

                Vector2Int coords = new Vector2Int(gridX, gridZ);
                GridNode currentNode = new GridNode(coords, floor.GetFloorID(), worldPos);
                currentNode.SetTerrain(TerrainType.OpenGround);
                //Add Components to the Tile(sliceObject) that we are manipulating in the grid


                if (sliceObject.GetComponentInParent<TileController>()) 
                {
                    TileController currentTile = sliceObject.GetComponentInParent<TileController>();
                    currentTile.Initialize(currentNode);
                    RegisterTile(currentTile);
                  
                }

                //Adds the node to the grid
                grid.Add(coords, currentNode);
                


            }
           
           
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

                if (currentGrid.TryGetValue(neighborPosition, out GridNode neighbor)) //Checks if the offset is in the grid
                {
                    pair.Value.AddConnections(neighbor); //establish the connection to the node if it receives a neighbor node
                }
            }
        }   
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
            connection.Connect(this);
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
    public TileController FindTile(GridNode node)
    {
        if (node == null)
            return null;

        tileLookup.TryGetValue(node, out TileController tile);
        return tile;
    }


}
