using System;
using System.Collections.Generic;
using UnityEngine;

// Optional temporary component for validating Step 3 before the Barracks UI exists.
// Attach to an ordinary Hub scene object, set a template, then use the context menu.
public sealed class UnitItemTransferTestProbe : MonoBehaviour
{
    [SerializeField, Min(0)] private int rosterDeploymentSlot;
    [SerializeField] private EquipmentData equipmentTemplate;
    [SerializeField] private UnitSlotId equipmentSlot = UnitSlotId.RightHand;
    [SerializeField] private ConsumableData consumableTemplate;
    [SerializeField] private UnitSlotId consumableSlot = UnitSlotId.Consumable1;
    [SerializeField] private UnitSlotId existingEquipmentSlot = UnitSlotId.RightHand;

    [ContextMenu("Debug / Test Equipment Storage Round Trip")]
    private void TestEquipment()
    {
        if (equipmentTemplate == null)
        {
            Debug.LogWarning("Assign an equipment template first.", this);

            return;

        }

        EquipmentInstance generated = EquipmentGenerator.CreateEquipment(equipmentTemplate);
        RunRoundTrip(new StoredItem(generated), equipmentSlot);

    }

    [ContextMenu("Debug / Test Consumable Storage Round Trip")]
    private void TestConsumable()
    {
        if (consumableTemplate == null)
        {
            Debug.LogWarning("Assign a consumable template first.", this);

            return;

        }

        ConsumableInstance generated = ConsumableGenerator.CreateConsumable(consumableTemplate);
        // Distinctive depleted charge value lets the test catch unwanted refills.
        generated.remainingUses = 0;
        RunRoundTrip(new StoredItem(generated), consumableSlot);

    }

    [ContextMenu("Debug / Test Existing Gear Return and Re-Equip")]
    private void TestExistingEquipment()
    {
        UnitRoster roster = UnitRoster.Instance;
        ItemStorage storage = ItemStorage.Instance;
        if (!Application.isPlaying || roster == null || storage == null ||
            !roster.TryGetDeploymentUnit(rosterDeploymentSlot, out UnitData unit) ||
            roster.IsUnitDeployed(unit.UnitId) ||
            !unit.TryGetEquipmentAtSlot(existingEquipmentSlot, out EquipmentInstance original))
        {
            Debug.LogWarning("Return to Hub first, then select an occupied equipment slot " +
                "from a permanent roster unit.", this);

            return;

        }

        string id = original.instanceId;
        if (!UnitItemTransfers.TryReturnToStorage(unit.UnitId, existingEquipmentSlot,
            out string returnError))
        {
            Debug.LogError("Step 3 existing item return failed: " + returnError, this);

            return;

        }

        bool inStorage = storage.TryGetItem(id, out StoredItem stored) &&
            stored.Kind == StoredItemKind.Equipment &&
            EqualityComparer<EquipmentInstance>.Default.Equals(stored.Equipment, original);

        bool reEquipped = UnitItemTransfers.TryEquipFromStorage(unit.UnitId, id,
            existingEquipmentSlot, out string equipError);
        bool restoredExact = reEquipped &&
            unit.TryGetEquipmentAtSlot(existingEquipmentSlot, out EquipmentInstance restored) &&
            EqualityComparer<EquipmentInstance>.Default.Equals(restored, original);

        if (!inStorage || !restoredExact)
        {
            Debug.LogError("Step 3 existing item test FAILED for " + id +
                ". Storage exact=" + inStorage + ", re-equipped exact=" +
                restoredExact + ". " + equipError +
                ". If re-equip failed, the item should remain in ItemStorage.", this);

            return;

        }

        Debug.Log("[Step 3] PASS: Existing " + original.itemName + " [" + id +
            "] returned to storage and re-equipped with its exact stats.", this);

    }

    private void RunRoundTrip(StoredItem original, UnitSlotId slot)
    {
        UnitRoster roster = UnitRoster.Instance;
        ItemStorage storage = ItemStorage.Instance;
        if (!Application.isPlaying || roster == null || storage == null ||
            !roster.TryGetDeploymentUnit(rosterDeploymentSlot, out UnitData unit) ||
            roster.IsUnitDeployed(unit.UnitId))
        {
            Debug.LogWarning("Enter Play Mode from Hub, create a roster unit first, " +
                "return to Hub, and select its deployment slot.", this);

            return;

        }

        if (unit.GetSlotBlockReason(slot) != UnitSlotBlockReason.None)
        {
            Debug.LogWarning("Choose an empty, unlocked slot. Slot " + slot +
                " is " + unit.GetSlotBlockReason(slot) + ".", this);

            return;

        }

        if (!storage.TryStoreTransferredItem(original))
        {
            Debug.LogError("Cannot seed test item in storage: " + original.InstanceId, this);

            return;

        }

        string id = original.InstanceId;
        if (!UnitItemTransfers.TryEquipFromStorage(unit.UnitId, id, slot, out string equipError))
        {
            storage.TryTakeItem(id, out StoredItem failedSeed);
            Debug.LogError("Step 3 equip test failed: " + equipError, this);

            return;

        }

        bool equippedCorrectly = original.Kind == StoredItemKind.Equipment
            ? unit.TryGetEquipmentAtSlot(slot, out EquipmentInstance equipped) &&
              EqualityComparer<EquipmentInstance>.Default.Equals(equipped, original.Equipment)
            : unit.TryGetConsumableAtSlot(slot, out ConsumableInstance carried) &&
              EqualityComparer<ConsumableInstance>.Default.Equals(carried, original.Consumable);

        bool returned = UnitItemTransfers.TryReturnToStorage(unit.UnitId, slot,
            out string returnError);
        bool storedCorrectly = storage.TryGetItem(id, out StoredItem stored) &&
            (original.Kind == StoredItemKind.Equipment
                ? stored.Kind == StoredItemKind.Equipment &&
                  EqualityComparer<EquipmentInstance>.Default.Equals(stored.Equipment, original.Equipment)
                : stored.Kind == StoredItemKind.Consumable &&
                  EqualityComparer<ConsumableInstance>.Default.Equals(stored.Consumable, original.Consumable));

        // Clean up ONLY this freshly generated probe instance; never delete owned gear.
        if (returned)
        {
            storage.TryTakeItem(id, out StoredItem cleaned);

        }

        if (!equippedCorrectly || !returned || !storedCorrectly)
        {
            Debug.LogError("Step 3 round trip FAILED for " + id +
                ". Equip exact=" + equippedCorrectly + ", return=" + returned +
                ", stored exact=" + storedCorrectly + ". " + returnError, this);

            return;

        }

        Debug.Log("[Step 3] PASS: " + original.Kind + " instance " + id +
            " moved Storage -> " + unit.characterName + " -> Storage " +
            "without changing its identity, rolls, or remaining uses.", this);

    }

}
