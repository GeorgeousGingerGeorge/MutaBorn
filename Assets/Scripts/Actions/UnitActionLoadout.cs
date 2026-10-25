using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[RequireComponent(typeof(UnitController))]
public class UnitActionLoadout : MonoBehaviour
{
    private UnitController unit;

    private readonly List<ActionDefinition> resolvedActions = new();
    private readonly Dictionary<string, int> successfulUses =
        new(StringComparer.OrdinalIgnoreCase);

    private string lastUsedId;
    private string lastUsedAbilityId;

    public int Version { get; private set; }
    public IReadOnlyList<ActionDefinition> Actions => resolvedActions;



    private void Awake()
    {
        unit = GetComponent<UnitController>();

    }

    private IEnumerator Start()
    {
        yield return null;
        InitializeForCombat();

    }

    public void InitializeForCombat()
    {
        resolvedActions.Clear();
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, ActionDefinition> definitions =
            new(StringComparer.OrdinalIgnoreCase);

        if (unit == null)
        {
            unit = GetComponent<UnitController>();

        }

        if (unit == null || unit.data == null)
        {
            Debug.LogWarning(
                $"Cannot initialize actions for '{name}': UnitData is missing.",
                this);
            Version++;

            return;

        }

        GameDataBase gameData = GameDataBaseLoader.Instance;
        if (gameData == null)
        {
            Debug.LogWarning(
                "Cannot initialize actions: GameDataBase could not be loaded from Resources.",
                this);
            Version++;

            return;

        }

        // Shared starting actions are configured once in GameDataBase.
        AddActionsFromSet(gameData.universalActions, "Universal", ids, definitions);

        // The race definition itself owns its racial ActionSet.
        if (gameData.raceData != null &&
            !string.IsNullOrWhiteSpace(unit.data.raceName))
        {
            DynamicStats race = gameData.raceData.GetRace(unit.data.raceName);
            if (race != null)
            {
                AddActionsFromSet(
                    race.RacialActions, $"Race ({race.name})", ids, definitions);

            }
            else
            {
                Debug.LogWarning(
                    $"No race definition found for '{unit.data.raceName}' on '{name}'.",
                    this);

            }

        }

        // Equipment actions come from the items the unit actually brought into combat.
        if (unit.data.equippedItems != null)
        {
            foreach (KeyValuePair<EquipmentSlots, EquipmentData> equipped in unit.data.equippedItems)
            {
                EquipmentData item = equipped.Value;

                if (item == null)
                {
                    continue;

                }

                AddActionsFromSet(
                    item.GrantedActionSet,
                    $"Equipment ({item.name}, {equipped.Key})",
                    ids,
                    definitions);

            }

        }

        // Mutation actions come from the mutations already equipped on UnitData.
        if (gameData.mutationData != null && unit.data.activeMutations != null)
        {
            foreach (KeyValuePair<BodySlots, BaseMutation> equipped in unit.data.activeMutations)
            {
                BaseMutation mutation = equipped.Value;

                if (mutation == null)
                {
                    continue;

                }

                ActionSet mutationActions = gameData.mutationData.GetGrantedActionSet(mutation.ID);

                AddActionsFromSet(
                    mutationActions,
                    $"Mutation ({mutation.name}, {equipped.Key})",
                    ids,
                    definitions);

            }

        }

        // Future source: learned skills. These belong to UnitData.

        Version++;

    }

    private void AddActionsFromSet(
        ActionSet set,
        string source,
        HashSet<string> ids,
        Dictionary<string, ActionDefinition> definitions)
    {
        if (set == null)
        {
            return;

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
                    $"Action '{action.name}' in {source} has an empty Action ID.",
                    this);

                continue;

            }

            if (!ids.Add(id))
            {
                if (definitions.TryGetValue(id, out ActionDefinition existing) && existing != action)
                {
                    Debug.LogWarning(
                        $"Conflicting Action ID '{id}' in {source}: " +
                        $"keeping '{existing.name}', ignoring '{action.name}'.",
                        this);

                }

                continue;

            }

            definitions.Add(id, action);
            resolvedActions.Add(action);

        }

    }

    public IEnumerable<ActionDefinition> GetActions(ActionMenuCategory category)
    {
        foreach (ActionDefinition action in resolvedActions)
        {
            if (action != null && action.Category == category)
            {
                yield return action;

            }

        }

    }


    public ActionDefinition MostUsed => FindMostUsed(abilitiesOnly: false);
    public ActionDefinition LastUsed => FindAvailableAction(lastUsedId, abilitiesOnly: false);

    // Quick slots cover active actions from the Abilities menu, not passives.
    public ActionDefinition MostUsedAbility => FindMostUsed(abilitiesOnly: true);
    public ActionDefinition LastUsedAbility => FindAvailableAction(lastUsedAbilityId, abilitiesOnly: true);

    private ActionDefinition FindMostUsed(bool abilitiesOnly)
    {
        ActionDefinition best = null;
        int highestCount = 0;


        foreach (ActionDefinition action in resolvedActions)
        {
            if (!IsEligible(action, abilitiesOnly))
            {
                continue;

            }

            if (successfulUses.TryGetValue(action.Id.Trim(), out int count) &&
                count > highestCount)
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

        foreach (ActionDefinition action in resolvedActions)
        {
            if (IsEligible(action, abilitiesOnly) &&
                string.Equals(action.Id.Trim(), actionId.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return action;

            }

        }

        return null;

    }

    private static bool IsEligible(ActionDefinition action, bool abilitiesOnly)
    {
        return action != null &&
               !action.IsPassive &&
               !string.IsNullOrWhiteSpace(action.Id) &&
               (!abilitiesOnly || action.Category == ActionMenuCategory.Abilities);

    }

    public void RecordSuccessfulUse(ActionDefinition action)
    {
        if (!IsEligible(action, abilitiesOnly: false))
        {
            return;

        }

        ActionDefinition available = FindAvailableAction(action.Id, abilitiesOnly: false);
        if (available == null)
        {
            return;

        }

        string id = available.Id.Trim();

        successfulUses.TryGetValue(id, out int count);
        successfulUses[id] = count + 1;
        lastUsedId = id;

        if (available.Category == ActionMenuCategory.Abilities)
        {
            lastUsedAbilityId = id;

        }

        Version++;

    }

    // Kept for compatibility with the existing loadout interface.
    // Rebuild only when preparing/reinitializing the unit, not every frame.
    public void NotifyActionsChanged()
    {
        InitializeForCombat();
    }

    [ContextMenu("Test / Log Cached Actions")]
    private void DebugResolvedActions()
    {
        Debug.Log($"{name} has {resolvedActions.Count} unique combat actions:", this);
        foreach (ActionDefinition action in resolvedActions)
        {
            Debug.Log($"- {action.DisplayName} ({action.Id})", this);

        }

    }


    [ContextMenu("Test / Record First Ability Used")]
    private void DebugFirstAbility() => DebugAbilityAt(0);

    [ContextMenu("Test / Record Second Ability Used")]
    private void DebugSecondAbility() => DebugAbilityAt(1);

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
                Debug.Log($"Recorded successful use of: {action.DisplayName}", this);
                return;

            }

        }

        Debug.LogWarning(
            $"No active ability at index {abilityIndex} on {name}.", this);

    }

}
