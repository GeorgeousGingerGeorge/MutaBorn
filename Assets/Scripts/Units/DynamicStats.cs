using UnityEngine;

[CreateAssetMenu(fileName = "DynamicStats", menuName = "Scriptable Objects/DynamicStats")]
public class DynamicStats : ScriptableObject
{
    [Header("Base Character Flat Stats")]
    [SerializeField] public UnitFlatStats flatStats;

    [Header("Core(Dynamic)-Stats")]
    private int baseStrength = 1;
    private int baseDexterity = 1;
    private int baseVitality = 1;
    private int baseConstitution = 1;
    private int basePerception = 1;
    private int baseIntelligence = 1;

    [Header("Core(Dynamic) stat bonuses")]
    public int strengthArmorBonus { get; private set; } = 10;
    public int strengthMeleeDMGBonus { get; private set; } = 15;
    public int dexterityMeleeDMGBonus { get; private set; } = 10;
    public int dexterityRangeDMGBonus { get; private set; } = 10;
    public int vitalityHealthBonus { get; private set; } = 10;
    public int vitalityActionPointBonus { get; private set; } = 2;
    public int constitutionArmorBonus { get; private set; } = 20;
    public float ailmentResistance { get; private set; } = 0.1f; //caps 75%
    public int perceptionRangeDMGBonus { get; private set; } = 20;
    public float perceptionDetectionBonus { get; private set; } = 0.05f; //caps 75%
    public int intelligenceManaBonus { get; private set; } = 10;
    public int intelligenceMagicDMGBonus { get; private set; } = 20;
    public float intelligenceDetectionBonus { get; private set; } = 0.02f;


    [Header("Core(Dynamic) Stat- Modifiers ")]
    [Range(0.3f, 2.0f)]
    [SerializeField] private float strengthModifier = 1.0f;
    [Range(0.3f, 2.0f)]
    [SerializeField] private float dexterityModifier = 1.0f;
    [Range(0.3f, 2.0f)]
    [SerializeField] private float vitalityModifier = 1.0f;
    [Range(0.3f, 2.0f)]
    [SerializeField] private float constitutionModifier = 1.0f;
    [Range(0.3f, 2.0f)]
    [SerializeField] private float perceptionModifier = 1.0f;
    [Range(0.3f, 2.0f)]
    [SerializeField] private float intelligenceModifier = 1.0f;


    [Header("Racial Modifiers")]
    [SerializeField] public int healthModifier;
    [SerializeField] public int manaModifier;
    [SerializeField] public int actionPointsModifier;
    [SerializeField] public int baseMeleeModifier;
    [SerializeField] public int baseRangeModifier;
    [SerializeField] public int baseMagicModifier;
    [SerializeField] public int baseArmorModifier;
    [SerializeField] public int baseWillModifier;


    [Header("DisplayValues (readOnly")]
    [SerializeField] private int _BaseHealth;

    [SerializeField] private int _BaseMana;


    [SerializeField] private int _ActionPoints;


    [SerializeField] private int _MeleeDamage;


    [SerializeField] private int _RangeDamage;


    [SerializeField] private int _MagicDamage;

    [SerializeField] private int _ArmorDef;


    [SerializeField] private int _Will;


    [Header("BaseCharacter-FlatStats Changed")]


    public int baseHealth => _BaseHealth;

    public int baseMana => _BaseMana;


    public int actionPoints => _ActionPoints;


    public int meleeDamage => _MeleeDamage;


    public int rangeDamage => _RangeDamage;


    public int magicDamage => _MagicDamage;

    public int armorDef => _ArmorDef;


    public int will => _Will;

    private void OnValidate()
    {
        if (flatStats == null)
            return;
        _BaseHealth = flatStats.BaseHealth + healthModifier;
        _BaseMana = flatStats.BaseMana + manaModifier;
        _ActionPoints = flatStats.ActionPoints + actionPointsModifier;
        _MeleeDamage = flatStats.BaseMeleeDamage + baseMeleeModifier;
        _RangeDamage = flatStats.RangeDamage + baseRangeModifier;
        _MagicDamage = flatStats.MagicDamage + baseMagicModifier;
        _ArmorDef = flatStats.BaseArmor + baseArmorModifier;
        _Will = flatStats.BaseWill + baseWillModifier;

    }

}

