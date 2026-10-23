using UnityEngine;

[System.Flags]
public enum BodySlots 
{
    Head = 0, 
    TorsoFront = 1 << 0,
    TorsoBack = 1 << 1, 
    LeftArm = 1 << 2,
    RightArm = 1 << 3,
    LeftLeg = 1 << 4, 
    RightLeg = 1 << 5,
    BothArms = LeftArm | RightArm, 
    BothLegs = LeftLeg | RightLeg
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
    MainHand = 0, //RightHand
    OffHand = 1 << 0, //LeftHand
    ArmorHelmet = 1 << 1,
    ArmorChest = 1 << 2,
    ArmorLeftArm = 1 << 3, 
    ArmorRightArm = 1 << 4,
    ArmorLeftLeg = 1 << 5,
    ArmorRightLeg = 1 << 6, 

    TwoHanded = MainHand | OffHand,
    ArmorArms = ArmorLeftArm | ArmorRightArm,
    ArmorLegs = ArmorLeftLeg | ArmorRightLeg
}