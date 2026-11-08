using UnityEngine;

[RequireComponent(typeof(WorldTrap))]
public class WorldTrapChestInteractable : WorldInteractable
{
    [SerializeField] private WorldTrap trap;

    public override string InteractionMenuLabel => "Open Chest";

    private void Awake()
    {
        if (trap == null)
        {
            trap = GetComponent<WorldTrap>();

        }

    }

    protected override void OnInteractionSucceeded(UnitController actor)
    {
        trap.Fire();

    }

}
