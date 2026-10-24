using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[DisallowMultipleComponent]

[RequireComponent(typeof(UnitController))]
[RequireComponent(typeof(ActionResolver))]

public class UnitStartingActions : MonoBehaviour
{
    [Serializable]
    private class RaceGrant
    {
        public string raceName;
        public ActionSet actionSet;

    }

    [Header("Shared by Every Unit")]

    [SerializeField] private ActionSet universalActions;

    [Header("Race Kits (match the UnitData race name)")]

    [SerializeField] private List<RaceGrant> raceActionSets = new();

    private IEnumerator Start()
    {
        // UnitController initializes UnitData in its Start(). Waiting one frame
        // avoids depending on the relative execution order of Start methods.
        yield return null;

        UnitController unit = GetComponent<UnitController>();
        ActionResolver resolver = GetComponent<ActionResolver>();

        if (unit.data == null)
        {
            Debug.LogWarning(
                $"Cannot grant starting actions to '{name}': UnitData is not initialized.",
                this);

            yield break;

        }

        if (universalActions != null)
            resolver.AddSet(universalActions);

        string raceName = unit.data.raceName?.Trim();
        bool foundRaceKit = false;

        if (!string.IsNullOrEmpty(raceName))
        {
            foreach (RaceGrant entry in raceActionSets)
            {
                if (entry == null || entry.actionSet == null)
                {
                    continue;

                }

                if (!string.Equals(
                        entry.raceName?.Trim(),
                        raceName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;

                }

                resolver.AddSet(entry.actionSet);
                foundRaceKit = true;

                break;

            }

        }

        if (!foundRaceKit)
        {
            Debug.LogWarning(
                $"No race ActionSet mapped for '{raceName ?? "(none)"}' on '{name}'. " +
                "Universal actions, if assigned, are still granted.",
                this);

        }

        Debug.Log(
            $"Starting action grants applied to '{unit.data.characterName}': " +
            $"{resolver.ResolvedActions.Count} unique actions.",
            this);

    }

}
