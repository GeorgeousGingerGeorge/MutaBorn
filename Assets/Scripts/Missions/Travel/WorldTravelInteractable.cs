using System.Collections.Generic;
using UnityEngine;

public enum WorldTravelKind
{
    Ladder,
    Portal

}

public sealed class WorldTravelInteractable : WorldInteractable
{
    [Header("Destination, assigned from the mission hierarchy")]
    [SerializeField] private WorldTravelKind travelKind;
    [SerializeField] private Transform exitPoint;
    [SerializeField, Min(1)] private int entranceFloorId = 1;
    [SerializeField, Min(1)] private int exitFloorId = 2;
    [SerializeField] private bool allowNeighborLanding = true;

    private void Start()
    {
        if (travelKind != WorldTravelKind.Ladder || exitPoint == null)
        {

            return;

        }

        GridManager grid = FindAnyObjectByType<GridManager>();
        if (grid == null)
        {

            return;

        }

        GridNode entrance = FindNearestOnFloor(grid, entranceFloorId, transform.position);
        GridNode exit = FindNearestOnFloor(grid, exitFloorId, exitPoint.position);
        if (entrance == null || exit == null || entrance == exit)
        {

            return;

        }

        if (!entrance.GetNeighbors().Contains(exit))
        {
            entrance.AddConnections(exit);

        }

        if (!exit.GetNeighbors().Contains(entrance))
        {
            exit.AddConnections(entrance);

        }

    }

    public override string InteractionMenuLabel => travelKind == WorldTravelKind.Ladder ? "Climb Ladder" : "Enter Portal";

    public override bool CanInteract(UnitController actor)
    {
        if (!base.CanInteract(actor) || actor == null || actor.currentNode == null)
        {

            return false;

        }

        if (actor.currentNode.floorID != entranceFloorId)
        {

            return false;

        }

        return TryFindLanding(actor, out _);

    }

    protected override void OnInteractionSucceeded(UnitController actor)
    {
        if (!TryFindLanding(actor, out GridNode landing))
        {

            return;

        }

        Vector3 offset = actor.transform.position - actor.currentNode.cellWorldPosition;
        actor.transform.position = landing.cellWorldPosition + offset;
        actor.SetNode(landing);

    }

    private static GridNode FindNearestOnFloor(GridManager grid, int floorId, Vector3 position)
    {
        Dictionary<Vector2Int, GridNode> floor = grid.GetGrid(floorId);
        if (floor == null)
        {

            return null;

        }

        GridNode closest = null;
        float nearest = float.PositiveInfinity;
        foreach (GridNode node in floor.Values)
        {
            Vector2 delta = new Vector2(node.cellWorldPosition.x - position.x,
                node.cellWorldPosition.z - position.z);
            if (delta.sqrMagnitude < nearest)
            {
                nearest = delta.sqrMagnitude;
                closest = node;

            }

        }

        return closest;

    }

    private bool TryFindLanding(UnitController actor, out GridNode landing)
    {
        landing = null;
        GridManager grid = FindAnyObjectByType<GridManager>();
        Occupancy occupancy = actor.occupancy;
        if (grid == null || occupancy == null || exitPoint == null ||
            (travelKind == WorldTravelKind.Ladder && actor.currentNode.floorID == exitFloorId))
        {

            return false;

        }

        GridNode exit = FindNearestOnFloor(grid, exitFloorId, exitPoint.position);
        if (exit == null)
        {

            return false;

        }

        if (occupancy.IsFootPrintMovable(exit, actor))
        {
            landing = exit;

            return true;

        }

        if (travelKind == WorldTravelKind.Ladder || !allowNeighborLanding ||
            occupancy.IsBlockedBySolidObstacle(exit))
        {

            return false;

        }

        foreach (GridNode candidate in exit.GetNeighbors())
        {
            if (candidate.floorID != exit.floorID ||
                occupancy.IsBlockedByEdgeObstacle(exit, candidate) ||
                occupancy.IsDiagonalBlockedByEdgeObstacle(exit, candidate) ||
                !occupancy.IsFootPrintMovable(candidate, actor))
            {

                continue;

            }

            landing = candidate;

            return true;

        }

        return false;

    }

}
