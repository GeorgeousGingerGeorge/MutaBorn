using System;
using System.Collections.Generic;
using UnityEngine;

// Mutations implement these callbacks only when they need combat reactions.
public interface IMutationCombatReaction
{
    void OnDamageDealt(UnitController owner, CombatDamageEvent damageEvent);
    void OnDamageReceived(UnitController owner, CombatDamageEvent damageEvent);

}

[DisallowMultipleComponent]
public sealed class MutationCombatHandler : MonoBehaviour
{
    private static MutationCombatHandler activeHandler;

    [Header("Testing")]
    [SerializeField] private bool logDamageEvents;

    private void OnEnable()
    {
        if (activeHandler != null && activeHandler != this)
        {
            Debug.LogError("Only one MutationCombatHandler should be active in the scene.", this);

            enabled = false;

            return;

        }

        activeHandler = this;
        CombatActionExecutor.DamageApplied += HandleDamageApplied;

    }

    private void OnDisable()
    {
        CombatActionExecutor.DamageApplied -= HandleDamageApplied;

        if (activeHandler == this)
        {
            activeHandler = null;

        }

    }

    private void HandleDamageApplied(CombatDamageEvent damageEvent)
    {
        if (damageEvent.ActualDamage <= 0)
        {
            return;

        }

        if (logDamageEvents)
        {
            Debug.Log(
                $"Mutation combat event: {damageEvent.Attacker?.name} dealt " +
                $"{damageEvent.ActualDamage} damage to {damageEvent.Target?.name}.",
                this);

        }

        NotifyMutations(damageEvent.Attacker, damageEvent, true);
        NotifyMutations(damageEvent.Target, damageEvent, false);

    }

    private void NotifyMutations(UnitController owner, CombatDamageEvent damageEvent, bool damageDealt)
    {
        if (owner == null || owner.data == null || owner.data.activeMutations == null)
        {
            return;

        }

        // An instance referenced by multiple slots should react only once per event.
        HashSet<BaseMutation> visited = new();

        foreach (BaseMutation mutation in owner.data.activeMutations.Values)
        {
            if (mutation == null || !visited.Add(mutation))
            {
                continue;

            }

            if (!(mutation is IMutationCombatReaction reaction))
            {
                continue;

            }

            try
            {
                if (damageDealt)
                {
                    reaction.OnDamageDealt(owner, damageEvent);

                }
                else
                {
                    reaction.OnDamageReceived(owner, damageEvent);

                }

            }
            catch (Exception exception)
            {
                Debug.LogException(exception, owner);

            }

        }

    }

}
