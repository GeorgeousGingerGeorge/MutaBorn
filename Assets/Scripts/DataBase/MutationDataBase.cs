using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MutationDataBase", menuName = "Scriptable Objects/MutationDataBase")]
public class MutationDataBase : ScriptableObject
{
    [SerializeField] private Dictionary<MutationID, int> mutationCost = new Dictionary<MutationID, int>();
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