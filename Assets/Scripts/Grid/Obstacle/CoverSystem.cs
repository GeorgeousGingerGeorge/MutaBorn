
using UnityEngine;

public struct CoverResult 
{
    public CoverState coverState;
    public float verticalCoverage;
    public ObstacleController protectingObstacle;

    public CoverResult(CoverState state, float coverage, ObstacleController obstacle) 
    {
        coverState = state;
        verticalCoverage = coverage;
        protectingObstacle = obstacle; 
    }
}
public class CoverSystem: MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Occupancy occupancy;
    [SerializeField] private GridManager gridManager;

    [Header("Cover Debug")]
    [SerializeField] private bool showCoverDebug = true;
    [SerializeField] private bool showRejectedObstacles = true;
    private enum ObstacleHitSide
    {
        None,
        Front,
        Back,
        Right,
        Left,
        Top,
        Bottom
    }

    public CoverResult CalculateCover(UnitController attacker, UnitController defender) 
    {
        CoverResult bestResult = new CoverResult(CoverState.NONE, 0f, null);

        if (attacker == null || defender == null || attacker == defender || occupancy == null) 
        {
            return bestResult;
        }

        GridNode attackerNode = attacker.currentNode;
        GridNode defenderNode = defender.currentNode;

        if (attackerNode == null || defenderNode == null || attackerNode.floorID != defenderNode.floorID) 
        {
            return bestResult;
        }

        Vector2Int attackOffset = attackerNode.coords - defenderNode.coords;

        float unitHeight = GetUnitHeight(defender);

        if(unitHeight <= 0f) 
        {
            return bestResult;
        }

        foreach (GridNode neighbor in defenderNode.GetNeighbors()) 
        {
            if (neighbor == null || neighbor.floorID != defenderNode.floorID) 
            {
                continue;
            }

            Vector2Int obstacleOffset = neighbor.coords - defenderNode.coords;

            if(Mathf.Abs(obstacleOffset.x) > 1 || Mathf.Abs(obstacleOffset.y) > 1 || obstacleOffset == Vector2Int.zero) 
            {
                continue;
            }

            if (!IsInAttackDirection(obstacleOffset, attackOffset)) 
            {
                continue;
            }

            bool isDiagonal = obstacleOffset.x != 0 && obstacleOffset.y != 0;

            ObstacleController solidObstacle = occupancy.FindSolidObstacle(neighbor);

            EvaluateObstacle(solidObstacle, obstacleOffset, isDiagonal, unitHeight, ref bestResult, "Solid");

            ObstacleController edgeObstacle = occupancy.FindEdgeObstacle(defenderNode, neighbor);

            EvaluateObstacle(edgeObstacle, obstacleOffset, isDiagonal, unitHeight, ref bestResult, "Edge");
        }

        if (bestResult.protectingObstacle != null)
        {
            LogCover(
                $"FINAL RESULT: {bestResult.protectingObstacle.obstacleName} | " +
                $"Cover: {bestResult.coverState} | " +
                $"Vertical Coverage: {bestResult.verticalCoverage:P1}");
        }
        else 
        {
            LogCover($"FINAL RESULT: No Cover Found");
        }

        return bestResult;
    }

    private void EvaluateObstacle(ObstacleController obstacle, Vector2Int obstacleOffset,  bool isDiagonal,float unitHeight, ref CoverResult bestResult, string source) 
    {

        if (obstacle == null) 
        {
            if (showRejectedObstacles) 
            {
                LogCover($"{source}: No Obstacle Found");
               
            }
            return;
        }

        if (obstacle.stateOfCover == CoverState.NONE) 
        {
            if (showRejectedObstacles) 
            {
                LogCover($"{source}: {obstacle.obstacleName} provides NONE cover");
            }
            return;
        }
    
        CoverState effectiveCover = GetEffectiveCoverState(obstacle.stateOfCover, isDiagonal);
        
        if (effectiveCover == CoverState.NONE)
        {
            if (showRejectedObstacles) 
            {
                LogCover($"{source}: {obstacle.obstacleName} reduced to NONE Cover");
            }
            return;
        }

        float verticalCoverage = CalculateVerticalCoverage(obstacle.coverHeight, unitHeight);

        if (verticalCoverage <= 0f) 
        {
            if (showRejectedObstacles) 
            {
                LogCover($"{source}: {obstacle.obstacleName} has no vertical coverage");
            }
            return;
        }

        LogCover(
            $"{source}: {obstacle.obstacleName} qualifies" +
            $"Postion: {obstacleOffset}, " +
            $"Type: {(isDiagonal ? "Diagonal" : "Cardinal")}" +
            $"Configured Cover: {obstacle.stateOfCover}," +
            $"Effective cover: {effectiveCover}" +
            $"Vertical Coverage: {verticalCoverage:P1}");


        bool isBetter = (int)effectiveCover > (int)bestResult.coverState || (effectiveCover == bestResult.coverState && verticalCoverage > bestResult.verticalCoverage);

        if (!isBetter) 
        {
            LogCover($"{obstacle.obstacleName} did not improve the current cover result");
            return;
        }

        bestResult = new CoverResult(effectiveCover, verticalCoverage, obstacle);

        LogCover($"New Best Cover: {obstacle.obstacleName} | " + $"State: {effectiveCover} |" + $"Coverage: {verticalCoverage: P1}");
    }

    private CoverState GetEffectiveCoverState(CoverState obstacleCover, bool isDiagonal) 
    {
        if (!isDiagonal) 
        {
            return obstacleCover;
        }

        switch (obstacleCover) 
        {
            case CoverState.HIGH:
                return CoverState.LOW;
            case CoverState.LOW:
                return CoverState.NONE;
            default:
                return CoverState.NONE;
        }
    }
    private float GetUnitHeight(UnitController unit)
    {
        if (unit == null) 
        {
            return 0f;
        }

        return unit.currentStance == UnitStance.Crouching ? unit.data.crouchingHeight : unit.data.standingHeight;
    }
    private float CalculateVerticalCoverage(float obstacleHeight,  float unitHeight)
    {
       if(unitHeight <= 0f || obstacleHeight <= 0f) 
       {
            return 0f;     
       }

        return Mathf.Clamp01(obstacleHeight / unitHeight);
    }
    private bool IsInAttackDirection(Vector2Int obstacleOffset, Vector2Int attackOffset)
    {
        if (obstacleOffset == Vector2Int.zero ||
            attackOffset == Vector2Int.zero)
        {
            return false;
        }

        Vector2 obstacleDirection =
            new Vector2(obstacleOffset.x, obstacleOffset.y).normalized;

        Vector2 attackerDirection =
            new Vector2(attackOffset.x, attackOffset.y).normalized;

        float dot = Vector2.Dot(obstacleDirection, attackerDirection);

        return dot >= 0.5f;
    }
    private void LogCover(string message) 
    {
        if (!showCoverDebug) 
        {
            return;
        }

        Debug.Log($"[CoverSystem] {message}", this);
    }
    private ObstacleHitSide GetObstacleHitSide(BoxCollider obstacleCollider,Vector3 attackerPosition,Vector3 defenderPosition)
    {
        Transform obstacleTransform = obstacleCollider.transform;

        Vector3 obstacleCenter = obstacleTransform.TransformPoint(obstacleCollider.center);

        // Direction from attacker toward defender.
        Vector3 attackDirection = (defenderPosition - attackerPosition).normalized;

        // Direction from obstacle toward attacker.
        Vector3 directionToAttacker = (attackerPosition - obstacleCenter).normalized;

        // Convert both directions into obstacle-local space.
        Vector3 localAttackDirection = obstacleTransform.InverseTransformDirection(attackDirection);

        Vector3 localAttackerDirection = obstacleTransform.InverseTransformDirection(directionToAttacker);

        float x = Mathf.Abs(localAttackDirection.x);
        float z = Mathf.Abs(localAttackDirection.z);

        // Determine which horizontal face the attack approaches.
        if (x > z)
        {
            return localAttackerDirection.x >= 0 ? ObstacleHitSide.Right : ObstacleHitSide.Left;
        }

        return localAttackerDirection.z >= 0 ? ObstacleHitSide.Front : ObstacleHitSide.Back;
    }

    private bool IsObstacleBetween(BoxCollider obstacleCollider, Vector3 attackerPosition, Vector3 defenderPosition) 
    {
        Vector3 attackDirection = defenderPosition - attackerPosition;

        float distance = attackDirection.magnitude;

        if (distance <= 0f)
        {
            return false;
        }
        attackDirection.Normalize();

        // Horizontal direction perpendicular to the attack.
        Vector3 perpendicular = Vector3.Cross(Vector3.up,attackDirection).normalized;

        // Test several points across the defender's body width.
        float sampleWidth = 0.4f;

        Vector3[] targetPoints ={defenderPosition, defenderPosition + perpendicular * sampleWidth, defenderPosition - perpendicular * sampleWidth};

        foreach (Vector3 point in targetPoints)
        {
            Vector3 direction = point - attackerPosition;
            float rayDistance = direction.magnitude;

            if (obstacleCollider.Raycast(new Ray(attackerPosition, direction.normalized), out RaycastHit hit, rayDistance))
            {
                return true;
            }
        }

        return false;
    }
    private BoxCollider GetObstacleCollider(ObstacleController obstacle)
    {
        if (obstacle == null)
            return null;

        return obstacle.GetComponent<BoxCollider>();
    }
}
