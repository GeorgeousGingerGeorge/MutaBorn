using UnityEngine;

[RequireComponent(typeof(GateController))]
public class GateInteractable : WorldInteractable
{
    [SerializeField] private GateController gate;

    public override string InteractionMenuLabel => gate == null
        ? "Gate"
        : gate.IsOpen ? "Close Gate" : "Open Gate";

    private void Awake()
    {
        if (gate == null)
        {
            gate = GetComponent<GateController>();

        }

    }

    public override bool CanInteract(UnitController actor)
    {

        return base.CanInteract(actor) && gate != null && (gate.IsOpen || !gate.IsLocked);

    }

    public override bool TryInteract(UnitController actor)
    {
        if (!CanInteract(actor) || !gate.TryToggle())
        {

            return false;

        }

        return base.TryInteract(actor);

    }

    protected override bool IsInRange(UnitController actor)
    {

        return actor.currentNode != null && gate != null && gate.IsAdjacentTo(actor.currentNode);

    }

}
