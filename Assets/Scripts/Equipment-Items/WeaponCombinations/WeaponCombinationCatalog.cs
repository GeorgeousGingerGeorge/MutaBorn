using System;
using UnityEngine;

[Serializable]
public class WeaponCombinationEntry
{
    public string name;
    public WeaponSubtype first;
    public WeaponSubtype second;
    public ActionDefinition grantedAction;
    [TextArea(2, 3)] public string plannedPassive;

}

[CreateAssetMenu(fileName = "WeaponCombinations", menuName = "Game/Equipment/Weapon Combinations")]
public class WeaponCombinationCatalog : ScriptableObject
{
    [SerializeField] private WeaponCombinationEntry[] combinations = Array.Empty<WeaponCombinationEntry>();

    public WeaponCombinationEntry GetActive(UnitData data)
    {
        if (data == null || !TryGetHandSubtype(data, UnitSlotId.LeftHand, out WeaponSubtype left) ||
            !TryGetHandSubtype(data, UnitSlotId.RightHand, out WeaponSubtype right))
        {
            return null;

        }

        foreach (WeaponCombinationEntry entry in combinations)
        {
            if (entry != null &&
                ((entry.first == left && entry.second == right) ||
                 (entry.first == right && entry.second == left)))
            {
                return entry;

            }

        }

        return null;

    }

    private static bool TryGetHandSubtype(UnitData data, UnitSlotId slot, out WeaponSubtype subtype)
    {
        subtype = WeaponSubtype.None;
        UnitLoadoutSlotState equippedSlot = data.GetSlot(slot);
        if (equippedSlot == null || string.IsNullOrEmpty(equippedSlot.ItemInstanceId))
        {
            return false;

        }

        foreach (EquipmentInstance item in data.equippedItems)
        {
            if (item.instanceId != equippedSlot.ItemInstanceId || item.template == null ||
                item.template.RequiresBothHands || item.template.EquipmentFit != EquipmentFitType.Hand)
            {
                continue;

            }

            subtype = item.template.WeaponSubtypeId;
            return subtype != WeaponSubtype.None;

        }

        return false;

    }

}
