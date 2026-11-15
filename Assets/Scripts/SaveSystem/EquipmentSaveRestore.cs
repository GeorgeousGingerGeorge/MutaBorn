
using MutaBorn.Gameplay;
using System;
using UnityEngine;

public static class EquipmentSaveRestore
{
    public static bool TryReconstructCore(EquipmentSaveData saved, out EquipmentInstance restored)
    {
        restored = default;

        if (saved == null || string.IsNullOrWhiteSpace(saved.instanceId))
        {
            return false;
        }

        GameDataBase database = GameDataBaseLoader.Instance;
        EquipmentType type = database != null ? database.GetEquipmentType(saved.typeId) : null;

        if (type == null || !type.TryGetTemplate(saved.templateId, out EquipmentTemplate template))
        {
            Debug.LogError($"[Save/Load] Equipment definition not found: {saved.typeId}/{saved.templateId}.");
            return false;
        }

        restored = new EquipmentInstance
        {
            instanceId = saved.instanceId,
            itemID = $"{type.TypeId}:{template.TemplateId}",
            itemName = template.DisplayName,
            typeDefinition = type,
            templateId = template.TemplateId,

            armor = type.GetArmorWeight(template),
            weapon = type.WeaponCategory,
            accessory = type.AccessoryCategory,
            type = template.GenerationType,
            rarity = saved.rarity,

            baseStats = saved.baseStats,
            rolledStats = saved.rolledStats,
            attributeScaling = saved.attributeScaling,

            resistances = saved.resistances != null ? (DamageResistance[])saved.resistances.Clone() : Array.Empty<DamageResistance>(),
            bonusDamage = saved.bonusDamage != null ? (DamageComponent[])saved.bonusDamage.Clone() : Array.Empty<DamageComponent>(),

            lastStatRoll1 = saved.lastStatRoll1,
            lastStatRoll2 = saved.lastStatRoll2,
            bonusStacks = saved.bonusStacks != null ? (EquipmentBonusStack[])saved.bonusStacks.Clone() : Array.Empty<EquipmentBonusStack>(),
            lastUpgradeBonus = saved.lastUpgradeBonus
        };

        return restored.IsValid;
    }

    public static bool TryRestoreEquippedItem(EquipmentSaveData saved, UnitData unit)
    {
        if (saved == null || unit == null || saved.equippedSlot == UnitSlotId.None)
        {
            Debug.LogError("[Save/Load] Invalid equipment restoration request.");
            return false;
        }

        if (!TryReconstructCore(saved, out EquipmentInstance restored))
        {
            return false;
        }

        if (!unit.TryEquipInstance(restored, saved.equippedSlot))
        {
            Debug.LogError($"[Save/Load] Failed to equip {saved.instanceId} into {saved.equippedSlot}.");
            return false;
        }

        UnitLoadoutSlotState slot = unit.GetSlot(saved.equippedSlot);

        if (slot == null || slot.ItemInstanceId != saved.instanceId)
        {
            Debug.LogError($"[Save/Load] Equipment slot verification failed: {saved.instanceId}.");
            return false;
        }

        return true;
    }

}
