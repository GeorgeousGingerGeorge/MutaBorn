using Unity.AppUI.Editor;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;


public struct EquipmentInstance
{
    [Header("Equipments Identifiers")]
    public string itemName;
    public string itemID;
    [Header("Equipment Type ")]
    public ArmorCategory armor;
    public WeaponCategory weapon;
    public AccessoryCategory accessory;
    public EquipmentSlots targetSlot;
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
public class EquipmentGenerator : MonoBehaviour
{
    float[] equipmentScaling = { 0.20f,0.40f, 0.60f, 0.80f, 1.00f, 1.20f };
    
    public EquipmentInstance CreateEquipment(EquipmentData equipmentInfo) 
    {
        EquipmentInstance equipment = new EquipmentInstance();

        equipment.itemName = equipmentInfo.name;
        equipment.itemID = equipmentInfo.id;
        equipment.armor = equipmentInfo.armorGear;
        equipment.weapon = equipmentInfo.weaponGear;
        equipment.accessory = equipmentInfo.accessoryGear;
        equipment.targetSlot = equipmentInfo.slot;
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
    private void CreateEquipmentFixed(EquipmentInstance equipment, EquipmentData equipmentInfo) 
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
    private void CreateEquipmentRandom(EquipmentInstance equipment, EquipmentData equipmentInfo) 
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
    private void CreateEquipmentUnique(EquipmentInstance equipment, EquipmentData equipmentInfo) 
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
    private void GradeBonus(EquipmentInstance equipment) 
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

        equipment.armorRating = Mathf.CeilToInt((equipment.armorRating * statMultiplier ));
        equipment.movementPenalty = Mathf.CeilToInt((equipment.movementPenalty * statMultiplier));
        equipment.accuracyBonus = Mathf.CeilToInt((equipment.accuracyBonus * statMultiplier));
        equipment.meleeDamage = Mathf.CeilToInt((equipment.meleeDamage * statMultiplier));
        equipment.rangeDamage = Mathf.CeilToInt((equipment.rangeDamage * statMultiplier)); 
        equipment.magicDamage = Mathf.CeilToInt((equipment.magicDamage * statMultiplier));
    }
}


