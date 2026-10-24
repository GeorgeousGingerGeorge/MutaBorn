using System;
using System.Collections.Generic;
using UnityEngine;



public class UnitActionLoadout : MonoBehaviour
{
    [Header("Fallback Actions (used only without an ActionResolver)")]

    [SerializeField] private List<ActionDefinition> actions = new List<ActionDefinition>();

    private ActionResolver actionResolver;

    // Runtime-only history. Dictionary keys are action IDs so duplicate definitions
    // of the same action are treated as one ability.
    private readonly Dictionary<string, int> successfulUses =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    private string lastUsedId;
    private string lastUsedAbilityId;
    private int localVersion;

    private void Awake()
    {
        actionResolver = GetComponent<ActionResolver>();

    }

    // ActionMenuUI watches this value to refresh an open menu.
    public int Version => localVersion + (actionResolver != null ? actionResolver.Version : 0);

    // If a resolver is present, its deduplicated list is the source of truth.
    // Otherwise, retain support for older units with manually assigned actions.
    public IReadOnlyList<ActionDefinition> Actions =>
        actionResolver != null ? actionResolver.ResolvedActions : actions;

    public IEnumerable<ActionDefinition> GetActions(ActionMenuCategory category)
    {
        HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ActionDefinition action in Actions)
        {
            if (action == null || action.Category != category)
            {
                continue;

            }

            string id = action.Id != null ? action.Id.Trim() : null;
            if (string.IsNullOrEmpty(id) || !seenIds.Add(id))
            {
                continue;

            }

            yield return action;

        }

    }

    // All active actions, not just the Abilities menu.
    public ActionDefinition MostUsed => FindMostUsed(abilitiesOnly: false);
    public ActionDefinition LastUsed => FindAvailableAction(lastUsedId, abilitiesOnly: false);

    // These two are used by the Most Used / Last Used ABILITY shortcut buttons.
    // Basic Attacks and Defense are intentionally not included.
    public ActionDefinition MostUsedAbility => FindMostUsed(abilitiesOnly: true);
    public ActionDefinition LastUsedAbility => FindAvailableAction(lastUsedAbilityId, abilitiesOnly: true);

    private ActionDefinition FindMostUsed(bool abilitiesOnly)
    {
        ActionDefinition best = null;
        int highestCount = 0;
        HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ActionDefinition action in Actions)
        {
            if (!IsEligible(action, abilitiesOnly))
            {
                continue;

            }

            string id = action.Id.Trim();
            if (!seenIds.Add(id))
            {
                continue;

            }

            int count;
            if (successfulUses.TryGetValue(id, out count) && count > highestCount)
            {
                best = action;
                highestCount = count;

            }

        }

        return best;
    }

    private ActionDefinition FindAvailableAction(string actionId, bool abilitiesOnly)
    {
        if (string.IsNullOrWhiteSpace(actionId))
        {
            return null;

        }

        foreach (ActionDefinition action in Actions)
        {
            if (IsEligible(action, abilitiesOnly) && string.Equals(action.Id.Trim(), actionId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return action;

            }

        }

        // If equipment/mutations removed the action, it cannot be used from a quick slot.
        return null;
    }

    private static bool IsEligible(ActionDefinition action, bool abilitiesOnly)
    {
        return action != null && !action.IsPassive && !string.IsNullOrWhiteSpace(action.Id) &&
               (!abilitiesOnly || action.Category == ActionMenuCategory.Abilities);

    }

    public void RecordSuccessfulUse(ActionDefinition action)
    {
        if (!IsEligible(action, abilitiesOnly: false))
            return;

        // Confirm that the unit still possesses the action.
        ActionDefinition available = FindAvailableAction(action.Id, abilitiesOnly: false);
        if (available == null)
            return;

        string id = available.Id.Trim();
        int count;
        successfulUses.TryGetValue(id, out count);
        successfulUses[id] = count + 1;

        lastUsedId = id;
        if (available.Category == ActionMenuCategory.Abilities)
            lastUsedAbilityId = id;

        localVersion++;
    }

    public void NotifyActionsChanged()
    {
        localVersion++;
    }

    // Debug helpers: simulate successful active-ability use before combat exists.
    [ContextMenu("Test / Record First Ability Used")]
    private void DebugFirstAbility() { DebugAbilityAt(0); }

    [ContextMenu("Test / Record Second Ability Used")]
    private void DebugSecondAbility() { DebugAbilityAt(1); }

    private void DebugAbilityAt(int abilityIndex)
    {
        int index = 0;
        foreach (ActionDefinition action in GetActions(ActionMenuCategory.Abilities))
        {
            if (action.IsPassive)
            {
                continue;

            }

            if (index++ == abilityIndex)
            {
                RecordSuccessfulUse(action);
                Debug.Log("Recorded successful use of: " + action.DisplayName, this);

                return;

            }

        }

        Debug.LogWarning("No active ability at index " + abilityIndex + " on " + name + ".", this);

    }

}
