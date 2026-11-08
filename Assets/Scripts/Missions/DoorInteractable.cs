using UnityEngine;

[RequireComponent(typeof(DoorController))]
public class DoorInteractable : WorldInteractable
{
    [SerializeField] private DoorController door;

    public override string InteractionMenuLabel => door == null
        ? "Door"
        : door.IsOpen ? "Close Door" : "Open Door";

    private void Awake()
    {
        if (door == null)
        {
            door = GetComponent<DoorController>();

        }

    }

    public override bool CanInteract(UnitController actor)
    {

        return base.CanInteract(actor) && door != null && (door.IsOpen || !door.IsLocked);

    }

    public override bool TryInteract(UnitController actor)
    {
        if (!CanInteract(actor) || !door.TryToggle())
        {

            return false;

        }

        return base.TryInteract(actor);

    }

    // An edge door belongs to the boundary between two nodes, not to one node center.
    protected override bool IsInRange(UnitController actor)
    {
        if (actor.currentNode == null || door == null || door.Obstacle == null)
        {

            return false;

        }

        ObstacleController obstacle = door.Obstacle;

        return actor.currentNode == obstacle.firstnode || actor.currentNode == obstacle.secondnode;

    }

}
