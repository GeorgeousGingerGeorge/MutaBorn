using UnityEngine;

[CreateAssetMenu(fileName = "UnitFlatStats", menuName = "Scriptable Objects/UnitFlatStats")]
public class UnitFlatStats : ScriptableObject
{
    [Header("Flat Stats - Starting Values")]
    [SerializeField, Min(1)] private int baseHealth = 50;
    [SerializeField, Min(1)] private int baseMana = 20;
    [SerializeField, Min(1)] private int actionPoints = 20;
    [SerializeField, Min(1)] private int baseMeleeDamage = 10;
    [SerializeField, Min(1)] private int rangeDamage = 10;
    [SerializeField, Min(1)] private int magicDamage = 10;
    [SerializeField, Min(1)] private int baseArmor = 10;
    [SerializeField, Min(1)] private int baseWill = 100;
    [SerializeField, Min(1)] private int LineOfSight_Distance= 15;
    [SerializeField, Min(1)] private int LineOfSight_Angle= 360;


    [Header("Public Flat Stats")]
    //Public access to Flat Stats
    public int BaseHealth => baseHealth;
    public int BaseMana => baseMana;
    public int BaseMeleeDamage => baseMeleeDamage;
    public int RangeDamage => rangeDamage;
    public int MagicDamage => magicDamage;
    public int BaseArmor => baseArmor;
    public int BaseWill => baseWill;
    public int VisionLOS_Distance => LineOfSight_Distance;
    public int VisionLOS_Angle => LineOfSight_Angle;
    public int ActionPoints => actionPoints;
}
