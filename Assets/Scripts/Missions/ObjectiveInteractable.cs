using System;
using UnityEngine;

// Generic interaction target: place on a civilian, terminal, door, or any other object.
// The existing ActionMenuUI discovers it through SimpleInteractable automatically.
public enum InteractionCompletionOutcome
{
    DestroyTarget,
    KeepTarget,
    DisableTarget

}

public sealed class ObjectiveInteractable : SimpleInteractable
{
    public static event Action<UnitController, ObjectiveInteractable> InteractionCompleted;

    [Header("Interaction Identity")]
    [Tooltip("Reusable ActionDefinition shown as a contextual INTERACT button. Set its Category to Interact and its Action ID to help for the rescue objective.")]
    [SerializeField] private ActionDefinition interactionAction;

    [Tooltip("Compatibility with existing mission definitions. If an Action SO is assigned, its Action ID is used instead.")]
    [SerializeField] private string interactionId = "help";
    [SerializeField, Min(1)] private int rangeInTiles = 1;
    [SerializeField] private bool singleUse = true;
    [Tooltip("Require at least one active mission objective that needs this interaction.")]
    [SerializeField] private bool requiresActiveObjective = true;

    [Header("After Successful Interaction")]
    [Tooltip("For rescuing a unit, Destroy Target removes it from the scene and the grid. Join Faction can be added as another outcome later.")]
    [SerializeField] private InteractionCompletionOutcome completionOutcome = InteractionCompletionOutcome.DestroyTarget;

    private bool used;

    public ActionDefinition InteractionAction => interactionAction;
    public string InteractionId => interactionAction != null ? interactionAction.Id : interactionId;
    public bool Used => used;

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents()
    {
        InteractionCompleted = null;

    }

    public override bool CanInteract(UnitController actor)
    {
        if (!base.CanInteract(actor) || used && singleUse ||
            actor.data == null || actor.data.currentHealth <= 0 ||
            !IsInRange(actor))
        {
            return false;

        }

        UnitController targetUnit = GetComponentInParent<UnitController>();

        if (targetUnit == actor ||
            targetUnit != null && (targetUnit.data == null || targetUnit.data.currentHealth <= 0))
        {
            return false;

        }

        if (requiresActiveObjective)
        {
            MissionObjectiveManager manager = MissionObjectiveManager.Instance;
            if (manager == null || !manager.CanReceiveInteraction(actor, this))
            {
                return false;

            }

        }

        return true;

    }

    public override void Interact(UnitController actor)
    {
        TryInteract(actor);

    }

    public bool TryInteract(UnitController actor)
    {
        if (!CanInteract(actor))
        {
            return false;

        }

        if (singleUse)
        {
            used = true;

        }

        // Objective progress is reported before disabling or destroying the target.
        InteractionCompleted?.Invoke(actor, this);

        Debug.Log($"{actor.name} used {InteractionId} on {InteractionTargetName}.", this);

        ApplyCompletionOutcome();

        return true;

    }

    private void ApplyCompletionOutcome()
    {
        if (completionOutcome == InteractionCompletionOutcome.KeepTarget)
        {
            return;

        }

        UnitController targetUnit = GetComponentInParent<UnitController>();
        GameObject targetObject = targetUnit != null ? targetUnit.gameObject : gameObject;

        // The teammate-owned Occupancy class does not unregister destroyed units automatically.
        if (targetUnit != null && targetUnit.occupancy != null && targetUnit.currentNode != null)
        {
            targetUnit.occupancy.UnregisterUnit(targetUnit.currentNode, targetUnit);

        }

        if (completionOutcome == InteractionCompletionOutcome.DisableTarget)
        {
            targetObject.SetActive(false);

            return;

        }

        if (completionOutcome == InteractionCompletionOutcome.DestroyTarget)
        {
            Destroy(targetObject);

        }

    }

    private bool IsInRange(UnitController actor)
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

        return distance > 0 && distance <= rangeInTiles;

    }

}
