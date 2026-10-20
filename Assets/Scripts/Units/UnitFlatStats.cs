using UnityEngine;

[CreateAssetMenu(fileName = "UnitFlatStats", menuName = "Scriptable Objects/UnitFlatStats")]
public class UnitFlatStats : ScriptableObject
{
    [Header("Flat Stats - Starting Values")]
    [SerializeField, Min(1)] private int baseHealth = 100;
    [SerializeField, Min(1)] private int baseMana = 1;
    [SerializeField, Min(1)] private int baseMeleeDamage = 10;
    [SerializeField, Min(1)] private int rangeDamage = 4;
    [SerializeField, Min(1)] private int magicDamage = 1;
    [SerializeField, Min(1)] private int baseArmor = 1;
    [SerializeField, Min(1)] private int baseWill = 1;
    [SerializeField, Min(1)] private int baseMovement = 1;
    [SerializeField, Min(1)] private int visionLOS = 45;
    [SerializeField, Min(1)] private int actionPoints = 20;


    //Public access to Flat Stats
    public int BaseHealth => baseHealth;
    public int BaseMana => baseMana;
    public int BaseMeleeDamage => baseMeleeDamage;
    public int RangeDamage => rangeDamage;
    public int MagicDamage => magicDamage;
    public int BaseArmor => baseArmor;
    public int BaseWill => baseWill;
    public int BaseMovement => baseMovement;
    public int VisionLOS => visionLOS;
    public int ActionPoints => actionPoints;
}
