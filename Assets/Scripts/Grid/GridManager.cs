using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.Hierarchy;
public class GridManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private Level_Design currentLevel;
    [Header("Adjust Tile Size")]
    [SerializeField] private float gridCellSize = 2f;

    [Header("CurrentGrid Information")]
    private Dictionary<Vector2Int, GridNode> grid = new Dictionary<Vector2Int, GridNode>();

    [Header("CurrentGrid Information- Grid Hierarchy")]
    private GameObject mainLevel;
    private GameObject floorNumber;

    [Header("CurrentGrid Information- Grids for Each Floor")]
    private Dictionary<int, Dictionary<Vector2Int, GridNode>> gridFloors = new Dictionary<int, Dictionary<Vector2Int, GridNode>>();


    public void Start()
    {
        BuildLevel(); 
    }

    public void BuildLevel()
    {
        //Checks if currentLevel and the levelPrefab exists, if it doesn't it would switch to the other generator
        if (currentLevel != null && currentLevel.levelPrefab != null) 
        {
            mainLevel = new GameObject($"{currentLevel.levelName}_Grid");
            mainLevel.transform.position = Vector3.zero;

            floorNumber = Instantiate(currentLevel.levelPrefab, Vector3.zero, Quaternion.identity, mainLevel.transform);
            floorNumber.transform.SetParent(mainLevel.transform, false);

            Debug.Log(floorNumber.name);
            foreach (PlacedFloor floorData in currentLevel.placedFloors)
            {
                BuildFloor(floorData.floorIndex);
            }
        }
    }

    

    public void BuildFloor()   // Build's procedurally generated grid
    {
        CreateGrid_Level_Generated(); 
    }
    public void BuildFloor(FloorIdentification index)  //Builds a prefab Grid
    {
        CreateGrid_Level_Prefab(index); 
    }

    //used for when we procedurally generate tiles later 
    public void CreateGrid_Level_Generated() 
    {
        
    }
    //used for when we create level using a prefab
    public void CreateGrid_Level_Prefab(FloorIdentification floor) 
    {
        Transform thisFloor = floorNumber.transform.Find($"Floor_{floor.GetFloorID()}");
        Debug.Log(thisFloor.name);

        int x = 0;
        int z = 0;

        foreach (Transform floorSlice in thisFloor) 
        {
            foreach (Transform sliceObject in floorSlice) 
            {
                if (z == floor.GetGridSize().y) 
                {
                    z = 0;
                }

                Vector3 worldPos = sliceObject.position;

                Vector2Int coords = new Vector2Int(x, z);
                GridNode currentNode = new GridNode(coords, floor.GetFloorID(), worldPos);

                //Add Components to the Tile(sliceObject) that we are manipulating in the grid

                Debug.Log($"Current Node Coordinate:{coords} CurrentFloor: {floor.GetFloorID()} ");
                if (sliceObject.GetComponentInParent<TileController>()) 
                {
                    TileController currentTile = sliceObject.GetComponentInParent<TileController>();
                    currentTile.Initialize(currentNode);
                    
                  
                }

                //Adds the node to the grid
                grid.Add(coords, currentNode);
                z++;


            }
           
            x++;
        }

        //We need to set up connections for all the nodes in the grid
        //SetUpConnections(grid); 
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
}
