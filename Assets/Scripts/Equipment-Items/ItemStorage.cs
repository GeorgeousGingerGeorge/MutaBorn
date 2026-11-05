using System;
using System.Collections.Generic;
using UnityEngine;

public enum StoredItemKind
{
    Equipment,
    Consumable

}

// One individually owned item. Consumables have rechargeable battle uses, not disposable stacks.
[Serializable]
public sealed class StoredItem
{
    [SerializeField] private StoredItemKind kind;
    [SerializeField] private EquipmentInstance equipment;
    [SerializeField] private ConsumableInstance consumable;

    public StoredItemKind Kind => kind;
    public string InstanceId => kind == StoredItemKind.Equipment
        ? equipment.instanceId : consumable.instanceId;
    public ItemData Definition => kind == StoredItemKind.Equipment
        ? (ItemData)equipment.template : consumable.definition;
    public EquipmentInstance Equipment => equipment;
    public ConsumableInstance Consumable => consumable;

    public StoredItem(EquipmentInstance item)
    {
        kind = StoredItemKind.Equipment;
        equipment = item;

    }

    public StoredItem(ConsumableInstance item)
    {
        kind = StoredItemKind.Consumable;
        consumable = item;

    }

    internal bool RefillConsumableUses()
    {
        if (kind != StoredItemKind.Consumable || consumable.definition == null ||
            consumable.remainingUses == consumable.definition.MaxUses)
        {
            return false;

        }

        consumable.remainingUses = consumable.definition.MaxUses;

        return true;

    }

}

public class ItemStorage : MonoBehaviour
{
    public static ItemStorage Instance { get; private set; }

    [Header("Owned equipment and consumables")]
    [SerializeField] private List<StoredItem> items = new List<StoredItem>();

    [Header("Separate mutation vault (unlocked through gameplay)")]
    [SerializeField] private bool mutationStorageUnlocked;
    [SerializeField] private List<ActiveMutationRecord> mutations = new List<ActiveMutationRecord>();

    public IReadOnlyList<StoredItem> Items => items;
    public IReadOnlyList<ActiveMutationRecord> Mutations => mutations;
    public bool MutationStorageUnlocked => mutationStorageUnlocked;

    public event Action StorageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);

            return;

        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;

        }

    }

    private int FindItemIndex(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return -1;

        }

        for (int i = 0; i < items.Count; i++)
        {
            StoredItem stored = items[i];
            if (stored != null && string.Equals(stored.InstanceId, instanceId, StringComparison.Ordinal))
            {
                return i;

            }

        }

        return -1;

    }

    // Only validates and prepares ownership data; crafting/upgrade rules belong elsewhere.
    private static bool TryPrepareItem(StoredItem source, bool refillUses,
        out StoredItem prepared)
    {
        prepared = null;
        if (source == null)
        {
            return false;

        }

        if (source.Kind == StoredItemKind.Equipment)
        {
            EquipmentInstance equipment = source.Equipment;
            if (equipment.template == null)
            {
                return false;

            }

            if (string.IsNullOrWhiteSpace(equipment.instanceId))
            {
                equipment.instanceId = Guid.NewGuid().ToString("N");

            }

            prepared = new StoredItem(equipment);

            return true;

        }

        if (source.Kind == StoredItemKind.Consumable)
        {
            ConsumableInstance consumable = source.Consumable;
            if (consumable.definition == null)
            {
                return false;

            }

            if (string.IsNullOrWhiteSpace(consumable.instanceId))
            {
                consumable.instanceId = Guid.NewGuid().ToString("N");

            }

            consumable.remainingUses = refillUses
                ? consumable.definition.MaxUses
                : Mathf.Clamp(consumable.remainingUses, 0, consumable.definition.MaxUses);
            prepared = new StoredItem(consumable);

            return true;

        }

        return false;

    }

    // Storage only adds an item. It does not create, craft, upgrade, or recycle it.
    public bool TryStoreItem(StoredItem item)
    {
        if (!TryPrepareItem(item, true, out StoredItem prepared) ||
            FindItemIndex(prepared.InstanceId) >= 0)
        {
            return false;

        }

        items.Add(prepared);
        StorageChanged?.Invoke();

        return true;

    }

    public bool TryStoreEquipment(EquipmentInstance item)
    {
        return TryStoreItem(new StoredItem(item));

    }

    public bool TryStoreConsumable(ConsumableInstance item)
    {
        return TryStoreItem(new StoredItem(item));

    }

    public bool TryGetItem(string instanceId, out StoredItem item)
    {
        int index = FindItemIndex(instanceId);
        if (index >= 0)
        {
            item = items[index];

            return true;

        }

        item = null;

        return false;

    }

    // The single removal operation. Caller decides whether to equip, discard, recycle, or craft.
    public bool TryTakeItem(string instanceId, out StoredItem item)
    {
        int index = FindItemIndex(instanceId);
        if (index >= 0)
        {
            item = items[index];
            items.RemoveAt(index);
            StorageChanged?.Invoke();

            return true;

        }

        item = null;

        return false;

    }

    // Future Workshop can commit changed stats to the same owned item, keeping its identity.
    // This method does not perform an upgrade, check costs, or spend ingredients.
    public bool TryReplaceItem(string instanceId, StoredItem replacement)
    {
        int index = FindItemIndex(instanceId);
        if (index < 0 || replacement == null ||
            !string.Equals(replacement.InstanceId, instanceId, StringComparison.Ordinal) ||
            items[index].Kind != replacement.Kind)
        {
            return false;

        }

        if (!TryPrepareItem(replacement, false, out StoredItem prepared))
        {
            return false;

        }

        items[index] = prepared;
        StorageChanged?.Invoke();

        return true;

    }

    public int CountItems(ItemData definition)
    {
        if (definition == null)
        {
            return 0;

        }

        int count = 0;
        foreach (StoredItem item in items)
        {
            if (item != null && item.Definition == definition)
            {
                count++;

            }

        }

        return count;

    }

    // Base-return flow will call this for stored copies; equipped copies stay on UnitData.
    public void RefillStoredConsumables()
    {
        bool changed = false;
        foreach (StoredItem item in items)
        {
            if (item != null && item.RefillConsumableUses())
            {
                changed = true;

            }

        }

        if (changed)
        {
            StorageChanged?.Invoke();

        }

    }

    // Mutation storage is deliberately separate from regular item ownership.
    public void SetMutationStorageUnlocked(bool unlocked)
    {
        if (mutationStorageUnlocked == unlocked)
        {
            return;

        }

        mutationStorageUnlocked = unlocked;
        StorageChanged?.Invoke();

    }

    public bool TryStoreMutation(ActiveMutationRecord record)
    {
        if (!mutationStorageUnlocked || record == null || record.MutationId == MutationID.none)
        {
            return false;

        }

        ActiveMutationRecord copy = new ActiveMutationRecord(record.MutationId);
        copy.AddProgress(record.ProgressPoints);
        mutations.Add(copy);
        StorageChanged?.Invoke();

        return true;

    }

    public bool TryTakeMutationAt(int index, out ActiveMutationRecord record)
    {
        if (!mutationStorageUnlocked || index < 0 || index >= mutations.Count ||
            mutations[index] == null)
        {
            record = null;

            return false;

        }

        ActiveMutationRecord selected = mutations[index];
        record = new ActiveMutationRecord(selected.MutationId);
        record.AddProgress(selected.ProgressPoints);
        mutations.RemoveAt(index);
        StorageChanged?.Invoke();

        return true;

    }

}
