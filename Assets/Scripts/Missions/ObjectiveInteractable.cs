using System;
using UnityEngine;

public enum InteractionCompletionOutcome
{
    DestroyTarget,
    KeepTarget,
    DisableTarget

}

// Existing mission/civilian interaction; uses the same interaction flow as world objects.
public sealed class ObjectiveInteractable : WorldInteractable
{
    public static event Action<UnitController, ObjectiveInteractable> InteractionCompleted;

    [Header("Mission Objective Requirement")]
    [Tooltip("Require at least one active mission objective that needs this interaction.")]
    [SerializeField] private bool requiresActiveObjective = true;

    [Header("After Successful Interaction")]
    [Tooltip("Destroy removes a rescued unit from the scene and grid.")]
    [SerializeField] private InteractionCompletionOutcome completionOutcome = InteractionCompletionOutcome.DestroyTarget;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents()
    {
        InteractionCompleted = null;

    }

    public override bool CanInteract(UnitController actor)
    {
        if (!base.CanInteract(actor))
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

    protected override void OnInteractionSucceeded(UnitController actor)
    {
        // Report progress before disabling or destroying the target.
        InteractionCompleted?.Invoke(actor, this);

    }

    protected override void OnInteractionFinished(UnitController actor)
    {
        ApplyCompletionOutcome();

    }

    private void ApplyCompletionOutcome()
    {
        if (completionOutcome == InteractionCompletionOutcome.KeepTarget)
        {
            return;

        }

        UnitController targetUnit = GetComponentInParent<UnitController>();
        GameObject targetObject = targetUnit != null ? targetUnit.gameObject : gameObject;

        // Occupancy does not unregister destroyed units automatically.
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

}
