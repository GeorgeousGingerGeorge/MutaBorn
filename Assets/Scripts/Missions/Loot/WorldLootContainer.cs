using System.Collections.Generic;
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

    public override bool CanInteract(UnitController actor)
    {
        return !inspected && !collected && MissionSpoils.Instance != null &&
            !MissionSpoils.Instance.IsPrepared && base.CanInteract(actor);

    }

    public override string InteractionMenuLabel => kind switch
    {
        WorldLootContainerKind.Chest => inspected ? "Inspect Chest" : "Open Chest",
        WorldLootContainerKind.Searchable => "Search Object",
        _ => "Inspect Podium"

    };

    private void Start()
    {
        if (inspected || collected)
        {
            ShowUsedState();

            return;

        }

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
        MissionSpoils.Instance.Collect(contents);
        collected = true;
        inspected = true;

        string title = kind switch
        {
            WorldLootContainerKind.Chest => "Chest Opened",
            WorldLootContainerKind.Searchable => "Search Results",
            _ => "Podium Loot"

        };
        MissionLootPopup.Show(title, contents, "You found nothing.");

        if (contents.Count > 0)
        {
            onLootFound.Invoke();

        }
        else
        {
            onEmpty.Invoke();

        }

    }

    protected override void OnInteractionFinished(UnitController actor)
    {
        ShowUsedState();

    }

    private void ShowUsedState()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            Material material = renderer.sharedMaterial;
            if (material == null)
            {
                continue;

            }

            int colorProperty = material.HasProperty("_BaseColor")
                ? Shader.PropertyToID("_BaseColor")
                : material.HasProperty("_Color") ? Shader.PropertyToID("_Color") : -1;
            if (colorProperty < 0)
            {
                continue;

            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(colorProperty, new Color(0.32f, 0.32f, 0.32f, 1f));
            renderer.SetPropertyBlock(block);

        }

        enabled = false;

    }

}
