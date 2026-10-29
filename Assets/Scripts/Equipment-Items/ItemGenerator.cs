
using UnityEngine;



public struct EquipmentInstance
{
    [Header("Equipments Identifiers")]
    public string itemName;
    public string itemID;
    [Header("Equipment Type ")]
    public ArmorCategory armor;
    public WeaponCategory weapon;
    public AccessoryCategory accessory;
    public EquipmentSlots slot;
    public EquipmentGrade rarity;
    public EquipmentGenerationType type;
    public EquipmentStatBonus statModifier;
    public ActionSet grantedActionSet;
    public float scalingCoefficient; 

    [Header("Equipment Damage")]
    public int armorRating;
    public int movementPenalty;
    public int meleeDamage;
    public int rangeDamage;
    public int magicDamage;
    public int accuracyBonus;

  

}

public struct ConsumableInstance
{
    [Header("Consumable Identifiers")]
    public ConsumableType consumableType;
    public bool consumeOnUse;
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

    static float[] equipmentScaling = { 0.20f, 0.40f, 0.60f, 0.80f, 1.00f, 1.20f };


    public static EquipmentInstance CreateEquipment(EquipmentData equipmentInfo)
    {
        EquipmentInstance equipment = new EquipmentInstance();

        equipment.itemName = equipmentInfo.itemName;
        equipment.itemID = equipmentInfo.itemID;
        equipment.armor = equipmentInfo.armorGear;
        equipment.weapon = equipmentInfo.weaponGear;
        equipment.accessory = equipmentInfo.accessoryGear;
        equipment.slot = equipmentInfo.slot;
        equipment.rarity = equipmentInfo.rarity;
        equipment.type = equipmentInfo.genType;
        equipment.statModifier = equipmentInfo.statMod;
        equipment.grantedActionSet = equipmentInfo.GrantedActionSet;


        switch (equipment.type)
        {

            case EquipmentGenerationType.Fixed:
                CreateEquipmentFixed(equipment, equipmentInfo);
                break;
            case EquipmentGenerationType.Random:
                CreateEquipmentRandom(equipment, equipmentInfo);
                break;
            case EquipmentGenerationType.Unique:
                CreateEquipmentUnique(equipment, equipmentInfo);
                break;
            default:
                break;
        }


        return equipment;
    }

    //Uses Normal stats, No modifications or different scaling
    private static void CreateEquipmentFixed(EquipmentInstance equipment, EquipmentData equipmentInfo)
    {

        equipment.armorRating = equipmentInfo.armorRating;
        equipment.movementPenalty = equipmentInfo.movementPenalty;
        equipment.accuracyBonus = equipmentInfo.accuracyBonus;
        equipment.meleeDamage = equipmentInfo.meleeDamage;
        equipment.rangeDamage = equipmentInfo.rangeDamage;
        equipment.magicDamage = equipmentInfo.magicDamage;

        GradeBonus(equipment);
    }
    //Based Scaling is still the same but possibly different starting values
    private static void CreateEquipmentRandom(EquipmentInstance equipment, EquipmentData equipmentInfo)
    {
        equipment.armorRating = Random.Range(equipmentInfo.armorRatingRange.x, equipmentInfo.armorRatingRange.y);
        equipment.movementPenalty = Random.Range(equipmentInfo.movementPenaltyRange.x, equipmentInfo.movementPenaltyRange.y);
        equipment.accuracyBonus = Random.Range(equipmentInfo.accuracyRange.x, equipmentInfo.accuracyRange.y);
        equipment.meleeDamage = Random.Range(equipmentInfo.meleeDamageRange.x, equipmentInfo.meleeDamageRange.y);
        equipment.rangeDamage = Random.Range(equipmentInfo.distanceDamageRange.x, equipmentInfo.distanceDamageRange.y);
        equipment.magicDamage = Random.Range(equipmentInfo.magicDamageRange.x, equipmentInfo.magicDamageRange.y);
        GradeBonus(equipment);
    }

    //Unique Equipment- Just changes based scaling
    private static void CreateEquipmentUnique(EquipmentInstance equipment, EquipmentData equipmentInfo)
    {
        equipmentScaling[0] = 0.35f;
        equipment.armorRating = equipmentInfo.armorRating;
        equipment.movementPenalty = equipmentInfo.movementPenalty;
        equipment.accuracyBonus = equipmentInfo.accuracyBonus;
        equipment.meleeDamage = equipmentInfo.meleeDamage;
        equipment.rangeDamage = equipmentInfo.rangeDamage;
        equipment.magicDamage = equipmentInfo.magicDamage;
        GradeBonus(equipment);
    }

    //Rarity/Scaling Benefits
    private static void GradeBonus(EquipmentInstance equipment)
    {
        float usingCoefficient = equipmentScaling[(int)equipment.rarity];
        float basedCoefficient = equipmentScaling[0];


        if (equipment.rarity == EquipmentGrade.None)
        {
            equipment.scalingCoefficient = basedCoefficient;
        }
        else
        {
            //Base 20% or 35% if unique
            //Base times the rarityMultiplier = which is then used to get the scaling coefficient when we get
            //to unitData to calculate the scaling bonus to damage or any of the stats
            equipment.scalingCoefficient = basedCoefficient * (1 + usingCoefficient);
        }

        float statMultiplier = (1 + equipment.scalingCoefficient);

        equipment.armorRating = Mathf.CeilToInt((equipment.armorRating * statMultiplier));
        equipment.movementPenalty = Mathf.CeilToInt((equipment.movementPenalty * statMultiplier));
        equipment.accuracyBonus = Mathf.CeilToInt((equipment.accuracyBonus * statMultiplier));
        equipment.meleeDamage = Mathf.CeilToInt((equipment.meleeDamage * statMultiplier));
        equipment.rangeDamage = Mathf.CeilToInt((equipment.rangeDamage * statMultiplier));
        equipment.magicDamage = Mathf.CeilToInt((equipment.magicDamage * statMultiplier));
    }
}

public static class ConsumableGenerator
{
    public static ConsumableInstance CreateConsumable(ConsumableData consumableData) 
    {
        ConsumableInstance consumable = new ConsumableInstance();

        consumable.consumableType = consumableData.consumable;
        consumable.consumeOnUse = consumableData.consumeOnUse;
        consumable.magnitude = consumableData.magnitude;
        consumable.damage = consumableData.damage;
        consumable.coverValue = consumableData.coverValue;
        
        return consumable;
    }
}