using System;
using System.Collections.Generic;
using UnityEngine;



public class ActionResolver : MonoBehaviour
{
    [Header("Action Sets")]

    [Tooltip("Temporary manual sources. Later, race, equipment and mutations will supply these automatically.")]
    [SerializeField] private List<ActionSet> actionSets = new();

    // The final collection of unique actions.
    private readonly List<ActionDefinition> resolvedActions = new();

    // Read-only access for other systems.
    public IReadOnlyList<ActionDefinition> ResolvedActions => resolvedActions;

    // Tracks when the available actions change.
    public int Version { get; private set; }

    private void Awake()
    {
        RefreshActions();

    }

    // Rebuild the unit's available actions.
    public void RefreshActions()
    {
        resolvedActions.Clear();

        // Action IDs must be unique, regardless of which set grants them.
        Dictionary<string, ActionDefinition> registeredActions = new(StringComparer.OrdinalIgnoreCase);

        foreach (ActionSet set in actionSets)
        {
            if (set == null)
            {
                continue;

            }
            foreach (ActionDefinition action in set.Actions)
            {
                if (action == null)
                {
                    continue;

                }

                string id = action.Id?.Trim();

                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning(
                        $"Action '{action.name}' has an invalid ID.",
                        this
                    );

                    continue;

                }

                // Check if another source already granted this action.
                if (registeredActions.TryGetValue(id, out ActionDefinition existing))
                {
                    // Different assets sharing an ID may be a configuration error.
                    if (existing != action)
                    {
                        Debug.LogWarning(
                            $"Conflicting Action ID '{id}' detected. " +
                            $"Keeping '{existing.name}', ignoring '{action.name}'.",
                            this
                        );

                    }

                    continue;

                }

                // Register the unique action.
                registeredActions.Add(id, action);
                resolvedActions.Add(action);

            }

        }

        Version++;
    }

    // Add a set at runtime.
    public void AddSet(ActionSet set)
    {
        if (set == null || actionSets.Contains(set))
        {
            return;

        }

        actionSets.Add(set);

        RefreshActions();

    }

    // Remove a set at runtime.
    public void RemoveSet(ActionSet set)
    {
        if (set != null && actionSets.Remove(set))
        {
            RefreshActions();

        }

    }

    // Test the resolver without needing to connect the Action Menu.
    [ContextMenu("Test / Log Resolved Actions")]

    private void DebugResolvedActions()
    {
        RefreshActions();

        Debug.Log($"{name} has {resolvedActions.Count} unique actions:", this);

        foreach (ActionDefinition action in resolvedActions)
        {
            Debug.Log($"- {action.DisplayName} ({action.Id})", this);

        }

    }

}