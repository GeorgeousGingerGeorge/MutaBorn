using UnityEngine;

[RequireComponent(typeof(WorldSwitchController))]
public class WorldSwitchInteractable : WorldInteractable
{
    [SerializeField] private WorldSwitchController mechanism;
    [SerializeField] private string activateLabel = "Pull Lever";
    [SerializeField] private string deactivateLabel = "Reset Lever";

    public override string InteractionMenuLabel => mechanism == null
        ? "Activate"
        : mechanism.IsOn ? deactivateLabel : activateLabel;

    private void Awake()
    {
        if (mechanism == null)
        {
            mechanism = GetComponent<WorldSwitchController>();

        }

    }

    public override bool CanInteract(UnitController actor)
    {

        return base.CanInteract(actor) && mechanism != null &&
            mechanism.isActiveAndEnabled && mechanism.CanToggle;

    }

    protected override void OnInteractionSucceeded(UnitController actor)
    {
        mechanism.TryToggle();

    }

}
