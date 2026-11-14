using System;
using UnityEngine;
using System.Collections.Generic;


// Coordinates exact-item ownership changes between base storage and a roster unit.
// No new instances, no extra inventory list, no combat-side swaps, no automatic refills.
public static class UnitItemTransfers
{
    public static bool TryEquipFromStorage(string unitId, string instanceId, UnitSlotId slot,
        out string error)
    {
        if (!TryGetEditableUnit(unitId, out UnitData unit, out ItemStorage storage, out error))
        {
            return false;

        }

        if (string.IsNullOrWhiteSpace(instanceId) ||
            !storage.TryGetItem(instanceId, out StoredItem selected) || selected == null ||
            !string.Equals(selected.InstanceId, instanceId, StringComparison.Ordinal))
        {
            error = "Item instance is missing from ItemStorage.";

            return false;

        }

        if (IsOwnedByAnyRosterUnit(instanceId))
        {
            error = "An existing roster unit already owns this item instance.";

            return false;

        }

        if (selected.Kind == StoredItemKind.Equipment)
        {
            if (!unit.CanEquipInstance(selected.Equipment, slot))
            {
                error = "Equipment cannot fit that unit slot (" +
                    unit.GetSlotBlockReason(slot) + ").";

                return false;

            }

        }
        else if (selected.Kind == StoredItemKind.Consumable)
        {
            if (selected.Consumable.definition == null ||
                !UnitLoadoutSlots.IsConsumable(slot) ||
                unit.GetSlotBlockReason(slot) != UnitSlotBlockReason.None ||
                unit.carriedConsumables.Count >= UnitData.ConsumableCapacity)
            {
                error = "Consumable slot is unavailable, occupied, or incompatible.";

                return false;

            }

        }
        else
        {
            error = "Unsupported item kind.";

            return false;

        }

        if (!storage.TryTakeItem(instanceId, out StoredItem taken) || taken == null)
        {
            error = "Could not take the exact item from ItemStorage.";

            return false;

        }

        bool equipped = taken.Kind == StoredItemKind.Equipment
            ? unit.TryEquipInstance(taken.Equipment, slot)
            : unit.TryAssignConsumable(taken.Consumable, slot);
        if (!equipped)
        {
            if (!storage.TryStoreTransferredItem(taken))
            {
                Debug.LogError("[UnitItemTransfers] CRITICAL: Failed to restore " +
                    instanceId + " after unit equip rejected it. Item is held in a " +
                    "failed transfer. Inspect immediately; do not continue playing.");

                error = "Equip rejected and storage rollback failed. See Console.";

                return false;

            }

            error = "Unit rejected the item; it was returned to ItemStorage.";

            return false;

        }

        error = null;

        return true;

    }

    public static bool TryReturnToStorage(string unitId, UnitSlotId slot, out string error)
    {
        if (!TryGetEditableUnit(unitId, out UnitData unit, out ItemStorage storage, out error))
        {
            return false;

        }

        UnitLoadoutSlotState state = unit.GetSlot(slot);
        if (state == null || !state.ProgressionUnlocked || state.PlayerLocked ||
            state.OccupyingMutation != MutationID.none ||
            string.IsNullOrWhiteSpace(state.ItemInstanceId))
        {
            error = "That slot is empty or cannot be modified.";

            return false;

        }

        bool isConsumable = UnitLoadoutSlots.IsConsumable(slot);
        StoredItem original;
        if (isConsumable)
        {
            if (!unit.TryGetConsumableAtSlot(slot, out ConsumableInstance consumable))
            {
                error = "Consumable slot does not resolve to an owned instance.";

                return false;

            }

            original = new StoredItem(consumable);

        }
        else
        {
            if (!unit.TryGetEquipmentAtSlot(slot, out EquipmentInstance equipment))
            {
                error = "Equipment slot does not resolve to an owned instance.";

                return false;

            }

            original = new StoredItem(equipment);

        }

        if (string.IsNullOrWhiteSpace(original.InstanceId) || !original.IsValid ||
            IsOwnedByAnotherUnit(original.InstanceId, unit))
        {
            error = "Missing item identity/definition or duplicate ownership across roster units.";

            return false;

        }

        // The currently owning unit is expected to have this item, so do not use
        // CanStoreTransferredItem until AFTER the unit has released it.
        if (storage.TryGetItem(original.InstanceId, out StoredItem duplicate))
        {
            error = "ItemStorage already contains this exact instance ID.";

            return false;

        }

        bool removed;
        if (isConsumable)
        {
            removed = unit.TryRemoveConsumable(slot, out ConsumableInstance removedConsumable);

        }
        else
        {
            removed = unit.TryUnequipInstance(slot, out EquipmentInstance removedEquipment);

        }
        if (!removed)
        {
            error = "The unit rejected removal from this slot.";

            return false;

        }

        if (!storage.TryStoreTransferredItem(original))
        {
            bool restored = isConsumable
                ? unit.TryAssignConsumable(original.Consumable, slot)
                : unit.TryEquipInstance(original.Equipment, slot);
            if (!restored)
            {
                Debug.LogError("[UnitItemTransfers] CRITICAL: Failed to restore " +
                    original.InstanceId + " to its original unit after storage rejected it.");

                error = "Storage rejected item and unit rollback failed. See Console.";

                return false;

            }

            error = "ItemStorage rejected the transfer; original unit loadout restored.";

            return false;

        }

        error = null;

        return true;

    }

    // The exact original items and their slots are kept until a swap commits.
    private sealed class DisplacedItem
    {
        public UnitSlotId Slot;
        public StoredItem Item;

        public DisplacedItem(UnitSlotId slot, StoredItem item)
        {
            Slot = slot;
            Item = item;

        }

    }

    // Read-only compatibility check for the Barracks picker. Allows occupied slots
    // and predicts additional displacement when a two-handed item is involved.
    public static bool CanSwapWithStorage(string unitId, string incomingId,
        UnitSlotId slot, out string error)
    {
        return TryPrepareSwap(unitId, incomingId, slot,
            out UnitData unit, out ItemStorage storage, out StoredItem incoming,
            out List<DisplacedItem> displaced, out error);

    }

    public static bool TrySwapWithStorage(string unitId, string incomingId,
        UnitSlotId slot, out string error)
    {
        if (!TryPrepareSwap(unitId, incomingId, slot,
            out UnitData unit, out ItemStorage storage, out StoredItem incoming,
            out List<DisplacedItem> displaced, out error))
        {
            return false;

        }

        if (!storage.TryTakeItem(incomingId, out StoredItem taken) || taken == null)
        {
            error = "Unable to take replacement item from storage.";

            return false;

        }

        List<DisplacedItem> removed = new List<DisplacedItem>();
        List<DisplacedItem> stored = new List<DisplacedItem>();
        bool incomingEquipped = false;
        string failure = null;

        foreach (DisplacedItem entry in displaced)
        {
            bool success;
            string actualId;
            if (entry.Item.Kind == StoredItemKind.Consumable)
            {
                success = unit.TryRemoveConsumable(entry.Slot, out ConsumableInstance old);
                actualId = old.instanceId;

            }
            else
            {
                success = unit.TryUnequipInstance(entry.Slot, out EquipmentInstance old);
                actualId = old.instanceId;

            }

            if (!success || !string.Equals(actualId, entry.Item.InstanceId,
                StringComparison.Ordinal))
            {
                // If an unexpected instance was removed, stop: never silently
                // commit a transfer against a changed inventory.
                if (success)
                {
                    removed.Add(entry);

                }

                failure = "Failed to remove the expected original equipment.";
                break;

            }

            removed.Add(entry);

        }

        if (failure == null)
        {
            incomingEquipped = taken.Kind == StoredItemKind.Equipment
                ? unit.TryEquipInstance(taken.Equipment, slot)
                : unit.TryAssignConsumable(taken.Consumable, slot);
            if (!incomingEquipped)
            {
                failure = "Unit rejected replacement after clearing required slots.";

            }

        }

        if (failure == null)
        {
            foreach (DisplacedItem entry in removed)
            {
                if (!storage.TryStoreTransferredItem(entry.Item))
                {
                    failure = "Storage rejected one of the displaced original items.";
                    break;

                }

                stored.Add(entry);

            }

        }

        if (failure == null)
        {
            error = null;

            return true;

        }

        // Roll back in reverse ownership order. New item never loses its ID.
        bool rollbackOK = true;
        if (incomingEquipped)
        {
            bool released;
            string releasedId;
            if (taken.Kind == StoredItemKind.Equipment)
            {
                released = unit.TryUnequipInstance(slot, out EquipmentInstance item);
                releasedId = item.instanceId;

            }
            else
            {
                released = unit.TryRemoveConsumable(slot, out ConsumableInstance item);
                releasedId = item.instanceId;

            }

            rollbackOK &= released && releasedId == incomingId;

        }

        foreach (DisplacedItem entry in stored)
        {
            rollbackOK &= storage.TryTakeItem(entry.Item.InstanceId, out StoredItem reclaimed) &&
                reclaimed != null && reclaimed.InstanceId == entry.Item.InstanceId;

        }

        foreach (DisplacedItem entry in removed)
        {
            rollbackOK &= RestoreOriginal(unit, entry.Slot, entry.Item);

        }

        rollbackOK &= storage.TryStoreTransferredItem(taken);
        if (!rollbackOK)
        {
            Debug.LogError("[UnitItemTransfers] CRITICAL: Two-hand swap rollback incomplete. " +
                "Stop playing and inspect inventory ownership. Do not save this state.");

        }

        error = failure + (rollbackOK ? " Original instances restored." :
            " CRITICAL rollback failure; see Console.");

        return false;

    }

    private static bool TryPrepareSwap(string unitId, string incomingId, UnitSlotId slot,
        out UnitData unit, out ItemStorage storage, out StoredItem incoming,
        out List<DisplacedItem> displaced, out string error)
    {
        incoming = null;
        displaced = new List<DisplacedItem>();
        if (!TryGetEditableUnit(unitId, out unit, out storage, out error))
        {
            return false;

        }

        if (string.IsNullOrWhiteSpace(incomingId) ||
            !storage.TryGetItem(incomingId, out incoming) || incoming == null ||
            !incoming.IsValid || IsOwnedByAnyRosterUnit(incomingId))
        {
            error = "Stored item is missing, invalid or already owned by a unit.";

            return false;

        }

        UnitLoadoutSlotState state = unit.GetSlot(slot);
        if (!CanModifySlot(state))
        {
            error = "The selected slot is progression-, player- or mutation-locked.";

            return false;

        }

        bool isConsumable = UnitLoadoutSlots.IsConsumable(slot);
        if (incoming.Kind == StoredItemKind.Equipment)
        {
            if (!incoming.Equipment.IsValid || isConsumable ||
                !UnitLoadoutSlots.Accepts(slot, incoming.Equipment.EquipmentFit))
            {
                error = "That equipment type cannot fit this slot.";

                return false;

            }

        }
        else if (incoming.Kind == StoredItemKind.Consumable)
        {
            if (!isConsumable || incoming.Consumable.definition == null)
            {
                error = "This item requires an unlocked consumable slot.";

                return false;

            }

        }
        else
        {
            error = "Unsupported stored item type.";

            return false;

        }

        if (!AddDisplaced(unit, storage, slot, displaced, out error))
        {
            return false;

        }

        if (UnitLoadoutSlots.IsHand(slot) && incoming.Kind == StoredItemKind.Equipment)
        {
            // If the selected hand is blocked by an existing two-handed item,
            // that existing item must return to storage first. If the incoming
            // item itself is two-handed, it must clear the opposite hand.
            bool clearOther = incoming.Equipment.RequiresBothHands ||
                unit.GetSlotBlockReason(slot) == UnitSlotBlockReason.TwoHandedWeapon;
            if (clearOther)
            {
                UnitSlotId other = UnitLoadoutSlots.OtherHand(slot);
                UnitLoadoutSlotState otherState = unit.GetSlot(other);
                if (!CanModifySlot(otherState))
                {
                    error = "The opposite hand is locked; cannot clear it for this swap.";

                    return false;

                }

                if (!AddDisplaced(unit, storage, other, displaced, out error))
                {
                    return false;

                }

            }

        }

        int releasingConsumables = 0;
        foreach (DisplacedItem entry in displaced)
        {
            if (entry.Item.Kind == StoredItemKind.Consumable)
            {
                releasingConsumables++;

            }

        }

        if (isConsumable && unit.carriedConsumables.Count - releasingConsumables >=
            UnitData.ConsumableCapacity)
        {
            error = "The unit has no available consumable capacity.";

            return false;

        }

        error = null;

        return true;

    }

    private static bool CanModifySlot(UnitLoadoutSlotState state)
    {
        return state != null && state.ProgressionUnlocked && !state.PlayerLocked &&
            state.OccupyingMutation == MutationID.none;

    }

    private static bool AddDisplaced(UnitData unit, ItemStorage storage,
        UnitSlotId slot, List<DisplacedItem> displaced, out string error)
    {
        error = null;
        UnitLoadoutSlotState state = unit.GetSlot(slot);
        if (state == null || string.IsNullOrWhiteSpace(state.ItemInstanceId))
        {
            return true;

        }

        StoredItem item;
        if (UnitLoadoutSlots.IsConsumable(slot))
        {
            if (!unit.TryGetConsumableAtSlot(slot, out ConsumableInstance consumable))
            {
                error = "Stored consumable reference does not resolve.";

                return false;

            }

            item = new StoredItem(consumable);

        }
        else
        {
            if (!unit.TryGetEquipmentAtSlot(slot, out EquipmentInstance gear))
            {
                error = "Equipped gear reference does not resolve.";

                return false;

            }

            item = new StoredItem(gear);

        }

        if (string.IsNullOrWhiteSpace(item.InstanceId) || !item.IsValid ||
            storage.TryGetItem(item.InstanceId, out StoredItem duplicate) ||
            IsOwnedByAnotherUnit(item.InstanceId, unit))
        {
            error = "Displaced item has an invalid ID or duplicate ownership.";

            return false;

        }

        foreach (DisplacedItem previous in displaced)
        {
            if (string.Equals(previous.Item.InstanceId, item.InstanceId,
                StringComparison.Ordinal))
            {
                error = "The same item instance appears in two loadout slots.";

                return false;

            }

        }

        displaced.Add(new DisplacedItem(slot, item));

        return true;

    }

    private static bool RestoreOriginal(UnitData unit, UnitSlotId slot, StoredItem original)
    {
        bool restored = original.Kind == StoredItemKind.Consumable
            ? unit.TryAssignConsumable(original.Consumable, slot)
            : unit.TryEquipInstance(original.Equipment, slot);
        if (!restored)
        {
            Debug.LogError("[UnitItemTransfers] CRITICAL: Original instance " +
                original.InstanceId + " could not be returned to its unit.");

        }

        return restored;

    }

    private static bool TryGetEditableUnit(string unitId, out UnitData unit,
        out ItemStorage storage, out string error)
    {
        unit = null;
        storage = ItemStorage.Instance;
        UnitRoster roster = UnitRoster.Instance;
        if (roster == null || storage == null || string.IsNullOrWhiteSpace(unitId) ||
            !roster.TryGetUnit(unitId, out unit) || unit == null)
        {
            error = "UnitRoster, ItemStorage, or target roster unit is missing.";

            return false;

        }

        if (roster.IsUnitDeployed(unitId))
        {
            error = "Cannot edit deployed units; use Barracks while the unit is at base.";

            return false;

        }

        error = null;

        return true;

    }

    private static bool IsOwnedByAnyRosterUnit(string instanceId)
    {
        return IsOwnedByAnotherUnit(instanceId, null);

    }

    private static bool IsOwnedByAnotherUnit(string instanceId, UnitData exclude)
    {
        UnitRoster roster = UnitRoster.Instance;
        if (roster == null)
        {
            return false;

        }

        foreach (UnitData unit in roster.Units)
        {
            if (unit == null || ReferenceEquals(unit, exclude))
            {
                continue;

            }

            foreach (EquipmentInstance equipment in unit.equippedItems)
            {
                if (string.Equals(equipment.instanceId, instanceId, StringComparison.Ordinal))
                {
                    return true;

                }

            }

            foreach (ConsumableInstance consumable in unit.carriedConsumables)
            {
                if (string.Equals(consumable.instanceId, instanceId, StringComparison.Ordinal))
                {
                    return true;

                }

            }

        }

        return false;

    }

}
