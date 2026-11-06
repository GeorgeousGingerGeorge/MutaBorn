using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public enum FogState 
{ 
    CLEAR,
    DARK
}
public class FogOfWarManager : MonoBehaviour
{
    [Header("LineOfSight System- Sight Calculations")]
    [SerializeField] private LineOfSight lineOfSight;
    private Occupancy occupancy;

    [Header("GridManager")]
    [SerializeField] private GridManager gridManager;

    [Header("Current player units nodes they've visited and stored")]
    private HashSet<GridNode> visitedNodes = new HashSet<GridNode>();
    private HashSet<GridNode> exploredNodes = new HashSet<GridNode>();

  
    
 



    private void Start() 
    {
        occupancy = FindAnyObjectByType<Occupancy>();
        InitializeFogOfWar();
        UpdateFriendlyUnitVisibility();

     
    }
    public void UpdateFriendlyUnitVisibility() 
    {

        HashSet<UnitController> friendlyUnits = new HashSet<UnitController>();
        HashSet<UnitController> otherUnits = new HashSet<UnitController>();
        

        foreach (UnitController unit in occupancy.GetUnits()) 
        {
            UnitFaction faction = unit.GetComponent<UnitFaction>();

            if (faction != null && faction.GetFactionID() == "playerFaction")
            {
                friendlyUnits.Add(unit);
              
            }
            else 
            {
                otherUnits.Add(unit);
            }
            
        }

       

        UpdateVisibility(friendlyUnits, otherUnits);
    }
    private void UpdateVisibility(IEnumerable<UnitController> friendlyUnits = null, IEnumerable<UnitController> otherUnits = null) 
    {
        visitedNodes.Clear();

        foreach (UnitController unit in friendlyUnits) 
        {
            HashSet<GridNode> visibleNodes = lineOfSight.GetVisibleNodes(unit);

            foreach (GridNode node in visibleNodes) 
            {
                visitedNodes.Add(node);
                exploredNodes.Add(node);
            }
        }
        foreach (UnitController unit in otherUnits) 
        {
            unit.SetFogState(GetFogState(unit.currentNode));
        }


        //TileChanges
        foreach (ObstacleController obstacle in occupancy.GetSolidObstacles()) 
        {
            FloorConnector connector = null;
            if (obstacle.GetComponentInParent<FloorConnector>())
            {
                connector = obstacle.GetComponentInParent<FloorConnector>();
                foreach (GridNode node in visitedNodes)
                {
                    List<GridNode> neighbors = node.GetNeighbors();

                    foreach (GridNode neighbor in neighbors)
                    {

                        foreach (GridNode connectorNode in connector.nodes)
                        {
                            if (connectorNode == neighbor)
                            {
                                TileController tile = gridManager.FindTile(connectorNode);
                                tile.SetFogState(GetFogState(node));
                            }
                        }

                    }

                }
            }
            else 
            {

                TileController tile = gridManager.FindTile(obstacle.firstnode);

                foreach (GridNode node in visitedNodes) 
                {
                    foreach(GridNode neighbor in node.GetNeighbors())
                    {
                        if(tile.node == neighbor)
                            tile.SetFogState(GetFogState(neighbor));

                    }
                }
               
            }
            
        }

        foreach (GridNode node in visitedNodes) 
        {
            TileController tile = gridManager.FindTile(node);

            tile.SetFogState(GetFogState(node));
        }
        
        

    }
    
    
    private void InitializeFogOfWar() 
    {
        if(gridManager == null) 
        {
            Debug.LogWarning("FogOfWarManager: GridManager reference is missing");
            return;
        }

       
        exploredNodes.Clear();
        visitedNodes.Clear();

        
        Debug.Log("FogOfWar: initialized with all nodes unexplored");
    }


    public FogState GetFogState(GridNode node) 
    {
        if (node == null) 
        {
            return FogState.DARK;
        }

        return exploredNodes.Contains(node) ? FogState.CLEAR : FogState.DARK;
    }
}
