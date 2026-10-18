using System;
using System.Collections.Generic;
using UnityEngine;



[Serializable] public class FactionStartingRelationship
{
    [SerializeField] private FactionData factionA;
    [SerializeField] private FactionData factionB;
    [SerializeField] private FactionRelation relation = FactionRelation.Hostile;

    public FactionData FactionA => factionA;
    public FactionData FactionB => factionB;
    public FactionRelation Relation => relation;

}


public class FactionManager : MonoBehaviour
{
    [Header("Starting Relationships (Unlisted Pairs Are Neutral)")]
    [SerializeField] private List<FactionStartingRelationship> startingRelationships = new List<FactionStartingRelationship>();


    // These collections are runtime-only. Unity does not display/save Dictionary
    // fields in the Inspector, so editable defaults are supplied by lists above.
    private readonly HashSet<FactionData> registeredFactions = new HashSet<FactionData>();
    private readonly Dictionary<string, FactionData> factionsById =
        new Dictionary<string, FactionData>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<FactionData, Dictionary<FactionData, FactionRelation>> relationships =
        new Dictionary<FactionData, Dictionary<FactionData, FactionRelation>>();

    public IReadOnlyCollection<FactionData> Factions => registeredFactions;


    // Subscribe to these from other game systems if they need to react to changes.
    public event Action<FactionData> FactionAdded;
    public event Action<FactionData> FactionRemoved;
    public event Action<FactionData, FactionData, FactionRelation> RelationshipChanged;



    private void Awake()
    {
        FactionData[] allFactions = Resources.LoadAll<FactionData>("Factions");

        foreach (FactionData faction in allFactions)
        {
            RegisterFaction(faction);

        }

        foreach (FactionStartingRelationship entry in startingRelationships)
        {
            if (entry != null)
            {
                SetRelation(entry.FactionA, entry.FactionB, entry.Relation);

            }

        }

    }

    public bool RegisterFaction(FactionData faction)
    {
        if (registeredFactions.Contains(faction))
        {
            return false;

        }

        string id = faction.Id == null ? string.Empty : faction.Id.Trim();

        registeredFactions.Add(faction);
        factionsById.Add(id, faction);
        FactionAdded?.Invoke(faction);

        return true;

    }

    public bool UnregisterFaction(FactionData faction)
    {
        if (!IsRegistered(faction))
        {
            return false;

        }

        registeredFactions.Remove(faction);

        factionsById.Remove(faction.Id);
        relationships.Remove(faction);

        foreach (Dictionary<FactionData, FactionRelation> otherRelations in relationships.Values)
        {
            otherRelations.Remove(faction);

        }

        FactionRemoved?.Invoke(faction);

        return true;

    }

    public bool IsRegistered(FactionData faction)
    {
        return faction != null && registeredFactions.Contains(faction);

    }

    public bool TryGetFaction(string factionId, out FactionData faction)
    {
        faction = null;
        if (string.IsNullOrWhiteSpace(factionId))
        {
            return false;

        }

        return factionsById.TryGetValue(factionId.Trim(), out faction);

    }


    public FactionRelation GetRelation(FactionData a, FactionData b)
    {
        if (a == b)
        {
            return FactionRelation.Allied;

        }

        if (relationships.TryGetValue(a, out Dictionary<FactionData, FactionRelation> otherRelations) && otherRelations.TryGetValue(b, out FactionRelation relation))
        {
            return relation;

        }

        return FactionRelation.Neutral;

    }


    public bool SetRelation(FactionData a, FactionData b, FactionRelation relation)
    {
        if (a == b)
        {
            return false;

        }

        if (GetRelation(a, b) == relation)
        {
            return false;

        }

        if (relation == FactionRelation.Neutral)
        {
            RemoveStoredRelation(a, b);
            RemoveStoredRelation(b, a);

        }
        else
        {
            StoreRelation(a, b, relation);
            StoreRelation(b, a, relation);

        }

        RelationshipChanged?.Invoke(a, b, relation);

        return true;

    }

    public bool DeclareWar(FactionData a, FactionData b)
    {
        return SetRelation(a, b, FactionRelation.Hostile);

    }

    public bool MakePeace(FactionData a, FactionData b)
    {
        return SetRelation(a, b, FactionRelation.Neutral);

    }

    public bool FormAlliance(FactionData a, FactionData b)
    {
        return SetRelation(a, b, FactionRelation.Allied);

    }

    public bool AreHostile(FactionData a, FactionData b)
    {
        return IsRegistered(a) && IsRegistered(b) && GetRelation(a, b) == FactionRelation.Hostile;

    }

    public bool AreAllied(FactionData a, FactionData b)
    {
        return IsRegistered(a) && IsRegistered(b) && GetRelation(a, b) == FactionRelation.Allied;

    }

    public Material GetMaterial(FactionData faction)
    {
        return IsRegistered(faction) ? faction.Material : null;

    }

    private void StoreRelation(FactionData a, FactionData b, FactionRelation relation)
    {
        if (!relationships.TryGetValue(a, out Dictionary<FactionData, FactionRelation> otherRelations))
        {
            otherRelations = new Dictionary<FactionData, FactionRelation>();
            relationships.Add(a, otherRelations);

        }

        otherRelations[b] = relation;

    }

    private void RemoveStoredRelation(FactionData a, FactionData b)
    {
        if (relationships.TryGetValue(a, out Dictionary<FactionData, FactionRelation> otherRelations))
        {
            otherRelations.Remove(b);
            if (otherRelations.Count == 0)
            {
                relationships.Remove(a);

            }

        }

    }

}
