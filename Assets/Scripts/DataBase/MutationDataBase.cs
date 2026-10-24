using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public struct MutationDefinition 
{
    [SerializeField]public int intialCost;
    [SerializeField]public float stageMultiplier;
}

[CreateAssetMenu(fileName = "MutationDataBase", menuName = "Scriptable Objects/MutationDataBase")]
public class MutationDataBase : ScriptableObject
{
    [SerializeField] private Dictionary<MutationID, MutationDefinition> mutationCost = new Dictionary<MutationID, MutationDefinition>();
    [SerializeField] private List<MutationLookupEntry> entries = new List<MutationLookupEntry>();

    private Dictionary<(BodySlots, MutationStrain), List<MutationID>> mutationLookup = new Dictionary<(BodySlots, MutationStrain), List<MutationID>>();


    public void Initialize() 
    {
        foreach (MutationLookupEntry entry in entries)
        {
            mutationLookup.Add((entry.slot, entry.strain), entry.mutations);
        }
    }

    public List<MutationID> GetMutationsForSlots(BodySlots slot, MutationStrain strain) 
    {

        if (mutationLookup.TryGetValue((slot, strain), out List<MutationID> mutations))
        {
            return mutations; 
        }
        return null;
    }
}