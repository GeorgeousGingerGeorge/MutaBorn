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
public enum WeaponSubtype
{
    None = 0,
    Sword = 1,
    Axe = 2,
    Blunt = 3,
    Polearm = 4,
    Dagger = 5,
    Fist = 6,
    Bow = 7,
    Firearm = 8,
    Crossbow = 9,
    Sling = 10,
    DartGun = 11,
    Spellbook = 12,
    Staff = 13,
    Wand = 14,
    Charm = 15,
    Shield = 16,

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

public enum ConsumableType 
{ 
    None, 
    Eat,
    Throw,
    Place
}

