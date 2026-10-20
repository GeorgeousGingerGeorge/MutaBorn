
using UnityEngine;
public class UnitStats : MonoBehaviour
{
    public enum DynamicStat
    {
        Vitality,
        Constitution,
        Strength,
        Perception,
        Intelligence,
        Dexterity
    }

    private int dynVitality;
    private int dynConstitution;
    private int dynStrength;
    private int dynPerception;
    private int dynIntelligence;
    private int dynDexterity;

    private int availableDynamicPoints = 10;
    public int AvailableDynamicPoints => availableDynamicPoints;

    [Header("Flat Stats - Starting Values")]
    [SerializeField, Min(1)] private int baseHealth = 100;
    [SerializeField, Min(1)] private int baseMana = 1;
    [SerializeField, Min(1)] private int baseMeleeDamage = 1;
    [SerializeField, Min(1)] private int rangeDamage = 1;
    [SerializeField, Min(1)] private int magicDamage = 1;
    [SerializeField, Min(1)] private int baseArmor = 1;
    [SerializeField, Min(1)] private int baseWill = 1;
    [SerializeField, Min(1)] private int baseMovement = 1;
    [SerializeField, Min(1)] private int visionLOS = 45;
    [SerializeField, Min(1)] private int actionPoints = 20;


    //Public access to Dynamic Stats
    public int DynVitality => dynVitality;
    public int DynConstitution => dynConstitution;
    public int DynStrength => dynStrength;
    public int DynPerception => dynPerception;
    public int DynIntelligence => dynIntelligence;
    public int DynDexterity => dynDexterity;


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

    //Calculated stats

    public int CalculatedMaxHealth => baseHealth + (dynVitality * 15);

    public int MaxMana => baseMana + (dynIntelligence * 10);

    public int MeleeDamage => baseMeleeDamage + (dynStrength * 20) + (dynDexterity * 10);

    public int FinalRangeDamage => rangeDamage + (dynPerception * 15) + (dynDexterity * 15);

    public int FinalMagicDamage => magicDamage + (dynIntelligence * 15);

    public int FinalArmor => baseArmor + (dynConstitution * 25) + (dynStrength * 25);

    public int Will => baseWill + (dynIntelligence * 20);

    public int MaxActionPoints => actionPoints + (dynVitality * 20);

    public float VisionAngle => visionLOS + (dynPerception * 25);

    public float DetectionChance => (dynPerception * 1f) + (dynIntelligence * 2f);

    public bool AllocateDynamicStat(DynamicStat stat)
    {
        if (availableDynamicPoints <= 0)
            return false;

        switch (stat)
        {
            case DynamicStat.Vitality:
                dynVitality++;
                break;

            case DynamicStat.Constitution:
                dynConstitution++;
                break;

            case DynamicStat.Strength:
                dynStrength++;
                break;

            case DynamicStat.Perception:
                dynPerception++;
                break;

            case DynamicStat.Intelligence:
                dynIntelligence++;
                break;

            case DynamicStat.Dexterity:
                dynDexterity++;
                break;

            default:
                return false;
        }

        availableDynamicPoints--;
        return true;
    }

    public bool RefundDynamicStat(DynamicStat stat)
    {
        switch (stat)
        {
            case DynamicStat.Vitality:
                if (dynVitality <= 0) return false;
                dynVitality--;
                break;

            case DynamicStat.Constitution:
                if (dynConstitution <= 0) return false;
                dynConstitution--;
                break;

            case DynamicStat.Strength:
                if (dynStrength <= 0) return false;
                dynStrength--;
                break;

            case DynamicStat.Perception:
                if (dynPerception <= 0) return false;
                dynPerception--;
                break;

            case DynamicStat.Intelligence:
                if (dynIntelligence <= 0) return false;
                dynIntelligence--;
                break;

            case DynamicStat.Dexterity:
                if (dynDexterity <= 0) return false;
                dynDexterity--;
                break;

            default:
                return false;
        }

        availableDynamicPoints++;
        return true;
    }
    public void ResetProgression()
    {
        dynVitality = 0;
        dynConstitution = 0;
        dynStrength = 0;
        dynPerception = 0;
        dynIntelligence = 0;
        dynDexterity = 0;
        availableDynamicPoints = 10;
    }

}
