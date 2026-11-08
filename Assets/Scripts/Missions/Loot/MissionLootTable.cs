using System;
using System.Collections.Generic;
using UnityEngine;

public enum WorldLootKind
{
    Resource,
    Item

}

[Serializable]
public sealed class WorldLootEntry
{
    public WorldLootKind kind;
    public string resourceId;
    public ItemData item;
    [Min(1)] public int minimumQuantity = 1;
    [Min(1)] public int maximumQuantity = 1;
    [Min(1)] public int weight = 1;

    public bool IsValid => kind == WorldLootKind.Item ? item != null : !string.IsNullOrWhiteSpace(resourceId);

}

[Serializable]
public sealed class WorldLootStack
{
    public WorldLootKind kind;
    public string resourceId;
    public ItemData item;
    public int quantity;

    public WorldLootStack(WorldLootEntry entry)
    {
        kind = entry.kind;
        resourceId = entry.resourceId;
        item = entry.item;
        quantity = UnityEngine.Random.Range(Mathf.Max(1, entry.minimumQuantity),
            Mathf.Max(entry.minimumQuantity, entry.maximumQuantity) + 1);

    }

    public string DisplayName => kind == WorldLootKind.Item
        ? (item != null ? item.itemName : "Missing Item") : resourceId;

}

[CreateAssetMenu(fileName = "Mission Loot Table", menuName = "MutaBorn/Missions/Loot Table")]
public sealed class MissionLootTable : ScriptableObject
{
    [SerializeField] private List<WorldLootEntry> possibleLoot = new();

    public WorldLootEntry Roll()
    {
        int totalWeight = 0;
        foreach (WorldLootEntry entry in possibleLoot)
        {
            if (entry != null && entry.IsValid && entry.weight > 0)
            {
                totalWeight += entry.weight;

            }

        }

        if (totalWeight == 0)
        {
            return null;

        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        foreach (WorldLootEntry entry in possibleLoot)
        {
            if (entry == null || !entry.IsValid || entry.weight <= 0)
            {
                continue;

            }

            roll -= entry.weight;
            if (roll < 0)
            {
                return entry;

            }

        }

        return null;

    }

}
