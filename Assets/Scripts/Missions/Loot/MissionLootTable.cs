using System;
using System.Collections.Generic;
using UnityEngine;

public enum WorldLootKind
{
    Resource,
    Item,
    EquipmentTemplate

}

[Serializable]
public sealed class WorldLootEntry
{
    public WorldLootKind kind;
    public string resourceId;
    public ItemData item;
    [Tooltip("For Equipment Template entries, choose an Equipment Type and stable Template ID.")]
    public EquipmentTemplateReference equipmentTemplate;
    [Min(1)] public int minimumQuantity = 1;
    [Min(1)] public int maximumQuantity = 1;
    [Min(1)] public int weight = 1;

    public bool IsValid => kind switch
    {
        WorldLootKind.Item => item != null,
        WorldLootKind.EquipmentTemplate => equipmentTemplate.TryResolve(out _),
        _ => !string.IsNullOrWhiteSpace(resourceId)

    };

}

[Serializable]
public sealed class WorldLootStack
{
    public WorldLootKind kind;
    public string resourceId;
    public ItemData item;
    public int quantity;
    public EquipmentInstance generatedEquipment;

    public WorldLootStack(WorldLootEntry entry) : this(entry, default, false)
    {

    }

    public WorldLootStack(WorldLootEntry entry, EquipmentStarDice extraStarRolls, bool singleItem = false)
    {
        kind = entry.kind;
        resourceId = entry.resourceId;
        item = entry.item;
        quantity = kind == WorldLootKind.EquipmentTemplate || singleItem && kind == WorldLootKind.Item
            ? 1 : UnityEngine.Random.Range(Mathf.Max(1, entry.minimumQuantity),
                Mathf.Max(entry.minimumQuantity, entry.maximumQuantity) + 1);
        if (kind == WorldLootKind.EquipmentTemplate)
        {
            generatedEquipment = EquipmentGenerator.CreateLootEquipment(entry.equipmentTemplate, extraStarRolls);

        }

    }

    public string DisplayName => kind switch
    {
        WorldLootKind.Item => item != null ? item.itemName : "Missing Item",
        WorldLootKind.EquipmentTemplate => generatedEquipment.IsValid
            ? $"{generatedEquipment.itemName} ({(int)generatedEquipment.rarity}★)" : "Missing Equipment",
        _ => resourceId

    };

}

[CreateAssetMenu(fileName = "Mission Loot Table", menuName = "MutaBorn/Missions/Loot Table")]
public sealed class MissionLootTable : ScriptableObject
{
    [SerializeField] private List<WorldLootEntry> possibleLoot = new();

    [Header("Separated Equipment and Resource Drops")]
    [Tooltip("Enable separate equipment and resource selection counts. Off retains old one-roll tables.")]
    [SerializeField] private bool useSeparateDropRanges;
    [SerializeField, Min(0)] private int minimumEquipmentDrops;
    [SerializeField, Min(0)] private int maximumEquipmentDrops = 1;
    [SerializeField, Min(0)] private int minimumResourceDrops;
    [SerializeField, Min(0)] private int maximumResourceDrops = 1;

    [Header("Extra Equipment Stars (Loot Only)")]
    [SerializeField] private EquipmentStarDice extraStarRolls;

    public WorldLootEntry Roll()
    {
        return RollEntry(null);

    }

    public List<WorldLootStack> RollContents()
    {
        List<WorldLootStack> rewards = new();
        if (!useSeparateDropRanges)
        {
            WorldLootEntry entry = Roll();
            if (entry != null)
            {
                rewards.Add(new WorldLootStack(entry, extraStarRolls));

            }

            return rewards;

        }

        int equipmentCount = UnityEngine.Random.Range(Mathf.Max(0, minimumEquipmentDrops),
            Mathf.Max(minimumEquipmentDrops, maximumEquipmentDrops) + 1);
        int resourceCount = UnityEngine.Random.Range(Mathf.Max(0, minimumResourceDrops),
            Mathf.Max(minimumResourceDrops, maximumResourceDrops) + 1);

        for (int i = 0; i < equipmentCount; i++)
        {
            WorldLootEntry entry = RollEntry(true);
            if (entry != null)
            {
                rewards.Add(new WorldLootStack(entry, extraStarRolls, true));

            }

        }

        for (int i = 0; i < resourceCount; i++)
        {
            WorldLootEntry entry = RollEntry(false);
            if (entry != null)
            {
                rewards.Add(new WorldLootStack(entry));

            }

        }

        return rewards;

    }

    private WorldLootEntry RollEntry(bool? equipment)
    {
        int totalWeight = 0;
        foreach (WorldLootEntry entry in possibleLoot)
        {
            if (entry != null && entry.IsValid && entry.weight > 0 &&
                (!equipment.HasValue || (entry.kind != WorldLootKind.Resource) == equipment.Value))
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
            if (entry == null || !entry.IsValid || entry.weight <= 0 ||
                equipment.HasValue && (entry.kind != WorldLootKind.Resource) != equipment.Value)
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
