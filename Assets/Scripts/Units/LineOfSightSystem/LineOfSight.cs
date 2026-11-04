
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


            return true;
    }
    private int GetManhattanDistance(Vector2Int observer, Vector2Int target)
    {
        return Mathf.Abs(observer.x - target.x) + Mathf.Abs(observer.y - target.y);
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


    //====================================
    // WORLD POSITION
    //====================================


    private Vector3 GetSightLineDirection(Vector3 observerPos, Vector3 targetPos) 
    {
        return (targetPos - observerPos).normalized;
    }

    private float GetSightLineDistance(Vector3 observerPos, Vector3 targetPos) 
    {
        return Vector3.Distance(observerPos, targetPos);
    }

    //====================================
    // Sight Obstruction
    //====================================

    private bool IsPhysicsSightLineBlocked(Vector3 observerPos, Vector3 targetPos, Transform observer = null, Transform target = null) 
    {

        Vector3 direction = GetSightLineDirection(observerPos, targetPos);


        return false;
    }
}
