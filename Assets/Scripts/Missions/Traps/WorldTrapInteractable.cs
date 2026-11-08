using UnityEngine;

[RequireComponent(typeof(WorldTrap))]
public class WorldTrapInteractable : WorldInteractable
{
    [SerializeField] private WorldTrap trap;

    public override string InteractionMenuLabel => "Disarm Trap";

    private void Awake()
    {
        if (trap == null)
        {
            trap = GetComponent<WorldTrap>();

        }

    }

    public override bool CanInteract(UnitController actor)
    {

        return base.CanInteract(actor) && trap != null && trap.IsRevealed && trap.IsArmed;

    }

    protected override void OnInteractionSucceeded(UnitController actor)
    {
        trap.Disarm();

    }

}
