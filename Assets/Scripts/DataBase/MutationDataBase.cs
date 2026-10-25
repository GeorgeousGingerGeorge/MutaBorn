using UnityEngine;
using System.Collections.Generic;
using System;


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

    private static readonly Dictionary<MutationID, Func<BaseMutation>> mutationFactory = new Dictionary<MutationID, Func<BaseMutation>>
    {
        {MutationID.Vampirism, () => new VampirismMutation() },
        {MutationID.LaunchExplosive, () => new VampirismMutation() },
        {MutationID.Sage, () => new VampirismMutation() },
        {MutationID.AssassinClaws, () => new VampirismMutation() },
       
    };


    public void Initialize() 
    {
        foreach (MutationLookupEntry entry in entries)
        {
            mutationLookup.Add((entry.slot, entry.strain), entry.mutations);
        }

        
    }


    public BaseMutation CreateMutation(MutationID id)
    {
        if (mutationFactory.TryGetValue(id, out Func<BaseMutation> factory)) 
        {
            return factory();
        }

        return null;

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