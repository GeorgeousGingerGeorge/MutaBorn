using UnityEngine;

/// <summary>
/// Small temporary interaction target for proving the Interact button works.
/// The object (or one of its children) MUST have a Collider.
/// Replace with your team's final interaction interface/system later.
/// </summary>
public class SimpleInteractable : MonoBehaviour
{
    [SerializeField] private string interactionName = "Test Object";
    [SerializeField] private bool enabledForInteraction = true;

    public virtual bool CanInteract(UnitController unit) =>
        enabledForInteraction && unit != null && isActiveAndEnabled;

    public virtual void Interact(UnitController unit)
    {
        if (!CanInteract(unit)) return;
        Debug.Log($"{unit.name} interacted with {interactionName}.", this);
        // Gameplay effects and AP costs are intentionally NOT implemented yet.
    }
}
