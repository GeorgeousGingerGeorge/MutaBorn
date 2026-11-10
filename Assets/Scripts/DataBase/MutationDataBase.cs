using UnityEngine;
using System.Collections.Generic;
using System;
using MutaBorn.Gameplay;


[System.Serializable]
public struct MutationDefinition 
{
    [SerializeField]public int intialCost;
    [SerializeField]public float stageMultiplier;
}

[Serializable]
public class MutationActionGrant
{
    public MutationID mutationId;
    public ActionSet actionSet;
    public EffectCollection effectCollection;

}

[Serializable]
public sealed class MutationStatMultiplier
{
    [SerializeField] private StatIdentifier stat;
    [SerializeField] private float percent;

    public StatIdentifier Stat => stat;
    public float Percent => percent;

}

[Serializable]
public sealed class MutationNodeDefinition
{
    [SerializeField] private MutationID id;
    [SerializeField] private string displayName;
    [TextArea(2, 5)] [SerializeField] private string description;
    [SerializeField] private bool hiddenFromAcquisition;
    [SerializeField] private string familyId;
    [SerializeField] private MutationStrain strain;
    [Min(1)]
    [SerializeField] private int level = 1;
    [Header("Complete Stats At This Level (Not Added To Previous Level)")]
    [SerializeField] private MutationBonus bonus = new MutationBonus();
    [SerializeField] private List<MutationStatMultiplier> percentBonuses = new List<MutationStatMultiplier>();
    [Header("Granted Abilities and Effects")]
    [SerializeField] private ActionDefinition[] grantedActions = Array.Empty<ActionDefinition>();
    [SerializeField] private EffectGrant[] grantedEffects = Array.Empty<EffectGrant>();
    [Header("Optional Shared Collections (For Existing Assets)")]
    [SerializeField] private ActionSet actionSet;
    [SerializeField] private EffectCollection effectCollection;
    [Header("Loadout Slots Occupied By This Mutation")]
    [Tooltip("Optional: these slots cannot hold normal items while the mutation is active.")]
    [SerializeField] private UnitSlotId[] occupiedEquipmentSlots = Array.Empty<UnitSlotId>();
    [Header("Next Evolution Choices")]
    [SerializeField] private MutationID[] nextEvolutions = Array.Empty<MutationID>();

    public MutationID Id => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id.ToString() : displayName;
    public string Description => description;
    public bool HiddenFromAcquisition => hiddenFromAcquisition;
    public IReadOnlyList<MutationStatMultiplier> PercentBonuses => percentBonuses ?? (IReadOnlyList<MutationStatMultiplier>)Array.Empty<MutationStatMultiplier>();
    public string FamilyId => familyId == null ? string.Empty : familyId.Trim();
    public MutationStrain Strain => strain;
    public int Level => level;
    public MutationBonus Bonus => bonus;
    public IReadOnlyList<ActionDefinition> GrantedActions => grantedActions ?? Array.Empty<ActionDefinition>();
    public IReadOnlyList<EffectGrant> GrantedEffects => grantedEffects ?? Array.Empty<EffectGrant>();
    public ActionSet ActionSet => actionSet;
    public EffectCollection EffectCollection => effectCollection;
    public IReadOnlyList<MutationID> NextEvolutions => nextEvolutions ?? Array.Empty<MutationID>();
    public IReadOnlyList<UnitSlotId> OccupiedEquipmentSlots =>
        occupiedEquipmentSlots ?? Array.Empty<UnitSlotId>();

    public bool CanEvolveTo(MutationID next)
    {
        foreach (MutationID option in NextEvolutions)
        {
            if (option == next)
            {
                return true;

            }

        }

        return false;

    }

}

[CreateAssetMenu(fileName = "MutationDataBase", menuName = "Scriptable Objects/MutationDataBase")]
public class MutationDataBase : ScriptableObject
{
    [SerializeField] private Dictionary<MutationID, MutationDefinition> mutationCost = new Dictionary<MutationID, MutationDefinition>();
    public Dictionary<MutationID, MutationDefinition> MutationCost => mutationCost;
    [SerializeField] private List<MutationLookupEntry> entries = new List<MutationLookupEntry>();

    [Header("Fixed Mutation Evolution Tree")]
    [SerializeField] private List<MutationNodeDefinition> mutationNodes = new List<MutationNodeDefinition>();

    public IReadOnlyList<MutationNodeDefinition> MutationNodes => mutationNodes;

    public MutationNodeDefinition GetNode(MutationID id)
    {
        if (id == MutationID.none || mutationNodes == null)
        {
            return null;

        }

        foreach (MutationNodeDefinition node in mutationNodes)
        {
            if (node != null && node.Id == id)
            {
                return node;

            }

        }

        return null;

    }

    // Detect configuration mistakes before they silently invalidate an evolution.
    private void OnValidate()
    {
        if (mutationNodes == null)
        {
            return;

        }

        HashSet<MutationID> ids = new HashSet<MutationID>();
        foreach (MutationNodeDefinition node in mutationNodes)
        {
            if (node == null || node.Id == MutationID.none || !ids.Add(node.Id))
            {
                Debug.LogWarning("Mutation tree contains an empty or duplicate Mutation ID.", this);

                continue;

            }

            if (string.IsNullOrEmpty(node.FamilyId) || node.Level < 1)
            {
                Debug.LogWarning($"Mutation {node.Id} requires a family ID and positive level.", this);

            }

        }

        foreach (MutationNodeDefinition node in mutationNodes)
        {
            if (node == null)
            {
                continue;

            }

            foreach (MutationID nextId in node.NextEvolutions)
            {
                MutationNodeDefinition next = GetNode(nextId);
                if (next == null || nextId == node.Id ||
                    !string.Equals(node.FamilyId, next.FamilyId, StringComparison.OrdinalIgnoreCase) ||
                    next.Level <= node.Level)
                {
                    Debug.LogWarning($"Invalid evolution {node.Id} -> {nextId}. Check ID, family, and level.", this);

                }

            }

        }

    }

    [Header("Granted Mutation Actions and Effects")]
    [SerializeField] private List<MutationActionGrant> actionGrants = new List<MutationActionGrant>();

    private Dictionary<(BodySlots, MutationStrain), List<MutationID>> mutationLookup = new Dictionary<(BodySlots, MutationStrain), List<MutationID>>();

    private static readonly Dictionary<MutationID, Func<BaseMutation>> mutationFactory = new Dictionary<MutationID, Func<BaseMutation>>
    {
        {MutationID.Vampirism, () => new VampirismMutation() },
        {MutationID.AssassinClaws, () => new AssassinClawMutation() },
       
    };


    public void Initialize()
    {
        mutationLookup.Clear();
        foreach (MutationLookupEntry entry in entries)
        {
            if (entry != null)
            {
                mutationLookup[(entry.slot, entry.strain)] = entry.mutations;

            }

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


    public EffectCollection GetGrantedEffectCollection(MutationID mutationId)
    {
        if (mutationId == MutationID.none)
        {
            return null;

        }

        foreach (MutationActionGrant grant in actionGrants)
        {
            if (grant != null && grant.mutationId == mutationId)
            {
                return grant.effectCollection;

            }

        }

        return null;

    }

    public ActionSet GetGrantedActionSet(MutationID mutationId)
    {
        if (mutationId == MutationID.none)
        {
            return null;

        }

        foreach (MutationActionGrant grant in actionGrants)
        {
            if (grant != null && grant.mutationId == mutationId)
            {
                return grant.actionSet;

            }

        }

        return null;

    }
}