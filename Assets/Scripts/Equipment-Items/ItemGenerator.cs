
using System;
using System.Collections.Generic;
using UnityEngine;
using MutaBorn.Gameplay;



[Serializable]
public struct EquipmentStatValues
{
    public int armor;
    public int ward;
    public int movementPenalty;
    public int meleeDamage;
    public int rangeDamage;
    public int magicDamage;
    public int accuracy;

}

public enum EquipmentFlatStat { Armor, Ward, MeleeDamage, RangedDamage, MagicDamage, Accuracy }

[Serializable]
public struct EquipmentStatUpgradeRoll
{
    public EquipmentFlatStat stat;
    public int amount;
}

[Serializable]
public struct EquipmentInstance
{
    [Header("Equipments Identifiers")]
    public string itemName;
    public string itemID;
    public string instanceId;
    public EquipmentData template;

    [Header("Equipment Type")]
    public ArmorCategory armor;
    public WeaponCategory weapon;
    public AccessoryCategory accessory;
    public EquipmentSlots slot;
    public EquipmentGrade rarity;
    public EquipmentGenerationType type;
    public EquipmentStatBonus statModifier;
    public ActionSet grantedActionSet;
    public EffectCollection grantedEffectCollection;
    public DamageType primaryDamageType;
    public float scalingCoefficient; // Legacy serialized value; no longer drives star upgrades.
    public EquipmentAttributeScaling attributeScaling;
    public EquipmentStatUpgradeRoll lastStatRoll1;
    public EquipmentStatUpgradeRoll lastStatRoll2;

    [Header("Generated Values")]
    public EquipmentStatValues baseStats;
    public EquipmentStatValues rolledStats;
    public int armorRating;
    public int wardRating;
    public DamageResistance[] resistances;
    public DamageComponent[] bonusDamage;
    public ActionDefinition[] grantedActions;
    public EffectGrant[] grantedEffects;
    public float[] abilityWeights;
    public int movementPenalty;
    public int meleeDamage;
    public int rangeDamage;
    public int magicDamage;
    public int accuracyBonus;

}

[Serializable]
public struct ConsumableInstance
{
    [Header("Consumable Identifiers")]
    public string instanceId;
    public ConsumableData definition;
    public ConsumableType consumableType;
    public bool consumeOnUse;
    public int remainingUses;
    public float magnitude;
    public int damage;
    public int coverValue;



}
public static class ItemGenerator 
{


    public static object ItemInstance(ItemData item) 
    {
      

        switch (item) 
        {
            case EquipmentData equipmentData:
                {
                    return EquipmentGenerator.CreateEquipment(equipmentData);
                }
            case ConsumableData consumableData:
                {
                    return ConsumableGenerator.CreateConsumable(consumableData);
                }
            default:
                return null;
           
        }


        
    }
  
}

public static class EquipmentGenerator
{

    public static EquipmentInstance CreateEquipment(EquipmentData equipmentInfo)
    {
        if (equipmentInfo == null)
        {
            return default(EquipmentInstance);

        }

        EquipmentInstance item = new EquipmentInstance
        {
            template = equipmentInfo,
            itemName = equipmentInfo.itemName,
            itemID = equipmentInfo.itemID,
            instanceId = Guid.NewGuid().ToString("N"),
            armor = equipmentInfo.armorGear,
            weapon = equipmentInfo.weaponGear,
            accessory = equipmentInfo.accessoryGear,
            slot = equipmentInfo.slot,
            type = equipmentInfo.genType,
            statModifier = equipmentInfo.statMod,
            attributeScaling = equipmentInfo.StartingScaling,
            grantedActionSet = equipmentInfo.GrantedActionSet,
            grantedEffectCollection = equipmentInfo.GrantedEffectCollection,
            primaryDamageType = equipmentInfo.PrimaryDamageType,
            grantedActions = CopyArray(equipmentInfo.GuaranteedActions),
            grantedEffects = CopyArray(equipmentInfo.GuaranteedEffects),
            bonusDamage = CopyArray(equipmentInfo.GuaranteedBonusDamage),
            resistances = CopyArray(equipmentInfo.Resistances),
            rarity = equipmentInfo.GenerateStarRewards ? EquipmentGrade.None : equipmentInfo.rarity
        };

        EquipmentAbilityOption[] abilityPool = equipmentInfo.PossibleAbilities;
        item.abilityWeights = new float[abilityPool == null ? 0 : abilityPool.Length];
        for (int i = 0; i < item.abilityWeights.Length; i++)
        {
            item.abilityWeights[i] = abilityPool[i] == null ? 0f : Mathf.Max(0f, abilityPool[i].weight);

        }

        switch (item.type)
        {
            case EquipmentGenerationType.Random:
            {
                item.baseStats = GetRandomBaseStats(equipmentInfo);

                break;

            }
            default:
            {
                item.baseStats = GetFixedBaseStats(equipmentInfo);

                break;

            }

        }

        if (equipmentInfo.GenerateStarRewards)
        {
            int desiredStar = Mathf.Clamp((int)equipmentInfo.rarity, 0, (int)EquipmentGrade.FiveStar);
            for (int star = 1; star <= desiredStar; star++)
            {
                if (!TryUpgrade(ref item))
                {
                    Debug.LogWarning($"'{equipmentInfo.name}' cannot improve star {star}: " +
                        "the item has no nonzero eligible flat stats.", equipmentInfo);

                    break;

                }

            }

        }

        RecalculateItemStats(ref item);

        return item;

    }

    // Each upgrade rolls two independent existing flat stats, then its star reward.
    // Roll information stays on the individual item so UI can report duplicate rolls.
    public static bool TryUpgrade(ref EquipmentInstance item)
    {
        if (item.template == null || item.rarity >= EquipmentGrade.FiveStar)
            return false;

        EquipmentInstance upgraded = item;
        if (!TryRollFlatStat(ref upgraded, out EquipmentStatUpgradeRoll roll1) ||
            !TryRollFlatStat(ref upgraded, out EquipmentStatUpgradeRoll roll2))
            return false;
        upgraded.lastStatRoll1 = roll1;
        upgraded.lastStatRoll2 = roll2;

        int nextStar = (int)item.rarity + 1;
        if (nextStar % 2 == 1)
        {
            TryImproveScaling(ref upgraded);
            if (item.template.GenerateStarRewards)
                TryRollAbility(ref upgraded);
        }
        else if (item.template.GenerateStarRewards)
        {
            TryRollBonus(ref upgraded);
        }

        upgraded.rarity = (EquipmentGrade)nextStar;
        RecalculateItemStats(ref upgraded);
        item = upgraded;
        return true;
    }

    private static bool TryRollFlatStat(ref EquipmentInstance item, out EquipmentStatUpgradeRoll roll)
    {
        roll = default(EquipmentStatUpgradeRoll);
        List<EquipmentFlatStat> eligible = new List<EquipmentFlatStat>();
        EquipmentStatValues stats = item.baseStats;
        if (stats.armor != 0) eligible.Add(EquipmentFlatStat.Armor);
        if (stats.ward != 0) eligible.Add(EquipmentFlatStat.Ward);
        if (stats.meleeDamage != 0) eligible.Add(EquipmentFlatStat.MeleeDamage);
        if (stats.rangeDamage != 0) eligible.Add(EquipmentFlatStat.RangedDamage);
        if (stats.magicDamage != 0) eligible.Add(EquipmentFlatStat.MagicDamage);
        if (stats.accuracy != 0) eligible.Add(EquipmentFlatStat.Accuracy);
        if (eligible.Count == 0) return false;

        roll.stat = eligible[UnityEngine.Random.Range(0, eligible.Count)];
        roll.amount = UnityEngine.Random.Range(2, 6);
        switch (roll.stat)
        {
            case EquipmentFlatStat.Armor: stats.armor += roll.amount; break;
            case EquipmentFlatStat.Ward: stats.ward += roll.amount; break;
            case EquipmentFlatStat.MeleeDamage: stats.meleeDamage += roll.amount; break;
            case EquipmentFlatStat.RangedDamage: stats.rangeDamage += roll.amount; break;
            case EquipmentFlatStat.MagicDamage: stats.magicDamage += roll.amount; break;
            case EquipmentFlatStat.Accuracy: stats.accuracy += roll.amount; break;
        }
        item.baseStats = stats;
        return true;
    }

    private static void TryImproveScaling(ref EquipmentInstance item)
    {
        EquipmentAttributeScaling grades = item.attributeScaling;
        float[] chances = grades.UpgradeChances();
        float roll = UnityEngine.Random.value;
        int selected = -1;
        int lastEligible = -1;
        for (int i = 0; i < chances.Length; i++)
        {
            if (chances[i] <= 0f) continue;
            lastEligible = i;
            roll -= chances[i];
            if (roll > 0f) continue;
            selected = i;
            break;
        }
        if (selected < 0) selected = lastEligible;
        if (selected < 0) return; // All S.
        grades.Set(selected, (EquipmentScalingGrade)((int)grades.Get(selected) + 1));
        item.attributeScaling = grades;
    }

    public static bool HasEligibleFlatStat(EquipmentInstance item)
    {
        EquipmentStatValues stats = item.baseStats;
        return stats.armor != 0 || stats.ward != 0 || stats.meleeDamage != 0 ||
            stats.rangeDamage != 0 || stats.magicDamage != 0 || stats.accuracy != 0;
    }

    private static bool TryRollAbility(ref EquipmentInstance item)
    {
        EquipmentAbilityOption[] pool = item.template.PossibleAbilities;
        if (pool == null || pool.Length == 0)
        {
            return false;

        }

        float[] weights = item.abilityWeights == null
            ? new float[pool.Length] : (float[])item.abilityWeights.Clone();
        if (weights.Length != pool.Length)
        {
            Debug.LogWarning("The ability pool was modified after item generation. Existing items cannot reroll safely.", item.template);

            return false;

        }

        HashSet<string> owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (item.grantedActions != null)
        {
            foreach (ActionDefinition action in item.grantedActions)
            {
                if (action != null && !string.IsNullOrWhiteSpace(action.Id))
                {
                    owned.Add(action.Id.Trim());

                }

            }

        }

        if (item.grantedActionSet != null)
        {
            foreach (ActionDefinition action in item.grantedActionSet.Actions)
            {
                if (action != null && !string.IsNullOrWhiteSpace(action.Id))
                {
                    owned.Add(action.Id.Trim());

                }

            }

        }

        List<int> eligible = new List<int>();
        for (int i = 0; i < pool.Length; i++)
        {
            ActionDefinition action = pool[i] != null ? pool[i].action : null;
            if (action == null || string.IsNullOrWhiteSpace(action.Id) ||
                owned.Contains(action.Id.Trim()) || weights[i] <= 0f ||
                float.IsNaN(weights[i]) || float.IsInfinity(weights[i]))
            {
                continue;

            }

            bool duplicate = false;
            foreach (int eligibleIndex in eligible)
            {
                if (string.Equals(pool[eligibleIndex].action.Id, action.Id,
                    StringComparison.OrdinalIgnoreCase))
                {
                    duplicate = true;

                    break;

                }

            }

            if (!duplicate)
            {
                eligible.Add(i);

            }

        }

        int selectedIndex = PickWeightedIndex(eligible, i => weights[i]);
        if (selectedIndex < 0)
        {
            return false;

        }

        ActionDefinition chosen = pool[selectedIndex].action;
        item.grantedActions = Append(item.grantedActions, chosen);
        float redistributed = weights[selectedIndex] / Mathf.Max(1, eligible.Count - 1);
        weights[selectedIndex] = 0f;
        foreach (int index in eligible)
        {
            if (index != selectedIndex)
            {
                weights[index] += redistributed;

            }

        }

        item.abilityWeights = weights;

        return true;

    }

    private static bool TryRollBonus(ref EquipmentInstance item)
    {
        EquipmentBonusOption[] pool = item.template.PossibleBonuses;
        if (pool == null || pool.Length == 0)
        {
            return false;

        }

        List<int> eligible = new List<int>();
        for (int i = 0; i < pool.Length; i++)
        {
            EquipmentBonusOption option = pool[i];
            if (option == null ||
                (option.kind != EquipmentBonusKind.Effect && option.kind != EquipmentBonusKind.Resistance) ||
                option.weight <= 0f || float.IsNaN(option.weight) || float.IsInfinity(option.weight))
            {
                continue;

            }

            if (option.kind == EquipmentBonusKind.Effect &&
                (option.effect == null || option.effect.Definition == null ||
                 option.effect.Definition.Operation != EffectOperation.ModifyResource))
            {
                continue;

            }

            if ((option.kind == EquipmentBonusKind.DamageComponent ||
                option.kind == EquipmentBonusKind.Resistance) &&
                option.damageType == DamageType.Unspecified)
            {
                continue;

            }

            if ((option.kind == EquipmentBonusKind.DamageComponent &&
                 Mathf.Max(option.amountRange.x, option.amountRange.y) <= 0) ||
                (option.kind != EquipmentBonusKind.Effect &&
                 option.amountRange.x == 0 && option.amountRange.y == 0))
            {
                continue;

            }

            eligible.Add(i);

        }

        int choice = PickWeightedIndex(eligible, i => pool[i].weight);
        if (choice < 0)
        {
            return false;

        }

        EquipmentBonusOption selected = pool[choice];
        int min = Mathf.Min(selected.amountRange.x, selected.amountRange.y);
        int max = Mathf.Max(selected.amountRange.x, selected.amountRange.y);
        int amount = UnityEngine.Random.Range(min, max == int.MaxValue ? max : max + 1);
        if (selected.kind != EquipmentBonusKind.Effect && amount == 0)
        {
            // A successful star always grants a nonzero bonus.
            amount = max > 0 ? 1 : -1;

        }

        switch (selected.kind)
        {
            case EquipmentBonusKind.Stat:
            {
                EquipmentStatValues stats = item.rolledStats;
                switch (selected.stat)
                {
                    case EquipmentRollStat.Armor:
                    {
                        stats.armor += amount;

                        break;

                    }
                    case EquipmentRollStat.Ward:
                    {
                        stats.ward += amount;

                        break;

                    }
                    case EquipmentRollStat.MeleeDamage:
                    {
                        stats.meleeDamage += amount;

                        break;

                    }
                    case EquipmentRollStat.RangedDamage:
                    {
                        stats.rangeDamage += amount;

                        break;

                    }
                    case EquipmentRollStat.MagicDamage:
                    {
                        stats.magicDamage += amount;

                        break;

                    }

                }

                item.rolledStats = stats;

                break;

            }
            case EquipmentBonusKind.DamageComponent:
            {
                item.bonusDamage = Append(item.bonusDamage, new DamageComponent(selected.damageType, Mathf.Max(0, amount)));

                break;

            }
            case EquipmentBonusKind.Resistance:
            {
                item.resistances = Append(item.resistances, new DamageResistance(selected.damageType, amount));

                break;

            }
            case EquipmentBonusKind.Effect:
            {
                item.grantedEffects = Append(item.grantedEffects, selected.effect);

                break;

            }
            default:
            {
                return false;

            }

        }

        return true;

    }

    private static int PickWeightedIndex(List<int> eligible, Func<int, float> weightAt)
    {
        double total = 0d;
        foreach (int index in eligible)
        {
            total += weightAt(index);

        }

        if (total <= 0d || double.IsNaN(total) || double.IsInfinity(total))
        {
            return -1;

        }

        double roll = UnityEngine.Random.value * total;
        foreach (int index in eligible)
        {
            roll -= weightAt(index);
            if (roll <= 0d)
            {
                return index;

            }

        }

        return eligible[eligible.Count - 1];

    }

    private static EquipmentStatValues GetFixedBaseStats(EquipmentData info)
    {
        return new EquipmentStatValues
        {
            armor = info.armorRating,
            ward = info.wardRating,
            movementPenalty = info.movementPenalty,
            meleeDamage = info.meleeDamage,
            rangeDamage = info.rangeDamage,
            magicDamage = info.magicDamage,
            accuracy = info.accuracyBonus
        };

    }

    private static EquipmentStatValues GetRandomBaseStats(EquipmentData info)
    {
        return new EquipmentStatValues
        {
            armor = RandomFromRange(info.armorRatingRange),
            ward = info.wardRating,
            movementPenalty = RandomFromRange(info.movementPenaltyRange),
            meleeDamage = RandomFromRange(info.meleeDamageRange),
            rangeDamage = RandomFromRange(info.distanceDamageRange),
            magicDamage = RandomFromRange(info.magicDamageRange),
            accuracy = RandomFromRange(info.accuracyRange)
        };

    }

    private static int RandomFromRange(Vector2Int range)
    {
        int min = Mathf.Min(range.x, range.y);
        int max = Mathf.Max(range.x, range.y);

        return UnityEngine.Random.Range(min, max == int.MaxValue ? max : max + 1);

    }

    // A stored item has no wielder: it shows permanent base stats plus unscaled bonuses.
    // Equipped stats must always be requested with the owner, never cached on the item.
    public static EquipmentStatValues GetEffectiveStats(EquipmentInstance item, UnitData wielder)
    {
        long bonusPercent = item.attributeScaling.TotalPercent(wielder);
        EquipmentStatValues baseStats = item.baseStats;
        EquipmentStatValues bonus = item.rolledStats;
        return new EquipmentStatValues
        {
            armor = Scale(baseStats.armor, bonusPercent) + bonus.armor,
            ward = Scale(baseStats.ward, bonusPercent) + bonus.ward,
            meleeDamage = Scale(baseStats.meleeDamage, bonusPercent) + bonus.meleeDamage,
            rangeDamage = Scale(baseStats.rangeDamage, bonusPercent) + bonus.rangeDamage,
            magicDamage = Scale(baseStats.magicDamage, bonusPercent) + bonus.magicDamage,
            accuracy = Scale(baseStats.accuracy, bonusPercent) + bonus.accuracy,
            movementPenalty = baseStats.movementPenalty + bonus.movementPenalty
        };
    }

    private static int Scale(int flatStat, long bonusPercent)
    {
        if (flatStat == 0) return 0;
        decimal scaled = decimal.Ceiling((decimal)flatStat * (100m + bonusPercent) / 100m);
        return scaled >= int.MaxValue ? int.MaxValue :
            scaled <= int.MinValue ? int.MinValue : (int)scaled;
    }

    private static void RecalculateItemStats(ref EquipmentInstance item)
    {
        item.scalingCoefficient = 0f;
        EquipmentStatValues values = GetEffectiveStats(item, null);
        item.armorRating = values.armor;
        item.wardRating = values.ward;
        item.movementPenalty = values.movementPenalty;
        item.accuracyBonus = values.accuracy;
        item.meleeDamage = values.meleeDamage;
        item.rangeDamage = values.rangeDamage;
        item.magicDamage = values.magicDamage;
    }

    private static T[] CopyArray<T>(T[] source)
    {
        return source == null ? new T[0] : (T[])source.Clone();

    }

    private static T[] Append<T>(T[] original, T value)
    {
        int count = original == null ? 0 : original.Length;
        T[] updated = new T[count + 1];
        if (count > 0)
        {
            Array.Copy(original, updated, count);

        }

        updated[count] = value;

        return updated;

    }

}

public static class ConsumableGenerator
{
    public static ConsumableInstance CreateConsumable(ConsumableData consumableData) 
    {
        ConsumableInstance consumable = new ConsumableInstance();

        consumable.definition = consumableData;
        consumable.instanceId = Guid.NewGuid().ToString("N");
        consumable.consumableType = consumableData.consumable;
        consumable.consumeOnUse = consumableData.consumeOnUse;
        consumable.remainingUses = consumableData.MaxUses;
        consumable.magnitude = consumableData.magnitude;
        consumable.damage = consumableData.damage;
        consumable.coverValue = consumableData.coverValue;
        
        return consumable;
    }
}