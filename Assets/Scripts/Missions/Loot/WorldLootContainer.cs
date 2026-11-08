using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public enum WorldLootContainerKind
{
    Chest,
    Searchable,
    Podium

}

public sealed class WorldLootContainer : WorldInteractable
{
    [Header("Container")]
    [SerializeField] private WorldLootContainerKind kind;
    [Tooltip("Optional override. Otherwise uses the table on this mission's LevelMissionData.")]
    [SerializeField] private MissionLootTable lootTableOverride;
    [Tooltip("These entries are always present, regardless of the mission loot table.")]
    [SerializeField] private List<WorldLootEntry> specificLoot = new();
    [Tooltip("Chest always rolls at least one. Podium uses only Specific Loot.")]
    [SerializeField, Min(1)] private int tableRolls = 1;
    [Tooltip("Only used by Searchable. A failed roll can leave it empty.")]
    [SerializeField, Range(0, 100)] private int searchFindChancePercent = 50;

    [Header("First Inspection Results")]
    [SerializeField] private UnityEvent onLootFound = new();
    [SerializeField] private UnityEvent onEmpty = new();

    [Header("Runtime Inventory (Preview Only)")]
    [SerializeField] private List<WorldLootStack> contents = new();
    [SerializeField] private bool generated;
    [SerializeField] private bool inspected;
    [SerializeField] private bool collected;

    public IReadOnlyList<WorldLootStack> Contents => contents;
    public override string InteractionMenuLabel => kind switch
    {
        WorldLootContainerKind.Chest => inspected ? "Inspect Chest" : "Open Chest",
        WorldLootContainerKind.Searchable => "Search Object",
        _ => "Inspect Podium"

    };

    private void Start()
    {
        GenerateContents();

    }

    public void GenerateContents()
    {
        if (generated)
        {
            return;

        }

        generated = true;
        contents.Clear();
        foreach (WorldLootEntry entry in specificLoot)
        {
            AddEntry(entry);

        }

        if (kind == WorldLootContainerKind.Podium ||
            kind == WorldLootContainerKind.Searchable && Random.Range(0, 100) >= searchFindChancePercent)
        {
            return;

        }

        MissionLootTable table = lootTableOverride;
        if (table == null && MissionObjectiveManager.Instance != null &&
            MissionObjectiveManager.Instance.LevelMission != null)
        {
            table = MissionObjectiveManager.Instance.LevelMission.LootTable;

        }

        if (table == null)
        {
            Debug.LogWarning($"{name} has no mission loot table; only its Specific Loot is available.", this);

            return;

        }

        for (int i = 0; i < tableRolls; i++)
        {
            AddEntry(table.Roll());

        }

    }

    private void AddEntry(WorldLootEntry entry)
    {
        if (entry != null && entry.IsValid)
        {
            contents.Add(new WorldLootStack(entry));

        }

    }

    protected override void OnInteractionSucceeded(UnitController actor)
    {
        GenerateContents();
        StringBuilder result = new StringBuilder($"{actor.name} inspected {name}: ");
        if (contents.Count == 0)
        {
            result.Append("Empty");

        }
        else
        {
            for (int i = 0; i < contents.Count; i++)
            {
                if (i > 0)
                {
                    result.Append(", ");

                }

                result.Append($"{contents[i].quantity}x {contents[i].DisplayName}");

            }

        }

        Debug.Log(result.ToString(), this);
        if (!collected && MissionSpoils.Instance != null)
        {
            MissionSpoils.Instance.Collect(contents);
            collected = true;

        }

        if (!inspected)
        {
            inspected = true;
            if (contents.Count > 0)
            {
                onLootFound.Invoke();

            }
            else
            {
                onEmpty.Invoke();

            }

        }

    }

}
