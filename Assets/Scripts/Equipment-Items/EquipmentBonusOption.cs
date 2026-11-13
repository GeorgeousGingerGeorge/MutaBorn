using System;
using UnityEngine;
using MutaBorn.Gameplay;



public enum EquipmentBonusKind
{
    Stat = 0,
    DamageComponent = 1,
    Resistance = 2,
    Effect = 3,
    Action = 4,
    ScalingGrade = 5

}

public enum EquipmentRollStat
{
    Armor = 0,
    Ward = 1,
    MeleeDamage = 2,
    RangedDamage = 3,
    MagicDamage = 4,
    Accuracy = 5

}

[Serializable]
public class EquipmentBonusOption
{
    public EquipmentBonusKind kind;
    [Min(0f)] public float weight = 1f;
    public EquipmentRollStat stat;
    public DamageType damageType = DamageType.Unspecified;
    [Tooltip("Inclusive integer range for stats, extra typed damage, and resistance percent.")]
    public Vector2Int amountRange;
    public EffectGrant effect;
    [Header("New template-based upgrade rewards")]
    public string bonusId;
    [Tooltip("Player-facing name shown when this bonus is rolled. Leave empty to show its effect instead.")]
    public string displayName;
    public bool repeatable;
    [Min(1)] public int maxStacks = 1;
    public ActionDefinition action;
    public EquipmentScalingAttribute scalingAttribute;

}
