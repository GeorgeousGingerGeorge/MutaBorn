using UnityEngine;
using UnityEngine.Events;

// Shared Action Menu interaction target for world objects and mission targets.
public class WorldInteractable : SimpleInteractable
{
    [Header("Interaction Identity")]
    [SerializeField] private ActionDefinition interactionAction;
    [SerializeField] private string interactionId = "interact";
    [SerializeField, Min(1)] private int rangeInTiles = 1;
    [SerializeField] private bool singleUse = true;

    [Header("On Successful Interaction")]
    [Tooltip("Invoked after a valid interaction. Connect any number of scene or prefab receivers.")]
    [SerializeField] private UnityEvent onInteracted = new UnityEvent();

    public bool used;
    public ActionDefinition InteractionAction => interactionAction;
    public string InteractionId => interactionAction != null ? interactionAction.Id : interactionId;
    public virtual string InteractionMenuLabel => interactionAction == null
        ? InteractionTargetName
        : $"{interactionAction.DisplayName} {InteractionTargetName}";

    public string InteractionTargetName
    {
        get
        {
            UnitController targetUnit = GetComponentInParent<UnitController>();
            if (targetUnit != null && targetUnit.data != null &&
                !string.IsNullOrWhiteSpace(targetUnit.data.characterName))
            {

                return targetUnit.data.characterName;

            }

            return targetUnit != null ? targetUnit.name : name;

        }

    }

    public override bool CanInteract(UnitController actor)
    {
        if (!base.CanInteract(actor) || used && singleUse ||
            actor.data == null || actor.data.currentHealth <= 0 || !IsInRange(actor))
        {

            return false;

        }

        UnitController targetUnit = GetComponentInParent<UnitController>();
        if (targetUnit == actor ||
            targetUnit != null && (targetUnit.data == null || targetUnit.data.currentHealth <= 0))
        {

            return false;

        }

        return true;

    }

    public override void Interact(UnitController actor)
    {
        TryInteract(actor);

    }

    public virtual bool TryInteract(UnitController actor)
    {
        if (!CanInteract(actor))
        {

            return false;

        }

        if (singleUse)
        {
            used = true;

        }

        OnInteractionSucceeded(actor);
        onInteracted?.Invoke();
        Debug.Log($"{actor.name} used {InteractionId} on {InteractionTargetName}.", this);
        OnInteractionFinished(actor);

        return true;

    }

    protected virtual void OnInteractionSucceeded(UnitController actor)
    {

    }
    protected virtual void OnInteractionFinished(UnitController actor)
    {

    }

    protected virtual bool IsInRange(UnitController actor)
    {
        if (actor.currentNode == null)
        {

            return false;

        }

        UnitController targetUnit = GetComponentInParent<UnitController>();
        GridNode targetNode = targetUnit != null ? targetUnit.currentNode : null;

        if (targetNode == null)
        {
            GridManager grid = FindAnyObjectByType<GridManager>();
            if (grid == null)
            {

                return false;

            }

            targetNode = grid.GetNodeFromClosestWorldPosition(transform.position);

        }

        if (targetNode == null || actor.currentNode.floorID != targetNode.floorID)
        {

            return false;

        }

        Vector2Int delta = actor.currentNode.coords - targetNode.coords;
        int distance = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));

        return distance <= rangeInTiles && (distance > 0 || targetUnit == null);

    }

}
