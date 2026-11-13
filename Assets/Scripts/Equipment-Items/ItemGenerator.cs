
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
    public EquipmentType typeDefinition;
    public string templateId;

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
    public EquipmentBonusStack[] bonusStacks;
    public string lastUpgradeBonus;

    public bool IsValid => typeDefinition != null && typeDefinition.TryGetTemplate(templateId, out _);
    public EquipmentFitType EquipmentFit => typeDefinition != null ? typeDefinition.EquipmentFit : EquipmentFitType.Auto;
    public bool RequiresBothHands => typeDefinition != null &&
        (typeDefinition.TryGetTemplate(templateId, out EquipmentTemplate value)
            ? typeDefinition.GetRequiresBothHands(value) : typeDefinition.RequiresBothHands);
    public ArmorCategory ArmorWeight => typeDefinition != null &&
        typeDefinition.TryGetTemplate(templateId, out EquipmentTemplate weight)
            ? typeDefinition.GetArmorWeight(weight) : armor;
    public WeaponSubtype WeaponSubtype => typeDefinition != null ? typeDefinition.WeaponSubtype : WeaponSubtype.None;
    public Sprite Icon => typeDefinition != null && typeDefinition.TryGetTemplate(templateId, out EquipmentTemplate icon)
        ? icon.TemplateIcon != null ? icon.TemplateIcon : typeDefinition.TypeIcon : null;
    public Sprite TypeIcon => typeDefinition != null ? typeDefinition.TypeIcon : null;
    public string Description => typeDefinition != null && typeDefinition.TryGetTemplate(templateId, out EquipmentTemplate desc)
        ? desc.Description : string.Empty;
    public string DisplayTypeLabel => typeDefinition != null ? typeDefinition.DisplayTypeLabel : string.Empty;
    public GameObject ModelPrefab => typeDefinition != null && typeDefinition.TryGetTemplate(templateId, out EquipmentTemplate model)
        ? model.ModelOverride != null ? model.ModelOverride : typeDefinition.ModelPrefab : null;

}

[Serializable]
public struct EquipmentBonusStack
{
    public string bonusId;
    public int stacks;

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

    // Crafted items use this path and always start at zero stars.
    public static EquipmentInstance CreateEquipment(EquipmentTemplateReference reference)
    {
        if (!reference.TryResolve(out EquipmentTemplate definition))
        {
            return default(EquipmentInstance);

        }

        EquipmentType equipmentType = reference.equipmentType;
        EquipmentInstance item = new EquipmentInstance
        {
            typeDefinition = equipmentType,
            templateId = definition.TemplateId,
            itemID = equipmentType.TypeId + ":" + definition.TemplateId,
            itemName = definition.DisplayName,
            instanceId = Guid.NewGuid().ToString("N"),
            armor = equipmentType.GetArmorWeight(definition),
            weapon = equipmentType.WeaponCategory,
            accessory = equipmentType.AccessoryCategory,
            rarity = EquipmentGrade.None,
            type = definition.GenerationType,
            attributeScaling = definition.StartingScaling,
            grantedActionSet = equipmentType.GrantedActionSet,
            grantedEffectCollection = equipmentType.GrantedEffectCollection,
            primaryDamageType = definition.DamageTypeOverride == DamageType.Unspecified
                ? equipmentType.PrimaryDamageType : definition.DamageTypeOverride,
            grantedActions = equipmentType.GetStartingActions(definition),
            grantedEffects = Combine(equipmentType.StartingEffects, definition.AddedEffects),
            resistances = CopyArray(definition.StartingResistances),
            bonusDamage = CopyArray(definition.StartingBonusDamage),
            abilityWeights = Array.Empty<float>()
        };
        item.baseStats = item.type == EquipmentGenerationType.Random
            ? GetRandomBaseStats(definition.RandomRanges) : definition.FixedStats;

        RecalculateItemStats(ref item);

        return item;

    }

    // Loot rolls the template's natural star chances and the source's extra chances.
    // Each earned star receives the same rewards as a normal upgrade, without spending resources.
    public static EquipmentInstance CreateLootEquipment(EquipmentTemplateReference reference,
        EquipmentStarDice additionalRolls)
    {
        EquipmentInstance item = CreateEquipment(reference);
        if (!item.IsValid)
        {
            return item;

        }

        if (!reference.TryResolve(out EquipmentTemplate definition))
        {
            return item;

        }

        int stars = Mathf.Min(5, definition.StartingStarRolls.Roll() + additionalRolls.Roll());
        for (int i = 0; i < stars; i++)
        {
            if (!TryUpgrade(ref item))
            {
                Debug.LogWarning($"Cannot generate all stars for '{item.itemName}': check its stat growth weights.", reference.equipmentType);

                break;

            }

        }

        return item;

    }

    // Each upgrade rolls two independent existing flat stats, then its star reward.
    // Roll information stays on the individual item so UI can report duplicate rolls.
    public static bool TryUpgrade(ref EquipmentInstance item)
    {
        return TryUpgradeEmbedded(ref item);

    }

    private static bool TryUpgradeEmbedded(ref EquipmentInstance item)
    {
        if (!item.IsValid || item.rarity >= EquipmentGrade.FiveStar ||
            !item.typeDefinition.TryGetTemplate(item.templateId, out EquipmentTemplate definition))
        {
            return false;

        }

        EquipmentInstance updated = item;
        if (!TryRollEmbeddedStat(ref updated, definition.StatGrowth, out EquipmentStatUpgradeRoll first) ||
            !TryRollEmbeddedStat(ref updated, definition.StatGrowth, out EquipmentStatUpgradeRoll second))
        {
            return false;

        }

        updated.lastStatRoll1 = first;
        updated.lastStatRoll2 = second;
        int nextStar = (int)item.rarity + 1;
        updated.lastUpgradeBonus = string.Empty;
        if (!RollEmbeddedBonus(ref updated, definition))
        {
            return false;

        }

        RollEmbeddedScaling(ref updated, definition, nextStar);
        updated.rarity = (EquipmentGrade)nextStar;
        RecalculateItemStats(ref updated);
        item = updated;

        return true;

    }

    private static bool TryRollEmbeddedStat(ref EquipmentInstance item,
        EquipmentWeightedStatGrowth[] growth, out EquipmentStatUpgradeRoll roll)
    {
        roll = default(EquipmentStatUpgradeRoll);
        if (growth == null || growth.Length == 0)
        {
            return false;

        }

        List<int> eligible = new List<int>();
        for (int i = 0; i < growth.Length; i++)
        {
            EquipmentWeightedStatGrowth entry = growth[i];
            float weight = GetFlatStat(item.baseStats, entry.stat) == 0 ? entry.zeroWeight : entry.growthWeight;
            if (weight > 0f)
            {
                eligible.Add(i);

            }

        }

        EquipmentStatValues startingStats = item.baseStats;
        int selected = PickWeightedIndex(eligible, i => GetFlatStat(startingStats, growth[i].stat) == 0
            ? growth[i].zeroWeight : growth[i].growthWeight);
        if (selected < 0)
        {
            return false;

        }

        EquipmentWeightedStatGrowth chosen = growth[selected];
        int amount = Mathf.Max(1, RandomFromRange(chosen.amountRange));
        EquipmentStatValues stats = item.baseStats;
        AddFlatStat(ref stats, chosen.stat, amount);
        item.baseStats = stats;
        roll = new EquipmentStatUpgradeRoll { stat = chosen.stat, amount = amount };

        return true;

    }

    private static int GetFlatStat(EquipmentStatValues stats, EquipmentFlatStat stat)
    {
        switch (stat)
        {
            case EquipmentFlatStat.Armor: return stats.armor;
            case EquipmentFlatStat.Ward: return stats.ward;
            case EquipmentFlatStat.MeleeDamage: return stats.meleeDamage;
            case EquipmentFlatStat.RangedDamage: return stats.rangeDamage;
            case EquipmentFlatStat.MagicDamage: return stats.magicDamage;
            default: return stats.accuracy;

        }

    }

    private static void AddFlatStat(ref EquipmentStatValues stats, EquipmentFlatStat stat, int amount)
    {
        switch (stat)
        {
            case EquipmentFlatStat.Armor: stats.armor += amount; break;
            case EquipmentFlatStat.Ward: stats.ward += amount; break;
            case EquipmentFlatStat.MeleeDamage: stats.meleeDamage += amount; break;
            case EquipmentFlatStat.RangedDamage: stats.rangeDamage += amount; break;
            case EquipmentFlatStat.MagicDamage: stats.magicDamage += amount; break;
            case EquipmentFlatStat.Accuracy: stats.accuracy += amount; break;

        }

    }

    private static void RollEmbeddedScaling(ref EquipmentInstance item, EquipmentTemplate template, int star)
    {
        foreach (EquipmentScalingOpportunity rule in template.ScalingOpportunities)
        {
            if (rule.star != star || UnityEngine.Random.value >= rule.successChance ||
                rule.eligibleAttributes == null)
            {
                continue;

            }

            List<int> eligible = new List<int>();
            foreach (EquipmentScalingAttribute attribute in rule.eligibleAttributes)
            {
                int index = (int)attribute;
                if (index >= 0 && index < 6 && item.attributeScaling.Get(index) < EquipmentScalingGrade.S &&
                    !eligible.Contains(index))
                {
                    eligible.Add(index);

                }

            }

            if (eligible.Count > 0)
            {
                ImproveScaling(ref item, eligible[UnityEngine.Random.Range(0, eligible.Count)]);

            }

        }

    }

    private static void ImproveScaling(ref EquipmentInstance item, int attribute)
    {
        EquipmentAttributeScaling scaling = item.attributeScaling;
        scaling.Set(attribute, (EquipmentScalingGrade)((int)scaling.Get(attribute) + 1));
        item.attributeScaling = scaling;

    }

    private static bool RollEmbeddedBonus(ref EquipmentInstance item, EquipmentTemplate template)
    {
        List<EquipmentBonusOption> pool = new List<EquipmentBonusOption>();
        List<string> ids = new List<string>();
        EquipmentBonusOption[] defaults = item.typeDefinition.UpgradeBonuses;
        for (int i = 0; i < defaults.Length; i++)
        {
            EquipmentBonusOption bonus = defaults[i];
            if (bonus == null || (template.ExcludedTypeBonusIds != null &&
                Array.IndexOf(template.ExcludedTypeBonusIds, bonus.bonusId) >= 0))
            {
                continue;

            }

            pool.Add(bonus);
            ids.Add(!string.IsNullOrWhiteSpace(bonus.bonusId) ? bonus.bonusId : "type-" + i);

        }

        EquipmentBonusOption[] added = template.AddedUpgradeBonuses;
        for (int i = 0; i < added.Length; i++)
        {
            if (added[i] != null)
            {
                pool.Add(added[i]);
                ids.Add(!string.IsNullOrWhiteSpace(added[i].bonusId) ? added[i].bonusId : "template-" + i);

            }

        }

        List<int> eligible = new List<int>();
        for (int i = 0; i < pool.Count; i++)
        {
            EquipmentBonusOption bonus = pool[i];
            int owned = BonusStackCount(item, ids[i]);
            int cap = bonus.repeatable ? Mathf.Max(1, bonus.maxStacks) : 1;
            if (bonus.weight <= 0f || owned >= cap || !IsEligibleBonus(item, bonus))
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
        int amount = RandomFromRange(selected.amountRange);
        if (amount == 0 && (selected.kind == EquipmentBonusKind.Stat ||
            selected.kind == EquipmentBonusKind.Resistance))
        {
            amount = Mathf.Max(selected.amountRange.x, selected.amountRange.y) > 0 ? 1 : -1;

        }

        string bonusDetail = string.Empty;
        switch (selected.kind)
        {
            case EquipmentBonusKind.Stat:
            {
                EquipmentStatValues stats = item.rolledStats;
                AddFlatStat(ref stats, (EquipmentFlatStat)selected.stat, amount);
                item.rolledStats = stats;
                bonusDetail = $"{amount:+0;-0;0} {BonusStatName(selected.stat)}";
                break;
            }
            case EquipmentBonusKind.DamageComponent:
            {
                int damage = Mathf.Max(0, amount);
                item.bonusDamage = Append(item.bonusDamage,
                    new DamageComponent(selected.damageType, damage));
                bonusDetail = $"+{damage} {selected.damageType} damage";
                break;
            }
            case EquipmentBonusKind.Resistance:
            {
                item.resistances = Append(item.resistances,
                    new DamageResistance(selected.damageType, amount));
                bonusDetail = $"{amount:+0;-0;0}% {selected.damageType} resistance";
                break;
            }
            case EquipmentBonusKind.Effect:
            {
                item.grantedEffects = Append(item.grantedEffects, selected.effect);
                bonusDetail = "Effect: " + selected.effect.Definition.name;
                break;
            }
            case EquipmentBonusKind.Action:
            {
                item.grantedActions = Append(item.grantedActions, selected.action);
                bonusDetail = "Ability: " + selected.action.name;
                break;
            }
            case EquipmentBonusKind.ScalingGrade:
            {
                int attribute = (int)selected.scalingAttribute;
                EquipmentScalingGrade previous = item.attributeScaling.Get(attribute);
                ImproveScaling(ref item, attribute);
                bonusDetail = $"{EquipmentAttributeScaling.Name(attribute)} scaling " +
                    $"{previous} -> {item.attributeScaling.Get(attribute)}";
                break;
            }

        }

        List<EquipmentBonusStack> stacks = new List<EquipmentBonusStack>(item.bonusStacks ?? Array.Empty<EquipmentBonusStack>());
        int index = stacks.FindIndex(entry => entry.bonusId == ids[choice]);
        if (index >= 0)
        {
            EquipmentBonusStack record = stacks[index];
            record.stacks++;
            stacks[index] = record;

        }
        else
        {
            stacks.Add(new EquipmentBonusStack { bonusId = ids[choice], stacks = 1 });

        }

        item.bonusStacks = stacks.ToArray();
        item.lastUpgradeBonus = string.IsNullOrWhiteSpace(selected.displayName)
            ? bonusDetail : selected.displayName + " (" + bonusDetail + ")";

        return true;

    }

    private static string BonusStatName(EquipmentRollStat stat)
    {
        switch (stat)
        {
            case EquipmentRollStat.Armor: return "Armor";
            case EquipmentRollStat.Ward: return "Ward";
            case EquipmentRollStat.MeleeDamage: return "Melee Damage";
            case EquipmentRollStat.RangedDamage: return "Ranged Damage";
            case EquipmentRollStat.MagicDamage: return "Magic Damage";
            default: return "Accuracy";

        }

    }

    private static int BonusStackCount(EquipmentInstance item, string id)
    {
        if (item.bonusStacks != null)
        {
            foreach (EquipmentBonusStack entry in item.bonusStacks)
            {
                if (entry.bonusId == id)
                {
                    return entry.stacks;

                }

            }

        }

        return 0;

    }

    private static bool IsEligibleBonus(EquipmentInstance item, EquipmentBonusOption bonus)
    {
        switch (bonus.kind)
        {
            case EquipmentBonusKind.Stat:
                return bonus.amountRange.x != 0 || bonus.amountRange.y != 0;
            case EquipmentBonusKind.DamageComponent:
                return bonus.damageType != DamageType.Unspecified &&
                    Mathf.Max(bonus.amountRange.x, bonus.amountRange.y) > 0;
            case EquipmentBonusKind.Resistance:
                return bonus.damageType != DamageType.Unspecified &&
                    (bonus.amountRange.x != 0 || bonus.amountRange.y != 0);
            case EquipmentBonusKind.Effect:
                return bonus.effect != null && bonus.effect.Definition != null;
            case EquipmentBonusKind.Action:
                return bonus.action != null && !HasAction(item, bonus.action);
            case EquipmentBonusKind.ScalingGrade:
                return (int)bonus.scalingAttribute >= 0 && (int)bonus.scalingAttribute < 6 &&
                    item.attributeScaling.Get((int)bonus.scalingAttribute) < EquipmentScalingGrade.S;
            default:
                return false;

        }

    }

    private static bool HasAction(EquipmentInstance item, ActionDefinition action)
    {
        if (item.grantedActions != null && Array.IndexOf(item.grantedActions, action) >= 0)
        {
            return true;

        }

        if (item.grantedActionSet != null)
        {
            foreach (ActionDefinition existing in item.grantedActionSet.Actions)
            {
                if (existing == action)
                {
                    return true;

                }

            }

        }

        return false;

    }

    public static bool HasEligibleFlatStat(EquipmentInstance item)
    {
        if (!item.IsValid || !item.typeDefinition.TryGetTemplate(item.templateId, out EquipmentTemplate definition))
        {
            return false;

        }

        foreach (EquipmentWeightedStatGrowth entry in definition.StatGrowth)
        {
            float weight = GetFlatStat(item.baseStats, entry.stat) == 0
                ? entry.zeroWeight : entry.growthWeight;
            if (weight > 0f)
            {
                return true;

            }

        }

        return false;

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

    private static EquipmentStatValues GetRandomBaseStats(EquipmentStatRanges ranges)
    {
        return new EquipmentStatValues
        {
            armor = RandomFromRange(ranges.armor),
            ward = RandomFromRange(ranges.ward),
            movementPenalty = RandomFromRange(ranges.movementPenalty),
            meleeDamage = RandomFromRange(ranges.meleeDamage),
            rangeDamage = RandomFromRange(ranges.rangeDamage),
            magicDamage = RandomFromRange(ranges.magicDamage),
            accuracy = RandomFromRange(ranges.accuracy)
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
        EquipmentStatValues result = new EquipmentStatValues
        {
            armor = Scale(baseStats.armor, bonusPercent) + bonus.armor,
            ward = Scale(baseStats.ward, bonusPercent) + bonus.ward,
            meleeDamage = Scale(baseStats.meleeDamage, bonusPercent) + bonus.meleeDamage,
            rangeDamage = Scale(baseStats.rangeDamage, bonusPercent) + bonus.rangeDamage,
            magicDamage = Scale(baseStats.magicDamage, bonusPercent) + bonus.magicDamage,
            accuracy = Scale(baseStats.accuracy, bonusPercent) + bonus.accuracy,
            movementPenalty = baseStats.movementPenalty + bonus.movementPenalty

        };

        int dwarfPercent = wielder != null && wielder.RaceDefinition != null
            ? wielder.RaceDefinition.GetEquipmentBonusPercent(item) : 0;
        if (dwarfPercent > 0)
        {
            result.armor = Scale(result.armor, dwarfPercent);
            result.ward = Scale(result.ward, dwarfPercent);
            result.meleeDamage = Scale(result.meleeDamage, dwarfPercent);
            result.rangeDamage = Scale(result.rangeDamage, dwarfPercent);
            result.magicDamage = Scale(result.magicDamage, dwarfPercent);
            result.accuracy = Scale(result.accuracy, dwarfPercent);

        }

        return result;

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

    private static T[] Combine<T>(T[] shared, T[] added)
    {
        int firstCount = shared == null ? 0 : shared.Length;
        int secondCount = added == null ? 0 : added.Length;
        T[] result = new T[firstCount + secondCount];
        if (firstCount > 0)
        {
            Array.Copy(shared, result, firstCount);

        }

        if (secondCount > 0)
        {
            Array.Copy(added, 0, result, firstCount, secondCount);

        }

        return result;

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