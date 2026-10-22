using UnityEngine;
public class UnitStats : MonoBehaviour
{
    public enum UnitRace
    {
        Human,
        Goblin
    }

    [SerializeField] private UnitRace race = UnitRace.Human;

    public UnitRace Race => race;

    [SerializeField] private UnitFlatStats flatstats;
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

   


    //Public access to Dynamic Stats
    public int DynVitality => dynVitality;
    public int DynConstitution => dynConstitution;
    public int DynStrength => dynStrength;
    public int DynPerception => dynPerception;
    public int DynIntelligence => dynIntelligence;
    public int DynDexterity => dynDexterity;

    //Calculated stats

    public int CalculatedMaxHealth => flatstats.BaseHealth + (dynVitality * 10);

    public int MaxMana => flatstats.BaseMana + (dynIntelligence * 10);

    public int MeleeDamage => flatstats.BaseMeleeDamage + (dynStrength * 20) + (dynDexterity * 10);

    public int FinalRangeDamage => flatstats.RangeDamage + (dynPerception * 15) + (dynDexterity * 15);

    public int FinalMagicDamage => flatstats.MagicDamage + (dynIntelligence * 20);

    public float AilmentResistence => Mathf.Min(dynConstitution * 0.10f, 0.75f);

    public int FinalArmor => flatstats.BaseArmor + (dynConstitution * 25) + (dynStrength * 25);

    public int Will => flatstats.BaseWill;

    public int MaxActionPoints => flatstats.ActionPoints + (dynVitality * 2);

    public float DetectionChance => (dynPerception * 0.05f) + (dynIntelligence * 0.02f);

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
