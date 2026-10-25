using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(fileName = "NewActionSet", menuName = "Game/Actions/Action Set")]

public class ActionSet : ScriptableObject
{
    [Header("Action Set Information")]

    [SerializeField]
    private string setName = "New Action Set";

    [TextArea(2, 4)]
    [SerializeField]
    private string description;

    [Header("Granted Actions")]

    [SerializeField]
    private List<ActionDefinition> actions = new();

    // Public read-only access.
    public string SetName => setName;
    public string Description => description;
    public IReadOnlyList<ActionDefinition> Actions => actions;


    // Checks whether this set contains an action.
    public bool ContainsAction(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId))
        {
            return false;

        }

        foreach (ActionDefinition action in actions)
        {
            if (action == null)
            {
                continue;

            }

            if (string.Equals(
                action.Id,
                actionId,
                System.StringComparison.OrdinalIgnoreCase))
            {
                return true;

            }

        }

        return false;

    }

    // Detect duplicate actions within this set.
    private void OnValidate()
    {
        HashSet<string> registeredIds =
            new(System.StringComparer.OrdinalIgnoreCase);

        foreach (ActionDefinition action in actions)
        {
            if (action == null)
            {
                continue;

            }

            string id = action.Id?.Trim();

            if (string.IsNullOrEmpty(id))
            {
                continue;

            }

            if (!registeredIds.Add(id))
            {
                Debug.LogWarning($"ActionSet '{name}' contains duplicate Action ID: '{id}'", this);

            }

        }

    }

}