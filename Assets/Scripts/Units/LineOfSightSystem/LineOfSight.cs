
using System.Collections.Generic;
using UnityEngine;

public class LineOfSight : MonoBehaviour
{

    [Header("System Dependencies")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Occupancy occupancy;

    [Header("Physics")]
    [SerializeField] private LayerMask obstructionMask;
    //public bool canSee
    //List<GridNode> GetTilesWithinVisionRange



    //====================================
    // VISION RANGE
    //====================================

    public bool CanSee(UnitController observer, UnitController target) 
    {
        if (observer == null || target == null || observer == target) 
        {
            return false;
        }

        if (observer.currentNode == null || target.currentNode == null)
        {
            return false;
        }

        if (observer.data == null || target.data == null) 
        {
            return false;
        }

        if (observer.unitDirection == null) 
        {
            return false;
        }
        if (!IsWithinVisionRange(observer, target)) 
        {
            return false;
        }
        if(!IsWithinFieldOfView(observer, target)) 
        {
            return false; 
        }
        if (!HasPhysicalLineOfSight(observer, target))
        {
            return false;
        }


            return true;
    }
    private int GetManhattanDistance(Vector2Int observer, Vector2Int target)
    {
        return Mathf.Abs(observer.x - target.x) + Mathf.Abs(observer.y - target.y);
    }
    private float GetManhattanDistance(Vector3 observer, Vector3 target)
    {
        return Mathf.Abs(observer.x - target.x) + Mathf.Abs(observer.z - target.z) ;
    }
    private bool IsWithinVisionRange(UnitController observer, UnitController target) 
    {


        Vector2Int observerCoords = observer.currentNode.coords;
        Vector2Int targetCoords = target.currentNode.coords;


        return GetManhattanDistance(observerCoords, targetCoords) <= observer.data.currentLineOfSight;

      
    }

    private bool IsWithinFieldOfView(UnitController observer, UnitController target) 
    {
        Vector3 forward = observer.unitDirection.facingVector;
        Vector3 direction = target.visionOrigin.position - observer.visionOrigin.position;


        forward.y = 0f;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) 
        {
            return true;
        }
        if (forward.sqrMagnitude < 0.0001f) 
        {
            return false;
        }

        float angle = Vector3.Angle(forward, direction);

        float halfFieldOfView = observer.data.currentFieldOfView * 0.5f;

        return angle <= halfFieldOfView;
    }
    private bool IsWithinFieldOfView(UnitController observer, Vector3 destination)
    {
        Vector3 forward = observer.unitDirection.facingVector;
        Vector3 direction = destination - observer.visionOrigin.position;


        forward.y = 0f;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return true;
        }
        if (forward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        float angle = Vector3.Angle(forward, direction);

        float halfFieldOfView = observer.data.currentFieldOfView * 0.5f;

        return angle <= halfFieldOfView;
    }


    //====================================
    // Sight Obstruction
    //====================================

    public bool HasPhysicalLineOfSight(UnitController observer, UnitController target) 
    {
        Vector3 origin = observer.visionOrigin.position;
        Vector3 destination = target.visionOrigin.position;

        Vector3 direction = destination - origin;

        float distance = direction.magnitude;

        if (distance <= 0.001f) 
        {
            return true;
        }

        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance, obstructionMask, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits) 
        {

            ObstacleController obstacle = hit.collider.GetComponent<ObstacleController>();

            if (obstacle == null) 
            {
                continue;
            }

            if (obstacle.currentData == null) 
            {
                continue;
            }

            if (obstacle.currentData.blocksLineOfSight) 
            {
                return false;
            }
        }


        return true;
    }
    public bool HasPhysicalLineOfSight(Vector3 origin, Vector3 destination) 
    {
        
        Vector3 direction = destination - origin;

        float distance = direction.magnitude;

        if (distance <= 0.001f) 
        {
            return true;
        }

        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance, obstructionMask, QueryTriggerInteraction.Ignore);
        

        foreach (RaycastHit hit in hits) 
        {

            ObstacleController obstacle = hit.collider.GetComponent<ObstacleController>();
            

            if (obstacle == null) 
            {
                continue;
            }

            if (obstacle.currentData == null) 
            {
                continue;
            }

            if (obstacle.currentData.blocksLineOfSight) 
            {
                return false;
            }
        }


        return true;
    }

    //================================
    // Node Visiblity
    //================================

    private List<Vector3> GetTilePoints(GridNode node) 
    {
        List<Vector3> points = new List<Vector3>();

        if (node == null) 
        {
            return points;
        }

        Vector3 center = node.cellWorldPosition;

        float halfSize = gridManager.GridCellSize * 0.5f;
        float inset = 0.1f;
        float offset = halfSize - inset;


        //Center
        points.Add(center);

        //Cardinal Directions
        points.Add(center + new Vector3(0f, 0f, offset)); //North
        points.Add(center + new Vector3(0f, 0f, -offset)); // South
        points.Add(center + new Vector3(offset, 0f, 0f)); //East
        points.Add(center + new Vector3(-offset, 0f, 0f));//West

        //Diagonal Directions;
        points.Add(center + new Vector3(offset, 0f, offset)); //NorthEast
        points.Add(center + new Vector3(-offset, 0f, offset)); // NorthWest
        points.Add(center + new Vector3(offset, 0f, -offset)); // SouthEast
        points.Add(center + new Vector3(-offset, 0f, -offset)); // SouthWest

        return points;
    }

    public bool IsNodeVisible(UnitController observer, GridNode node) 
    {
        if (observer == null || node == null) 
        {
            return false;
        }

        if (observer.currentNode == null || observer.data == null) 
        {
            return false;
        }

        if (observer.unitDirection == null || observer.visionOrigin == null) 
        {
            return false;
        }

        Vector3 observerPosition = observer.currentNode.cellWorldPosition;
        Vector3 targetPosition = node.cellWorldPosition;

        float horizontalDistance  = GetManhattanDistance(observerPosition, targetPosition);
        float distanceInGridCells = horizontalDistance / gridManager.GridCellSize;

        if (distanceInGridCells >= observer.data.currentLineOfSight) 
        {
            return false;
        }

        Vector3 forward = observer.unitDirection.facingVector;
        Vector3 direction = node.cellWorldPosition - observer.visionOrigin.position;

        forward.y = 0f;
        direction.y = 0f;


        if (direction.sqrMagnitude > 0.0001f) 
        {
            if (forward.sqrMagnitude < 0.0001f) 
            {
                return false;
            }

            float angle = Vector3.Angle(forward, direction);
            float halfFieldOfView = observer.data.currentFieldOfView * 0.5f;

            if (angle > halfFieldOfView) 
            {
                return false;
            }
        }

        List<Vector3> points = GetTilePoints(node);

        foreach (Vector3 point in points)
        {
            if (!IsWithinFieldOfView(observer, point)) 
            {
                continue;
            }
            if (HasPhysicalLineOfSight(observer.visionOrigin.position, point))
            {
                return true;
            }
        }

        return false;
    }
    public HashSet<GridNode> GetVisibleNodes(UnitController observer) 
    {
        HashSet<GridNode> visibleNodes = new HashSet<GridNode>();

        if (observer == null || observer.currentNode == null || observer.data == null) 
        {
            return visibleNodes;
        }

        foreach (int FloorId in gridManager.GetFloorIDS()) 
        {
            Dictionary<Vector2Int, GridNode> floorGrid = gridManager.GetGrid(FloorId);

            if (floorGrid == null) 
            {
                continue;
            }

            foreach (GridNode node in floorGrid.Values) 
            {
                if (IsNodeVisible(observer, node)) 
                {
                    visibleNodes.Add(node);
                }
            }
        }

        return visibleNodes;
    }

    private void DrawTileSamplePoints(GridNode node)
    {
        if (node == null)
            return;

        List<Vector3> samplePoints = GetTilePoints(node);

        foreach (Vector3 point in samplePoints)
        {
            // Draw a small vertical marker at each sample point
            Debug.DrawRay(
                point,
                Vector3.up * 0.2f,
                Color.green,
                5f
            );
        }
    }

    public void DebugDrawTilePoints(GridNode node) 
    {
        DrawTileSamplePoints(node);
    }
}
