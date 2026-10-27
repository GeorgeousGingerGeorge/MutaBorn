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

    private void GradeBonus(EquipmentInstance equipment) 
    {
        float usingCoefficient = equipmentScaling[(int)equipment.rarity];
        float basedCoefficient = equipmentScaling[0];
        switch (equipment.rarity) 
        {
            case EquipmentGrade.OneStar:
                {
                    equipment.scalingCoefficient = basedCoefficient * (1 * equipmentScaling[(int)equipment.rarity]);

                }
                break;
            case EquipmentGrade.TwoStar:
                {
                    equipment.scalingCoefficient = basedCoefficient * (1 * equipmentScaling[(int)equipment.rarity]);
                }
                break;
            case EquipmentGrade.ThreeStar:
                {
                    equipment.scalingCoefficient = basedCoefficient * (1 * equipmentScaling[(int)equipment.rarity]);
                }
                break;
            case EquipmentGrade.FourStar:
                {
                    equipment.scalingCoefficient = basedCoefficient * (1 * equipmentScaling[(int)equipment.rarity]);
                }
                break;
            case EquipmentGrade.FiveStar:
                {

                    equipment.scalingCoefficient = basedCoefficient * (1 * equipmentScaling[(int)equipment.rarity]);
                }
                break;
            default:
                equipment.scalingCoefficient = equipmentScaling[(int)equipment.rarity];
                break;
        }

        equipment.armorRating = Mathf.CeilToInt((equipment.armorRating * (1 + equipment.scalingCoefficient)));
        equipment.movementPenalty = Mathf.CeilToInt((equipment.movementPenalty * (1 + equipment.scalingCoefficient)));
        equipment.accuracyBonus = Mathf.CeilToInt((equipment.accuracyBonus * (1 + equipment.scalingCoefficient)));
        equipment.meleeDamage = Mathf.CeilToInt((equipment.meleeDamage * (1 + equipment.scalingCoefficient)));
        equipment.rangeDamage = Mathf.CeilToInt((equipment.rangeDamage * (1 + equipment.scalingCoefficient)));
        equipment.magicDamage = Mathf.CeilToInt((equipment.magicDamage * (1 + equipment.scalingCoefficient)));
    }
}


