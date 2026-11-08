using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Temporary mission ownership. The Hub stores are changed only when the result is claimed.
[DisallowMultipleComponent]
[RequireComponent(typeof(MissionObjectiveManager))]
public sealed class MissionSpoils : MonoBehaviour
{
    private sealed class CasualtyItem
    {
        public UnitData unit;
        public UnitSlotId slot;
        public StoredItem item;

    }

    public static MissionSpoils Instance { get; private set; }

    [Header("Battle loot rolls")]
    [SerializeField, Min(0)] private int lootTableRollsPerEnemy = 1;
    [SerializeField, Range(0, 100)] private int enemyEquipmentRecoveryPercent = 5;
    [SerializeField, Range(0, 100)] private int playerEquipmentLossPercent = 25;
    [SerializeField, Range(0, 100)] private int playerConsumableLossPercent = 25;

    private readonly Dictionary<string, int> resources = new(StringComparer.Ordinal);
    private readonly List<StoredItem> items = new();
    private readonly List<CasualtyItem> losses = new();
    private readonly List<CasualtyItem> recovered = new();
    private readonly List<UnitData> casualties = new();
    private readonly HashSet<UnitController> defeatedEnemies = new();
    private readonly HashSet<string> rewardedObjectiveIds = new(StringComparer.OrdinalIgnoreCase);

    private MissionObjectiveManager mission;
    private FactionManager factions;
    private bool prepared;
    private bool claimed;

    public bool IsPrepared => prepared;
    public bool IsClaimed => claimed;
    public IReadOnlyList<UnitData> Casualties => casualties;

    private void Awake()
    {
        Instance = this;
        mission = GetComponent<MissionObjectiveManager>();
        factions = FindAnyObjectByType<FactionManager>();

    }

    private void OnEnable()
    {
        CombatActionExecutor.UnitDefeated += OnUnitDefeated;
        mission.MissionFinished += OnMissionFinished;

    }

    private void OnDisable()
    {
        CombatActionExecutor.UnitDefeated -= OnUnitDefeated;
        mission.MissionFinished -= OnMissionFinished;
        if (Instance == this)
        {
            Instance = null;

        }

    }

    private void OnUnitDefeated(UnitController attacker, UnitController defeated)
    {
        RecordEnemyDefeat(defeated);

    }

    private void OnMissionFinished(MissionStatus status)
    {
        PrepareResult();

    }

    private bool IsEnemy(UnitController unit)
    {
        if (unit == null || unit.data == null || factions == null ||
            mission.LevelMission == null || mission.LevelMission.PlayerFaction == null)
        {

            return false;

        }

        UnitRoster roster = UnitRoster.Instance;
        if (roster != null && roster.TryGetUnit(unit.data.UnitId, out UnitData owned) &&
            ReferenceEquals(owned, unit.data))
        {

            return false;

        }

        UnitFaction allegiance = unit.GetComponent<UnitFaction>();

        return allegiance != null && allegiance.Faction != null &&
            factions.AreHostile(mission.LevelMission.PlayerFaction, allegiance.Faction);

    }

    private void RecordEnemyDefeat(UnitController unit)
    {
        if (prepared || !IsEnemy(unit) || !defeatedEnemies.Add(unit))
        {

            return;

        }

        MissionLootTable table = mission.LevelMission.LootTable;
        if (table == null)
        {

            return;

        }

        for (int i = 0; i < lootTableRollsPerEnemy; i++)
        {
            WorldLootEntry entry = table.Roll();
            if (entry != null && entry.IsValid)
            {
                Collect(new WorldLootStack(entry));

            }

        }

    }

    public void Collect(IReadOnlyList<WorldLootStack> loot)
    {
        if (loot == null || prepared)
        {

            return;

        }

        foreach (WorldLootStack stack in loot)
        {
            Collect(stack);

        }

    }

    private void Collect(WorldLootStack stack)
    {
        if (stack == null || stack.quantity <= 0)
        {

            return;

        }

        if (stack.kind == WorldLootKind.Resource)
        {
            if (string.IsNullOrWhiteSpace(stack.resourceId))
            {

                return;

            }

            resources.TryGetValue(stack.resourceId, out int previous);
            if (previous > int.MaxValue - stack.quantity)
            {
                Debug.LogWarning($"[MissionSpoils] Resource quantity overflow: {stack.resourceId}.", this);

                return;

            }

            resources[stack.resourceId] = previous + stack.quantity;

            return;

        }

        for (int i = 0; i < stack.quantity; i++)
        {
            object generated = ItemGenerator.ItemInstance(stack.item);
            if (generated is EquipmentInstance equipment && equipment.template != null)
            {
                items.Add(new StoredItem(equipment));

            }
            else if (generated is ConsumableInstance consumable && consumable.definition != null)
            {
                items.Add(new StoredItem(consumable));

            }

        }

    }

    public void PrepareResult()
    {
        if (prepared || mission.Status != MissionStatus.Victory &&
            mission.Status != MissionStatus.Defeat)
        {

            return;

        }

        // Dead units are still in the scene, but CombatActionExecutor disabled their GameObjects.
        UnitRoster roster = UnitRoster.Instance;
        foreach (UnitController unit in FindObjectsByType<UnitController>(FindObjectsInactive.Include))
        {
            if (unit == null || unit.data == null || unit.data.currentHealth > 0)
            {

                continue;

            }

            RecordEnemyDefeat(unit);
            if (defeatedEnemies.Contains(unit))
            {
                RollEquipmentRecovery(unit);

            }
            else if (roster != null &&
                roster.TryGetUnit(unit.data.UnitId, out UnitData owned) &&
                ReferenceEquals(owned, unit.data) && !casualties.Contains(owned))
            {
                casualties.Add(owned);
                RollCasualtyEquipment(owned);

            }

        }

        if (mission.LevelMission != null)
        {
            foreach (LevelObjectiveSetup setup in mission.LevelMission.Objectives)
            {
                if (setup == null || setup.Rewards == null)
                {

                    continue;

                }

                foreach (MissionObjectiveEntry objective in mission.Objectives)
                {
                    if (!string.Equals(objective.Id, setup.ObjectiveId?.Trim(),
                        StringComparison.OrdinalIgnoreCase) ||
                        objective.Status != MissionObjectiveStatus.Completed ||
                        !rewardedObjectiveIds.Add(objective.Id))
                    {

                        continue;

                    }

                    foreach (WorldLootEntry reward in setup.Rewards)
                    {
                        if (reward != null && reward.IsValid)
                        {
                            Collect(new WorldLootStack(reward));

                        }

                    }

                }

            }

        }

        prepared = true;

    }

    private void RollEquipmentRecovery(UnitController defeated)
    {
        foreach (UnitSlotId slot in UnitLoadoutSlots.All)
        {
            if (UnityEngine.Random.Range(0, 100) >= enemyEquipmentRecoveryPercent)
            {

                continue;

            }

            if (UnitLoadoutSlots.IsConsumable(slot))
            {
                if (defeated.data.TryGetConsumableAtSlot(slot, out ConsumableInstance consumable) &&
                    consumable.definition != null && defeated.data.TryLoseItemAfterBattle(slot))
                {
                    if (string.IsNullOrWhiteSpace(consumable.instanceId))
                    {
                        consumable.instanceId = Guid.NewGuid().ToString("N");

                    }

                    items.Add(new StoredItem(consumable));

                }

            }
            else if (defeated.data.TryGetEquipmentAtSlot(slot, out EquipmentInstance equipment) &&
                equipment.template != null && defeated.data.TryLoseItemAfterBattle(slot))
            {
                if (string.IsNullOrWhiteSpace(equipment.instanceId))
                {
                    equipment.instanceId = Guid.NewGuid().ToString("N");

                }

                items.Add(new StoredItem(equipment));

            }

        }

    }

    private void RollCasualtyEquipment(UnitData unit)
    {
        foreach (UnitSlotId slot in UnitLoadoutSlots.All)
        {
            StoredItem item = null;
            bool consumableSlot = UnitLoadoutSlots.IsConsumable(slot);
            if (consumableSlot && unit.TryGetConsumableAtSlot(slot, out ConsumableInstance consumable))
            {
                item = new StoredItem(consumable);

            }
            else if (!consumableSlot && unit.TryGetEquipmentAtSlot(slot, out EquipmentInstance equipment))
            {
                item = new StoredItem(equipment);

            }

            if (item == null)
            {

                continue;

            }

            int chance = consumableSlot ? playerConsumableLossPercent : playerEquipmentLossPercent;
            CasualtyItem entry = new() { unit = unit, slot = slot, item = item };
            if (UnityEngine.Random.Range(0, 100) < chance)
            {
                losses.Add(entry);

            }
            else
            {
                recovered.Add(entry);

            }

        }

    }

    public string ResourceSummary()
    {
        if (resources.Count == 0)
        {

            return "No resources collected.";

        }

        StringBuilder text = new();
        ResourceStockpile stockpile = ResourceStockpile.Instance;
        foreach (KeyValuePair<string, int> entry in resources)
        {
            ResourceData definition = stockpile != null ? stockpile.GetResourceData(entry.Key) : null;
            string name = definition != null ? definition.ResourceName : entry.Key;
            text.AppendLine($"+{entry.Value} {name}");

        }

        return text.ToString();

    }

    public string EquipmentSummary()
    {
        StringBuilder text = new();
        foreach (StoredItem item in items)
        {
            text.AppendLine($"+ {item.Definition.itemName}");

        }

        foreach (CasualtyItem recovery in recovered)
        {
            text.AppendLine($"+ {recovery.item.Definition.itemName} (Recovered from {recovery.unit.characterName})");

        }

        foreach (CasualtyItem loss in losses)
        {
            text.AppendLine($"- {loss.item.Definition.itemName} ({loss.unit.characterName})");

        }

        return text.Length == 0 ? "No equipment gained or lost." : text.ToString();

    }

    public string CasualtySummary()
    {
        if (casualties.Count == 0)
        {

            return "No crew casualties.";

        }

        StringBuilder text = new();
        foreach (UnitData unit in casualties)
        {
            text.AppendLine(unit.characterName + " — Permanently Lost");

        }

        return text.ToString();

    }

    private static bool HasMatchingSlot(CasualtyItem item)
    {
        UnitLoadoutSlotState state = item.unit.GetSlot(item.slot);

        return state != null && state.ItemInstanceId == item.item.InstanceId;

    }

    public bool TryClaim()
    {
        if (!prepared || claimed)
        {

            return false;

        }

        ItemStorage storage = ItemStorage.Instance;
        ResourceStockpile stockpile = ResourceStockpile.Instance;
        if (storage == null || stockpile == null)
        {
            Debug.LogError("[MissionSpoils] Claim requires both persistent ItemStorage and ResourceStockpile.", this);

            return false;

        }

        foreach (KeyValuePair<string, int> resource in resources)
        {
            if (stockpile.GetResourceData(resource.Key) == null || resource.Value <= 0 ||
                stockpile.GetQuantity(resource.Key) > int.MaxValue - resource.Value)
            {
                Debug.LogError($"[MissionSpoils] Cannot grant resource '{resource.Key}'.", this);

                return false;

            }

        }

        HashSet<string> incomingIds = new(StringComparer.Ordinal);
        foreach (StoredItem item in items)
        {
            if (!storage.CanStoreTransferredItem(item) || !incomingIds.Add(item.InstanceId))
            {
                Debug.LogError($"[MissionSpoils] Item ID collision or invalid item: '{item.InstanceId}'.", this);

                return false;

            }

        }

        UnitRoster roster = UnitRoster.Instance;
        if (casualties.Count > 0 && roster == null)
        {
            Debug.LogError("[MissionSpoils] Cannot finalize permanent casualties without UnitRoster.", this);

            return false;

        }

        foreach (UnitData unit in casualties)
        {
            if (!roster.TryGetUnit(unit.UnitId, out UnitData owned) || !ReferenceEquals(owned, unit))
            {
                Debug.LogError("[MissionSpoils] Casualty roster membership changed before claim.", this);

                return false;

            }

        }

        foreach (CasualtyItem recoveredItem in recovered)
        {
            StoredItem item = recoveredItem.item;
            if (string.IsNullOrWhiteSpace(item.InstanceId) || item.Definition == null ||
                !incomingIds.Add(item.InstanceId) || storage.TryGetItem(item.InstanceId, out _))
            {
                Debug.LogError($"[MissionSpoils] Cannot recover casualty item '{item.InstanceId}'.", this);

                return false;

            }

        }

        foreach (CasualtyItem casualtyItem in losses)
        {
            if (!HasMatchingSlot(casualtyItem))
            {
                Debug.LogError("[MissionSpoils] Lost-item loadout changed before claim.", this);

                return false;

            }

        }

        foreach (CasualtyItem casualtyItem in recovered)
        {
            if (!HasMatchingSlot(casualtyItem))
            {
                Debug.LogError("[MissionSpoils] Recovered-item loadout changed before claim.", this);

                return false;

            }

        }

        foreach (CasualtyItem loss in losses)
        {
            if (!loss.unit.TryLoseItemAfterBattle(loss.slot))
            {

                return false;

            }

        }

        foreach (CasualtyItem recovery in recovered)
        {
            if (!recovery.unit.TryLoseItemAfterBattle(recovery.slot) ||
                !storage.TryStoreTransferredItem(recovery.item))
            {
                Debug.LogError($"[MissionSpoils] Failed to recover '{recovery.item.InstanceId}'.", this);

                return false;

            }

        }

        foreach (StoredItem item in items)
        {
            if (!storage.TryStoreTransferredItem(item))
            {
                Debug.LogError($"[MissionSpoils] Failed to transfer '{item.InstanceId}'. Claim was interrupted.", this);

                return false;

            }

        }

        foreach (UnitData casualty in casualties)
        {
            if (!roster.TryRemoveFallenUnit(casualty.UnitId))
            {
                Debug.LogError($"[MissionSpoils] Failed to remove fallen crew '{casualty.characterName}'.", this);

                return false;

            }

        }

        foreach (KeyValuePair<string, int> resource in resources)
        {
            stockpile.AddResource(resource.Key, resource.Value);
            stockpile.DiscoverResource(resource.Key);

        }

        storage.RefillStoredConsumables();
        if (roster != null)
        {
            roster.RefreshAfterMission();

        }

        claimed = true;

        return true;

    }

}
