using System;
using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(fileName = "ActionDataBase", menuName = "Game/Actions/Action Database")]

public class ActionDataBase : ScriptableObject
{
    [Header("Registered Actions")]

    [SerializeField]
    private List<ActionDefinition> actions = new();

    // Read-only access to registered actions.
    public IReadOnlyList<ActionDefinition> Actions => actions;

    // Retrieve an action using its unique ID.
    public ActionDefinition GetAction(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId))
        {
            return null;

        }

        foreach (ActionDefinition action in actions)
        {
            if (action == null)
            {
                continue;

            }

            if (string.Equals(action.Id, actionId, StringComparison.OrdinalIgnoreCase))
            {
                return action;

            }

        }

        return null;

    }

    // Validate registered actions in the Unity Editor.
    private void OnValidate()
    {
        HashSet<string> registeredIds = new(StringComparer.OrdinalIgnoreCase);

        foreach (ActionDefinition action in actions)
        {
            if (action == null)
            {
                continue;

            }

            string id = action.Id?.Trim();

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"Action '{action.name}' has no valid Action ID.", this);

                continue;

            }

            if (!registeredIds.Add(id))
            {
                Debug.LogWarning($"Duplicate Action ID detected: '{id}'", this );

            }

        }

    }

}