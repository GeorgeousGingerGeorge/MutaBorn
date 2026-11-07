using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MutaBorn.Gameplay;

[Serializable]
public sealed class WorkshopUpgradeCosts
{
    [Tooltip("Resource IDs from your existing ResourceDatabase. Leave blank to disable that cost.")]
    public string currencyId = "gold";
    [Min(0)] public int goldPerNextStar = 20;
    public string materialId = "steel";
    [Min(0)] public int materialPerNextStar = 1;
}

// Upgrades only individual EQUIPMENT instances. Consumables do not have star rarity.
// ItemStorage, UnitData and ResourceStockpile remain the only owners of game state.
public static class WorkshopUpgradeService
{
    public static bool CanUpgrade(EquipmentInstance item, ResourceStockpile stockpile,
        WorkshopUpgradeCosts rules, out string reason)
    {
        reason = string.Empty;
        if (item.template == null || string.IsNullOrWhiteSpace(item.instanceId))
        {
            reason = "This equipment instance is not valid.";
            return false;
        }

        if (!EquipmentGenerator.HasEligibleFlatStat(item))
        {
            reason = "This equipment has no eligible nonzero flat stats to improve.";
            return false;
        }

        if (item.rarity >= EquipmentGrade.FiveStar)
        {
            reason = "This equipment already has five stars.";
            return false;
        }

        if (stockpile == null || rules == null)
        {
            reason = "Resource stockpile or upgrade settings are missing.";
            return false;
        }

        return TryGetCost(item, stockpile, rules, out _, out reason);
    }

    public static string CostDescription(EquipmentInstance item, ResourceStockpile stockpile,
        WorkshopUpgradeCosts rules)
    {
        if (item.template == null) return "No equipment selected.";
        if (item.rarity >= EquipmentGrade.FiveStar) return "Maximum stars reached.";
        if (stockpile == null || rules == null) return "Resource stockpile missing.";
        if (!BuildCosts(item, stockpile, rules, out List<KeyValuePair<string, int>> costs, out string error))
            return error;

        if (costs.Count == 0) return "No resource cost (testing).";
        StringBuilder result = new StringBuilder();
        foreach (KeyValuePair<string, int> entry in costs)
        {
            ResourceData definition = stockpile.GetResourceData(entry.Key);
            result.Append(definition != null ? definition.ResourceName : entry.Key)
                .Append(": ").Append(stockpile.GetQuantity(entry.Key)).Append(" / ")
                .Append(entry.Value).AppendLine();
        }
        return result.ToString().TrimEnd();
    }

    public static bool TryUpgradeStored(string instanceId, ResourceStockpile stockpile,
        ItemStorage storage, WorkshopUpgradeCosts rules, out string result)
    {
        result = string.Empty;
        if (storage == null || !storage.TryGetItem(instanceId, out StoredItem stored) ||
            stored.Kind != StoredItemKind.Equipment)
        {
            result = "Equipment was not found in storage.";
            return false;
        }

        EquipmentInstance before = stored.Equipment;
        if (!CanUpgrade(before, stockpile, rules, out result) ||
            !TryGetCost(before, stockpile, rules, out List<KeyValuePair<string, int>> costs, out result))
            return false;

        EquipmentInstance after = before;
        if (!EquipmentGenerator.TryUpgrade(ref after))
        {
            result = "No eligible nonzero flat stats were found for this item.";
            return false;
        }

        if (!Spend(stockpile, costs, out result)) return false;
        if (!storage.TryReplaceItem(instanceId, new StoredItem(after)))
        {
            Refund(stockpile, costs);
            result = "Storage changed during upgrade; resources were refunded.";
            return false;
        }

        result = DescribeRoll(before, after);
        return true;
    }

    public static bool TryUpgradeEquipped(UnitData owner, string instanceId, ResourceStockpile stockpile,
        WorkshopUpgradeCosts rules, out string result)
    {
        result = string.Empty;
        UnitRoster roster = UnitRoster.Instance;
        if (roster == null || owner == null || !roster.TryGetUnit(owner.UnitId, out UnitData owned) ||
            !ReferenceEquals(owner, owned) || roster.IsUnitDeployed(owner.UnitId))
        {
            result = "Only non-deployed roster equipment can be upgraded in the Hub.";
            return false;
        }

        int index = -1;
        for (int i = 0; i < owner.equippedItems.Count; i++)
        {
            if (string.Equals(owner.equippedItems[i].instanceId, instanceId, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            result = "The equipped item is no longer on this unit.";
            return false;
        }

        EquipmentInstance before = owner.equippedItems[index];
        if (!CanUpgrade(before, stockpile, rules, out result) ||
            !TryGetCost(before, stockpile, rules, out List<KeyValuePair<string, int>> costs, out result))
            return false;

        // Validate that a reward exists before deducting resources. Actual commit
        // goes through UnitData so derived stats and LoadoutChanged are updated.
        // Random state can advance if the unit changes before commit; failure is refunded.
        if (!Spend(stockpile, costs, out result)) return false;
        if (!owner.TryUpgradeEquippedItem(index))
        {
            Refund(stockpile, costs);
            result = "No eligible reward, or equipped item changed. Resources were refunded.";
            return false;
        }

        EquipmentInstance after = owner.equippedItems[index];
        result = DescribeRoll(before, after);
        return true;
    }

    private static bool TryGetCost(EquipmentInstance item, ResourceStockpile stockpile,
        WorkshopUpgradeCosts rules, out List<KeyValuePair<string, int>> costs, out string reason)
    {
        if (!BuildCosts(item, stockpile, rules, out costs, out reason)) return false;
        foreach (KeyValuePair<string, int> entry in costs)
        {
            if (stockpile.GetQuantity(entry.Key) >= entry.Value) continue;
            ResourceData resource = stockpile.GetResourceData(entry.Key);
            reason = "Not enough " + (resource != null ? resource.ResourceName : entry.Key) +
                     " (" + stockpile.GetQuantity(entry.Key) + "/" + entry.Value + ").";
            return false;
        }
        return true;
    }

    private static bool BuildCosts(EquipmentInstance item, ResourceStockpile stockpile,
        WorkshopUpgradeCosts rules, out List<KeyValuePair<string, int>> costs, out string reason)
    {
        costs = new List<KeyValuePair<string, int>>();
        reason = string.Empty;
        int nextStar = Mathf.Clamp((int)item.rarity + 1, 1, 5);
        if (!AddCost(costs, stockpile, rules.currencyId, rules.goldPerNextStar, nextStar, out reason)) return false;
        if (!AddCost(costs, stockpile, rules.materialId, rules.materialPerNextStar, nextStar, out reason)) return false;
        return true;
    }

    private static bool AddCost(List<KeyValuePair<string, int>> costs, ResourceStockpile stockpile,
        string id, int baseAmount, int nextStar, out string reason)
    {
        reason = string.Empty;
        if (baseAmount <= 0) return true;
        if (string.IsNullOrWhiteSpace(id) || stockpile.GetResourceData(id) == null)
        {
            reason = "Upgrade cost references a missing resource ID: " + id;
            return false;
        }

        long amount = (long)baseAmount * nextStar;
        for (int i = 0; i < costs.Count; i++)
        {
            if (costs[i].Key != id) continue;
            amount += costs[i].Value;
            costs.RemoveAt(i);
            break;
        }
        if (amount > int.MaxValue)
        {
            reason = "Upgrade cost exceeds resource limit.";
            return false;
        }
        costs.Add(new KeyValuePair<string, int>(id, (int)amount));
        return true;
    }

    private static bool Spend(ResourceStockpile stockpile, List<KeyValuePair<string, int>> costs,
        out string error)
    {
        error = string.Empty;
        List<KeyValuePair<string, int>> spent = new List<KeyValuePair<string, int>>();
        foreach (KeyValuePair<string, int> cost in costs)
        {
            if (!stockpile.RemoveResource(cost.Key, cost.Value))
            {
                Refund(stockpile, spent);
                error = "Resources changed before upgrade; transaction cancelled.";
                return false;
            }
            spent.Add(cost);
        }
        return true;
    }

    private static void Refund(ResourceStockpile stockpile, List<KeyValuePair<string, int>> costs)
    {
        foreach (KeyValuePair<string, int> cost in costs)
        {
            if (!stockpile.AddResource(cost.Key, cost.Value))
                Debug.LogError("CRITICAL: Cannot refund upgrade resource " + cost.Key);
        }
    }

    public static string DescribeRoll(EquipmentInstance before, EquipmentInstance after)
    {
        StringBuilder text = new StringBuilder();
        text.Append(before.itemName).Append("  ").Append((int)before.rarity)
            .Append("/5 -> ").Append((int)after.rarity).AppendLine("/5");
        AppendDice(text, "Stat roll 1", after.lastStatRoll1);
        AppendDice(text, "Stat roll 2", after.lastStatRoll2);
        if (after.lastStatRoll1.stat == after.lastStatRoll2.stat)
        {
            text.Append("Both rolls hit ").Append(StatName(after.lastStatRoll1.stat))
                .Append(": total +").AppendLine((after.lastStatRoll1.amount + after.lastStatRoll2.amount).ToString());
        }

        AppendStat(text, "Armor", before.baseStats.armor, after.baseStats.armor);
        AppendStat(text, "Ward", before.baseStats.ward, after.baseStats.ward);
        AppendStat(text, "Melee Damage", before.baseStats.meleeDamage, after.baseStats.meleeDamage);
        AppendStat(text, "Ranged Damage", before.baseStats.rangeDamage, after.baseStats.rangeDamage);
        AppendStat(text, "Magic Damage", before.baseStats.magicDamage, after.baseStats.magicDamage);
        AppendStat(text, "Accuracy", before.baseStats.accuracy, after.baseStats.accuracy);

        int gradesGained = 0;
        for (int i = 0; i < 6; i++)
        {
            EquipmentScalingGrade previous = before.attributeScaling.Get(i);
            EquipmentScalingGrade current = after.attributeScaling.Get(i);
            if (previous == current) continue;
            text.Append(EquipmentAttributeScaling.Name(i)).Append(" scaling: ")
                .Append(previous).Append(" -> ").AppendLine(current.ToString());
            gradesGained++;
        }
        if ((int)after.rarity % 2 == 1 && gradesGained == 0)
            text.AppendLine("Scaling already maxed (all S).");

        int actionsBefore = before.grantedActions == null ? 0 : before.grantedActions.Length;
        int actionsAfter = after.grantedActions == null ? 0 : after.grantedActions.Length;
        for (int i = actionsBefore; i < actionsAfter; i++)
            if (after.grantedActions[i] != null)
                text.Append("New ability: ").AppendLine(after.grantedActions[i].name);
        if ((int)after.rarity % 2 == 1 && actionsAfter == actionsBefore)
            text.AppendLine("No new ability: add eligible abilities to this item's template.");

        int resBefore = before.resistances == null ? 0 : before.resistances.Length;
        int resAfter = after.resistances == null ? 0 : after.resistances.Length;
        for (int i = resBefore; i < resAfter; i++)
        {
            DamageResistance r = after.resistances[i];
            text.Append("New ").Append(r.Type).Append(" resistance: ")
                .Append(r.Percent.ToString("+0.##;-0.##;0")).AppendLine("%");
        }
        int effectsBefore = before.grantedEffects == null ? 0 : before.grantedEffects.Length;
        int effectsAfter = after.grantedEffects == null ? 0 : after.grantedEffects.Length;
        for (int i = effectsBefore; i < effectsAfter; i++)
            if (after.grantedEffects[i] != null && after.grantedEffects[i].Definition != null)
                text.Append("New effect: ").AppendLine(after.grantedEffects[i].Definition.name);
        if ((int)after.rarity % 2 == 0 && resAfter == resBefore && effectsAfter == effectsBefore)
            text.AppendLine("No effect/resistance: configure this item's bonus pool.");

        return text.ToString().TrimEnd();
    }

    private static void AppendDice(StringBuilder text, string prefix, EquipmentStatUpgradeRoll roll)
    {
        text.Append(prefix).Append(": ").Append(StatName(roll.stat))
            .Append(" +").AppendLine(roll.amount.ToString());
    }

    private static string StatName(EquipmentFlatStat stat)
    {
        switch (stat)
        {
            case EquipmentFlatStat.Armor: return "Armor";
            case EquipmentFlatStat.Ward: return "Ward";
            case EquipmentFlatStat.MeleeDamage: return "Melee Damage";
            case EquipmentFlatStat.RangedDamage: return "Ranged Damage";
            case EquipmentFlatStat.MagicDamage: return "Magic Damage";
            default: return "Accuracy";
        }
    }

    private static void AppendStat(StringBuilder text, string name, int oldValue, int newValue)
    {
        if (oldValue == newValue) return;
        text.Append(name).Append(": ").Append(oldValue).Append(" -> ").Append(newValue)
            .Append(" (+").Append(newValue - oldValue).AppendLine(")");
    }
}
