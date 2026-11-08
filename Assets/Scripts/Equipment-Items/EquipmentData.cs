using System;
using UnityEngine;
using MutaBorn.Gameplay;


[CreateAssetMenu(fileName = "EquipmentData", menuName = "Scriptable Objects/EquipmentData")]
public class EquipmentData : ItemData
{
   
    [Header("ItemInforamtion- equipmentType/Grade/Scalers")]
    [SerializeField] private ArmorCategory armor = ArmorCategory.None;
    [SerializeField] private WeaponCategory weapon = WeaponCategory.None;
    [Header("Weapon Subtype")]
    [SerializeField] private WeaponSubtype weaponSubtype = WeaponSubtype.None;
    [Header("Weapon Visuals")]
    [SerializeField] private GameObject _weaponModelPrefab;
    [SerializeField] private AccessoryCategory accessory = AccessoryCategory.None; 
    // Legacy serialized bit flags remain for old instances and backward compatibility.
    // Choose the visible Equipment Fit instead for new templates.
    [SerializeField, HideInInspector] private EquipmentSlots targetSlot;
    [Header("Unit Loadout Slots")]
    [Tooltip("Hand accepts either Left Hand or Right Hand; includes shields, weapons, spellbooks, and other held gear. Auto reads legacy asset flags.")]
    [SerializeField] private EquipmentFitType equipmentFit = EquipmentFitType.Auto;
    [Tooltip("A two-handed item appears in one hand and blocks the other. Requires Hand fit.")]
    [SerializeField] private bool requiresBothHands;
    [Header("Item Display - UI Only")]
    [Tooltip("Player-facing category, e.g. Sword, Bow, Magic, Shield, Chest Armor. Does not affect slot compatibility or combat.")]
    [SerializeField] private string displayTypeLabel;
    public string DisplayTypeLabel => displayTypeLabel;

    [SerializeField] private EquipmentGrade currentRarity;
    [SerializeField] private EquipmentGenerationType type;
    [SerializeField] private EquipmentStatBonus statModifier; // Legacy attribute tags.
    [Header("Starting Attribute Scaling (E = 0%, S = 10% per point)")]
    [SerializeField] private EquipmentAttributeScaling startingScaling;

    [Header("Guaranteed Actions")]
    [SerializeField] private ActionDefinition[] guaranteedActions = new ActionDefinition[0];

    [Header("Guaranteed Effects")]
    [SerializeField] private EffectGrant[] guaranteedEffects = new EffectGrant[0];

    [Header("Weapon Damage")]
    [Tooltip("Default damage type for an action without an explicit damage type when this weapon is its source.")]
    [SerializeField] private DamageType primaryDamageType = DamageType.Unspecified;
    [SerializeField] private DamageComponent[] guaranteedBonusDamage = new DamageComponent[0];

    [Header("Star Reward Generation")]
    [Tooltip("Enable for templates with configured reward pools. Existing templates remain compatible when disabled.")]
    [SerializeField] private bool generateStarRewards;
    [SerializeField] private EquipmentAbilityOption[] possibleAbilities = new EquipmentAbilityOption[0];
    [SerializeField] private EquipmentBonusOption[] possibleBonuses = new EquipmentBonusOption[0];

    [Header("Legacy Shared Grants (Optional)")]
    [SerializeField] private ActionSet grantedActionSet;
    [SerializeField] private EffectCollection grantedEffectCollection;



    [Header("StatValues")]

    [SerializeField] private int _armorRating;
    [SerializeField, Min(0)] private int _wardRating;

    [Header("Damage Type Resistances / Weaknesses")]
    [Tooltip("Percent per damage type. Positive = resistance, negative = weakness. Unlisted types have 0%. Values stack across equipment slots.")]
    [SerializeField] private DamageResistance[] resistances = new DamageResistance[0];
    [SerializeField] private int _movementPenalty; //additional movement cost depends on 
    [SerializeField] private int _meleeDamage;
    [SerializeField] private int _rangeDamage;
    [SerializeField] private int _magicDamage;
    [SerializeField] private int _accuracyBonus;

    [Header("RandomModifiers- Min(StatValues were set too)")]
    [SerializeField] private Vector2Int _meleeDamageRange = new Vector2Int();
    [SerializeField] private Vector2Int _distanceDamageRange = new Vector2Int();
    [SerializeField] private Vector2Int _magicDamageRange = new Vector2Int();
    [SerializeField] private Vector2Int _armorRatingRange = new Vector2Int();
    [SerializeField] private Vector2Int _movementPenaltyRange = new Vector2Int();
    [SerializeField] private Vector2Int _accuracyRange = new Vector2Int();


    public Vector2Int meleeDamageRange => _meleeDamageRange;
    public Vector2Int distanceDamageRange => _distanceDamageRange;
    public Vector2Int magicDamageRange => _magicDamageRange;
    public Vector2Int armorRatingRange => _armorRatingRange;
    public Vector2Int movementPenaltyRange => _movementPenaltyRange;
    public Vector2Int accuracyRange => _accuracyRange;


    [Header("Access To Equipments Detail")]

    public ArmorCategory armorGear => armor; 
    public WeaponCategory weaponGear => weapon;
    public WeaponSubtype WeaponSubtypeId => weaponSubtype;
    public AccessoryCategory accessoryGear => accessory;
    public EquipmentSlots slot => targetSlot;
    public EquipmentFitType EquipmentFit
    {
        get
        {
            if (equipmentFit != EquipmentFitType.Auto)
            {
                return equipmentFit;

            }

            if (accessory != AccessoryCategory.None)
            {
                return EquipmentFitType.Accessory;

            }

            if (weapon != WeaponCategory.None ||
                targetSlot == EquipmentSlots.MainHand ||
                targetSlot == EquipmentSlots.OffHand ||
                targetSlot == EquipmentSlots.TwoHanded)
            {
                return EquipmentFitType.Hand;

            }

            switch (targetSlot)
            {
                case EquipmentSlots.ArmorHelmet:
                    return EquipmentFitType.Head;
                case EquipmentSlots.ArmorChest:
                    return EquipmentFitType.Chest;
                case EquipmentSlots.ArmorArms:
                    return EquipmentFitType.Gloves;
                case EquipmentSlots.ArmorLegs:
                    return EquipmentFitType.Pants;
                default:
                    // Ambiguous old masks need an explicit fit, not an arbitrary slot.
                    return EquipmentFitType.Auto;

            }

        }

    }
    public bool RequiresBothHands => EquipmentFit == EquipmentFitType.Hand &&
        (requiresBothHands || targetSlot == EquipmentSlots.TwoHanded);
    public EquipmentGrade rarity => currentRarity;
    public EquipmentGenerationType genType => type;
    public EquipmentStatBonus statMod => statModifier;
    public EquipmentAttributeScaling StartingScaling => startingScaling;
    public int armorRating => _armorRating;
    public int wardRating => _wardRating;
    public DamageResistance[] Resistances => resistances;
    public int movementPenalty => _movementPenalty;
    public int meleeDamage => _meleeDamage;
    public int rangeDamage => _rangeDamage;
    public int magicDamage => _magicDamage;
    public int accuracyBonus => _accuracyBonus;
    public ActionSet GrantedActionSet => grantedActionSet;
    public EffectCollection GrantedEffectCollection => grantedEffectCollection;
    public ActionDefinition[] GuaranteedActions => guaranteedActions;
    public EffectGrant[] GuaranteedEffects => guaranteedEffects;
    public DamageType PrimaryDamageType => primaryDamageType;
    public DamageComponent[] GuaranteedBonusDamage => guaranteedBonusDamage;
    public bool GenerateStarRewards => generateStarRewards;
    public EquipmentAbilityOption[] PossibleAbilities => possibleAbilities;
    public EquipmentBonusOption[] PossibleBonuses => possibleBonuses;
    public GameObject weaponModelPrefab => _weaponModelPrefab;

}

[Serializable]
public class EquipmentAbilityOption
{
    public ActionDefinition action;
    [Min(0f)] public float weight = 1f;

}

public enum EquipmentBonusKind
{
    Stat = 0,
    DamageComponent = 1,
    Resistance = 2,
    Effect = 3

}

public enum EquipmentRollStat
{
    Armor = 0,
    Ward = 1,
    MeleeDamage = 2,
    RangedDamage = 3,
    MagicDamage = 4

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

}
