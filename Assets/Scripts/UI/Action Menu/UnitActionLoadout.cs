using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TEMPORARY UI adapter for a unit's actions and successful-use history.
/// Later, UnitActions / CombatUnit can become the authoritative provider.
/// Does not spend AP or execute combat actions.
/// </summary>
public class UnitActionLoadout : MonoBehaviour
{
    [SerializeField] private List<ActionDefinition> actions = new List<ActionDefinition>();

    // Runtime-only history. Never put this data on a shared ScriptableObject.
    private readonly Dictionary<ActionDefinition, int> successfulUses =
        new Dictionary<ActionDefinition, int>();
    private ActionDefinition lastUsed;
    private ActionDefinition lastUsedAbility;

    public int Version { get; private set; }
    public IReadOnlyList<ActionDefinition> Actions => actions;

    public IEnumerable<ActionDefinition> GetActions(ActionMenuCategory category)
    {
        foreach (ActionDefinition action in actions)
            if (action != null && action.Category == category)
                yield return action;
    }

    public ActionDefinition MostUsed
    {
        get
        {
            ActionDefinition best = null;
            int highest = 0;
            foreach (ActionDefinition action in actions)
            {
                if (action == null) continue;
                int count = successfulUses.TryGetValue(action, out int value) ? value : 0;
                if (count > highest)
                {
                    best = action;
                    highest = count;
                }
            }
            return best;
        }
    }

    public ActionDefinition LastUsed => lastUsed;

    // The two quick slots intentionally focus on ABILITIES, not Fists/Defense.
    public ActionDefinition MostUsedAbility
    {
        get
        {
            ActionDefinition best = null;
            int highest = 0;
            foreach (ActionDefinition action in actions)
            {
                if (action == null || action.Category != ActionMenuCategory.Abilities) continue;
                int count = successfulUses.TryGetValue(action, out int value) ? value : 0;
                if (count > highest)
                {
                    best = action;
                    highest = count;
                }
            }
            return best;
        }
    }
    public ActionDefinition LastUsedAbility => lastUsedAbility;

    /// <summary>
    /// Call ONLY after gameplay has successfully executed the action.
    /// Do not call just because a UI button was clicked or targeting began.
    /// </summary>
    public void RecordSuccessfulUse(ActionDefinition action)
    {
        if (action == null || !actions.Contains(action)) return;
        successfulUses.TryGetValue(action, out int count);
        successfulUses[action] = count + 1;
        lastUsed = action;
        if (action.Category == ActionMenuCategory.Abilities)
            lastUsedAbility = action;
        Version++;
    }

    /// <summary>Call if the unit gains/loses actions at runtime.</summary>
    public void NotifyActionsChanged() => Version++;

    // DEBUG ONLY: enables testing the ABILITY quick slots without combat.
    [ContextMenu("Test / Record First Ability Used")]
    private void DebugFirstAbility() => DebugAbilityAt(0);

    [ContextMenu("Test / Record Second Ability Used")]
    private void DebugSecondAbility() => DebugAbilityAt(1);

    private void DebugAbilityAt(int abilityIndex)
    {
        int current = 0;
        foreach (ActionDefinition action in actions)
        {
            if (action == null || action.Category != ActionMenuCategory.Abilities) continue;
            if (current++ == abilityIndex)
            {
                RecordSuccessfulUse(action);
                return;
            }
        }
        Debug.LogWarning($"No ability at index {abilityIndex} on {name}.", this);
    }
}
