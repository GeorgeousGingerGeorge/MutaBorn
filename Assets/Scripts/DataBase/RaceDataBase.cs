using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RaceDataBase", menuName = "Scriptable Objects/RaceDataBase")]
public class RaceDataBase : ScriptableObject
{
    [SerializeField] private Dictionary<string, DynamicStats> raceDataBase;


    public DynamicStats GetRace(string raceName) 
    {
        if (raceDataBase.TryGetValue(raceName, out DynamicStats race)) 
        {
            return race;
        }
       return null;
    }
}
