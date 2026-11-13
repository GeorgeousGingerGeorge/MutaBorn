using System;
using System.Collections.Generic;
using UnityEngine;

// Stable IDs for the unit's fourteen real loadout slots.
// RightHand=1 and LeftHand=2 preserve the original serialized IDs.
// Existing EquipmentSlots bit values remain untouched for old equipment assets.
public enum UnitSlotId
{
    None = 0,
    RightHand = 1,
    LeftHand = 2,
    Head = 3,
    Chest = 4,
    Gloves = 5,
    Pants = 6,
    Boots = 7,
    Accessory1 = 8,
    Accessory2 = 9,
    Accessory3 = 10,
    Consumable1 = 11,
    Consumable2 = 12,
    Consumable3 = 13,
    Consumable4 = 14

}

// The family of slot an Equipment Type template may use.
// Auto preserves the old assets until their slot fit is explicitly configured.
public enum EquipmentFitType
{
    Auto = 0,
    Hand = 1,
    Head = 2,
    Chest = 3,
    Gloves = 4,
    Pants = 5,
    Boots = 6,
    Accessory = 7

}

public enum UnitSlotBlockReason
{
    None = 0,
    ProgressionLocked = 1,
    PlayerLocked = 2,
    MutationOccupied = 3,
    TwoHandedWeapon = 4,
    ItemOccupied = 5

}

[Serializable]
public sealed class UnitLoadoutSlotState
{
    [SerializeField] private UnitSlotId slot;
    [SerializeField] private string itemInstanceId;
    [SerializeField] private bool progressionUnlocked;
    [SerializeField] private bool playerLocked;
    [SerializeField] private MutationID occupyingMutation = MutationID.none;

    public UnitSlotId Slot => slot;
    public string ItemInstanceId => itemInstanceId;
    public bool ProgressionUnlocked => progressionUnlocked;
    public bool PlayerLocked => playerLocked;
    public MutationID OccupyingMutation => occupyingMutation;

    public UnitLoadoutSlotState(UnitSlotId id)
    {
        slot = id;
        progressionUnlocked = UnitLoadoutSlots.IsInitiallyUnlocked(id);
        itemInstanceId = string.Empty;

    }

    internal void SetItem(string id)
    {
        itemInstanceId = id ?? string.Empty;

    }

    internal void SetProgressionUnlocked()
    {
        progressionUnlocked = true;

    }

    internal void SetPlayerLock(bool locked)
    {
        playerLocked = locked;

    }

    internal void SetMutation(MutationID id)
    {
        occupyingMutation = id;

    }

}

public static class UnitLoadoutSlots
{
    public static readonly UnitSlotId[] All =
    {
        UnitSlotId.RightHand,
        UnitSlotId.LeftHand,
        UnitSlotId.Head,
        UnitSlotId.Chest,
        UnitSlotId.Gloves,
        UnitSlotId.Pants,
        UnitSlotId.Boots,
        UnitSlotId.Accessory1,
        UnitSlotId.Accessory2,
        UnitSlotId.Accessory3,
        UnitSlotId.Consumable1,
        UnitSlotId.Consumable2,
        UnitSlotId.Consumable3,
        UnitSlotId.Consumable4

    };

    public static bool IsValid(UnitSlotId id)
    {
        return id >= UnitSlotId.RightHand && id <= UnitSlotId.Consumable4;

    }

    public static bool IsHand(UnitSlotId id)
    {
        return id == UnitSlotId.RightHand || id == UnitSlotId.LeftHand;

    }

    public static bool IsConsumable(UnitSlotId id)
    {
        return id >= UnitSlotId.Consumable1 && id <= UnitSlotId.Consumable4;

    }

    public static bool IsAccessory(UnitSlotId id)
    {
        return id >= UnitSlotId.Accessory1 && id <= UnitSlotId.Accessory3;

    }

    public static bool IsInitiallyUnlocked(UnitSlotId id)
    {
        return IsValid(id) && id != UnitSlotId.Accessory2 &&
            id != UnitSlotId.Accessory3 && id != UnitSlotId.Consumable2 &&
            id != UnitSlotId.Consumable3 && id != UnitSlotId.Consumable4;

    }

    public static UnitSlotId OtherHand(UnitSlotId id)
    {
        return id == UnitSlotId.RightHand ? UnitSlotId.LeftHand :
            id == UnitSlotId.LeftHand ? UnitSlotId.RightHand : UnitSlotId.None;

    }

    public static bool Accepts(UnitSlotId slot, EquipmentFitType fit)
    {
        switch (fit)
        {
            case EquipmentFitType.Hand:
                return IsHand(slot);
            case EquipmentFitType.Head:
                return slot == UnitSlotId.Head;
            case EquipmentFitType.Chest:
                return slot == UnitSlotId.Chest;
            case EquipmentFitType.Gloves:
                return slot == UnitSlotId.Gloves;
            case EquipmentFitType.Pants:
                return slot == UnitSlotId.Pants;
            case EquipmentFitType.Boots:
                return slot == UnitSlotId.Boots;
            case EquipmentFitType.Accessory:
                return IsAccessory(slot);
            default:
                return false;

        }

    }

}
