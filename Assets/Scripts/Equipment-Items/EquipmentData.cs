using UnityEngine;

public enum ArmorCategory
{
    None,
    Light,
    Medium,
    Heavy
}
public enum WeaponCategory 
{
    None,
    Melee,
    Range,
    Magic
}
public enum EquipmentGrade
{
    None,
    OneStar,
    TwoStar,
    ThreeStar,
    FourStar,
    FiveStar

}

[System.Flags]
public enum EquipmentStatBonus 
{
    None = 0, 
    Strength = 1 << 0,
    Dexterity = 1 << 1,
    Vitality = 1 << 2, 
    Constitution = 1 << 3,
    Perception = 1 << 4, 
    Intelligence = 1 << 5
}
[CreateAssetMenu(fileName = "EquipmentData", menuName = "Scriptable Objects/EquipmentData")]
public class EquipmentData : ScriptableObject
{
    [Header("ItemInformation")]
    [SerializeField]public string itemName;
    [SerializeField]public ArmorCategory armor= ArmorCategory.None; 
    [SerializeField]public WeaponCategory weapon = WeaponCategory.None; 
    [SerializeField]public EquipmentSlots targetSlot;
    [SerializeField]public EquipmentGrade currentRarity;
    [SerializeField]public EquipmentStatBonus currentStat; 

    [Header("StatModifiers")]

    [SerializeField]public int armorRating;
    [SerializeField]public int movementPenalty; //additional movement cost depends on 
    [SerializeField]public int meleeDamage;
    [SerializeField]public int rangeDamage;
    [SerializeField]public int magicDamage;
    [SerializeField]public int accuracy;


}
