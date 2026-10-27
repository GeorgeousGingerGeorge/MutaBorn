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
public enum AccessoryCategory 
{
    None, 
    Carried,
    Worn,
    Functional

   
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
public enum EquipmentGenerationType 
{
    Random,
    Fixed,
    Unique
}
