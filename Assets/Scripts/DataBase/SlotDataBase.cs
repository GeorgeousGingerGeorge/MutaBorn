using UnityEngine;

[System.Flags]
public enum BodySlots 
{
    None = 0 ,
    Head = 1 << 0, 
    TorsoFront = 1 << 1,
    TorsoBack = 1 << 2, 
    LeftArm = 1 << 3,
    RightArm = 1 << 4,
    LeftLeg = 1 << 5, 
    RightLeg = 1 << 6,
    BothArms = LeftArm | RightArm, 
    BothLegs = LeftLeg | RightLeg,
    Chest = TorsoFront | TorsoBack
}


public enum MutationStrain 
{
    Brute, 
    Vile, 
    Eldritch,
    MindBore,
    Warden
}

[System.Flags]
public enum EquipmentSlots 
{
    None,
    MainHand = 1 << 0, //RightHand
    OffHand = 1 << 1, //LeftHand
    ArmorHelmet = 1 << 2,
    ArmorChest = 1 << 3,
    ArmorLeftArm = 1 << 4, 
    ArmorRightArm = 1 << 5,
    ArmorLeftLeg = 1 << 6,
    ArmorRightLeg = 1 << 7, 

    TwoHanded = MainHand | OffHand,
    ArmorArms = ArmorLeftArm | ArmorRightArm,
    ArmorLegs = ArmorLeftLeg | ArmorRightLeg
}