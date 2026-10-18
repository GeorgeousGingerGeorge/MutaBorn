
using UnityEngine;

public enum LabStat{ Strength,Defense,Vitality }
public enum LabRace { Human, Orc, Elf }
public enum LabClass { Warrior, Scout, Mage }

public class UnitStats : MonoBehaviour
{
    [Header("Base stats before allocated points")]
    [SerializeField, Min(1)] private int baseStrength = 10;
    [SerializeField, Min(0)] private int baseDefense = 5;
    [SerializeField, Min(0)] private int baseVitality = 10;

    //New variables
    private int dynVitality;
    private int dynConstitution;
    private int dynStrength;
    private int dynPerception;
    private int dynIntelligence;
    private int dynDexterity;

    //Dynamic Stat Point Allocation
    private int availableDynamicPoints = 10;

    public int AvailableDynamicPoints => availableDynamicPoints;

    public enum DynamicStat
    {
        Vitality,
        Constitution,
        Strength,
        Perception,
        Intelligence,
        Dexterity

    }

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

    //Old Variables
    private int strengthPoints;
    private int defensePoints;
    private int vitalityPoints;
    private bool swordEquipped;
    private bool mutationActive;
    private float buffEndsAt;

    public int Level { get; private set; } = 1;
    public int AvailablePoints { get; private set; } = 10;
    public int BaseStrength => baseStrength;
    public int BaseDefense => baseDefense;
    public int BaseVitality => baseVitality;
    public int StrengthPoints => strengthPoints;
    public int DefensePoints => defensePoints;
    public int VitalityPoints => vitalityPoints;
    public bool SwordEquipped => swordEquipped;
    public bool MutationEquipped => mutationActive;
    public bool BuffActive => Time.time < buffEndsAt;
    public float BuffSecondsLeft => Mathf.Max(0f, buffEndsAt - Time.time);

    public int SwordStrength => SwordEquipped ? 5 : 0;
    public int SwordDefense => SwordEquipped ? 2 : 0;

    public float StrengthPercent => (BuffActive ? 0.20f : 0f) + (mutationActive ? 0.20f : 0f);
    public float Strength => (baseStrength + strengthPoints + SwordStrength) * (1f + StrengthPercent);
    public int Defense => baseDefense + defensePoints + SwordDefense;
    public int Vitality => baseVitality + vitalityPoints;
    public int MaxHealth => Vitality * 10;

    public int AttackDamage => Mathf.CeilToInt(Strength * 2f);

    //Test 1: Dynamic Stat Allocation
    [ContextMenu("Test Dynamic Stat Allocation")]
    private void TestDynamicStatAllocation()
    {
        Debug.Log($"Starting points: {AvailableDynamicPoints}");
        Debug.Log($"Starting melee damage: {MeleeDamage}");

        bool allocated = AllocateDynamicStat(DynamicStat.Strength);

        Debug.Log($"Allocation successful: {allocated}");
        Debug.Log($"Points after allocation: {AvailableDynamicPoints}");
        Debug.Log($"Melee damage after allocation: {MeleeDamage}");

        bool refunded = RefundDynamicStat(DynamicStat.Strength);

        Debug.Log($"Refund successful: {refunded}");
        Debug.Log($"Points after refund: {AvailableDynamicPoints}");
        Debug.Log($"Melee damage after refund: {MeleeDamage}");
    }


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
    public bool Allocate(LabStat stat)
    {
        if (AvailablePoints <= 0) return false;

        switch (stat)
        {
            case LabStat.Strength:
                strengthPoints++;
                break;

            case LabStat.Defense:
                defensePoints++;
                break;

            case LabStat.Vitality:
                vitalityPoints++;
                break;

            default:
                return false;
        }

        AvailablePoints--;
        return true;
    }

    public bool Refund(LabStat stat)
    {
        switch (stat)
        {
            case LabStat.Strength:
                if (strengthPoints <= 0) return false;
                strengthPoints--;
                break;

            case LabStat.Defense:
                if (defensePoints <= 0) return false;
                defensePoints--;
                break;

            case LabStat.Vitality:
                if (vitalityPoints <= 0) return false;
                vitalityPoints--;
                break;

            default:
                return false;
        }

        AvailablePoints++;
        return true;
    }


    public void LevelUp()
    {
        Level++;
        AvailablePoints += 3;
    }

    public void SetSword(bool equipped)
    {
        swordEquipped = equipped;
    }

    public void ApplyBuff()
    {
        buffEndsAt = Time.time + 10f;
    }

    public void RemoveBuff()
    {
        buffEndsAt = 0f;
    }

    public void SetMutation(bool active)
    {
        mutationActive = active;
    }

    public void SetBaseStats(int strength, int defense, int vitality)
    {
        baseStrength = Mathf.Max(1, strength);
        baseDefense = Mathf.Max(0, defense);
        baseVitality = Mathf.Max(1, vitality);
    }

    public void ResetProgression()
    {
        Level = 1;
        AvailablePoints = 10;
        strengthPoints = 0;
        defensePoints = 0;
        vitalityPoints = 0;

        //Reset new dynamic stats
        dynVitality = 0;
        dynConstitution = 0;
        dynStrength = 0;
        dynPerception = 0;
        dynIntelligence = 0;
        dynDexterity = 0;
        availableDynamicPoints = 10;

        //Reset equipment and effects
        swordEquipped = false;
        mutationActive = false;
        RemoveBuff();
    }

    public void ApplyPreset(LabRace race, LabClass unitClass)
    {
        int strength = 8;
        int defense = 3;
        int vitality = 8;

        switch (race)
        {
            case LabRace.Orc:
                strength = 12;
                defense = 4;
                vitality = 10;
                break;

            case LabRace.Elf:
                strength = 6;
                defense = 2;
                vitality = 7;
                break;
        }

        switch (unitClass)
        {
            case LabClass.Warrior:
                strength += 2;
                defense += 2;
                vitality += 2;
                break;

            case LabClass.Scout:
                strength += 3;
                defense += 1;
                vitality += 1;
                break;

            case LabClass.Mage:
                vitality += 2;
                break;
        }

        SetBaseStats(strength, defense, vitality);
        ResetProgression();
    }
} 
