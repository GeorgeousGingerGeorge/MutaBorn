using System;
using UnityEngine;

// Serializable per-attribute ranks. E is zero, S is the maximum.
public enum EquipmentScalingGrade { E, D, C, B, A, S }

[Serializable]
public struct EquipmentAttributeScaling
{
    public EquipmentScalingGrade strength;
    public EquipmentScalingGrade dexterity;
    public EquipmentScalingGrade vitality;
    public EquipmentScalingGrade constitution;
    public EquipmentScalingGrade perception;
    public EquipmentScalingGrade intelligence;

    public EquipmentScalingGrade Get(int index)
    {
        switch (index)
        {
            case 0: return strength;
            case 1: return dexterity;
            case 2: return vitality;
            case 3: return constitution;
            case 4: return perception;
            default: return intelligence;
        }
    }

    public void Set(int index, EquipmentScalingGrade grade)
    {
        switch (index)
        {
            case 0: strength = grade; break;
            case 1: dexterity = grade; break;
            case 2: vitality = grade; break;
            case 3: constitution = grade; break;
            case 4: perception = grade; break;
            default: intelligence = grade; break;
        }
    }

    public static string Name(int index)
    {
        switch (index)
        {
            case 0: return "STR";
            case 1: return "DEX";
            case 2: return "VIT";
            case 3: return "CON";
            case 4: return "PER";
            default: return "INT";
        }
    }

    public static int PercentPerPoint(EquipmentScalingGrade grade)
    {
        return Mathf.Clamp((int)grade, 0, 5) * 2;
    }

    public long TotalPercent(UnitData unit)
    {
        if (unit == null) return 0;
        int[] points = {
            unit.totalStrength, unit.totalDexterity, unit.totalVitality,
            unit.totalConstitution, unit.totalPerception, unit.totalIntelligence
        };
        long percent = 0;
        for (int i = 0; i < points.Length; i++)
            percent += (long)points[i] * PercentPerPoint(Get(i));
        return percent;
    }

    // Relative weights follow the CURRENT ranks, not a fixed random chance.
    // Early low-rank items favor discovering new scalers; mature items favor existing ranks.
    // E receives at least 2% each while other eligible ranks exist.
    public float[] UpgradeChances()
    {
        float[] result = new float[6];
        int totalPoints = 0;
        int eCount = 0;
        float nonEWeight = 0f;
        for (int i = 0; i < 6; i++)
        {
            int grade = (int)Get(i);
            totalPoints += grade;
        }
        for (int i = 0; i < 6; i++)
        {
            int grade = (int)Get(i);
            if (grade == 0) eCount++;
            else if (grade < 5)
            {
                result[i] = grade * (1f + totalPoints / 2f);
                nonEWeight += result[i];
            }
        }
        if (nonEWeight <= 0f)
        {
            if (eCount > 0)
                for (int i = 0; i < 6; i++)
                    if (Get(i) == EquipmentScalingGrade.E) result[i] = 1f / eCount;
            return result;
        }

        float eChance = eCount == 0 ? 0f : Mathf.Max(0.02f, 1f / (eCount + nonEWeight));
        float remaining = 1f - eCount * eChance;
        for (int i = 0; i < 6; i++)
            result[i] = Get(i) == EquipmentScalingGrade.E ? eChance : result[i] / nonEWeight * remaining;
        return result;
    }
}
