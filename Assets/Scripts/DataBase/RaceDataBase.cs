using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RaceDataBase", menuName = "Scriptable Objects/RaceDataBase")]
public class RaceDataBase : ScriptableObject
{
    [SerializeField] private List<DynamicStats> races = new List<DynamicStats>();

    public DynamicStats GetRace(string raceName)
    {
        if (string.IsNullOrWhiteSpace(raceName))
        {
            return null;
        }

        foreach (DynamicStats race in races)
        {
            if (race != null && string.Equals(race.RaceName, raceName, StringComparison.OrdinalIgnoreCase))
            {
                return race;
            }
        }

        return null;
    }
}