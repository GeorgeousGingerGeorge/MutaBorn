using UnityEngine;

[CreateAssetMenu(fileName = "DynamicStats", menuName = "Scriptable Objects/DynamicStats")]
public class DynamicStats : ScriptableObject
{

    [Header("Race Information")]
    [SerializeField] private string raceName;
    [SerializeField] private string objectName;

    public string RaceName => raceName;
    public string ObjectName => objectName;


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
    [SerializeField] private float _strengthModifier = 1.0f;
    [Range(0.3f, 2.0f)]
    [SerializeField] private float _dexterityModifier = 1.0f;
    [Range(0.3f, 2.0f)]
    [SerializeField] private float _vitalityModifier = 1.0f;
    [Range(0.3f, 2.0f)]
    [SerializeField] private float _constitutionModifier = 1.0f;
    [Range(0.3f, 2.0f)]
    [SerializeField] private float _perceptionModifier = 1.0f;
    [Range(0.3f, 2.0f)]
    [SerializeField] private float _intelligenceModifier = 1.0f;


    [Header("Racial Modifiers")]
    [SerializeField] public int healthModifier;
    [SerializeField] public int manaModifier;
    [SerializeField] public int actionPointsModifier;
    [SerializeField] public int baseMeleeModifier;
    [SerializeField] public int baseRangeModifier;
    [SerializeField] public int baseMagicModifier;
    [SerializeField] public int baseArmorModifier;
    [SerializeField] public int baseWillModifier;


    [Header("DisplayValues (readOnly)")]
    [SerializeField] private int _BaseHealth;

    [SerializeField] private int _BaseMana;


    [SerializeField] private int _ActionPoints;


    [SerializeField] private int _MeleeDamage;


    [SerializeField] private int _RangeDamage;


    [SerializeField] private int _MagicDamage;

    [SerializeField] private int _ArmorDef;


    [SerializeField] private int _Will;


    [Header("Racial Actions")]
    [Tooltip("Actions automatically granted to units of this race, in addition to the universal action set.")]
    [SerializeField] private ActionSet racialActionSet;

    public ActionSet RacialActions => racialActionSet;

    
    [Header("BaseCharater- Modifiers")]
    public float strengthModifier => _strengthModifier;
    public float dexterityModifier => _dexterityModifier;
    public float vitalityModifier => _vitalityModifier;
    public float constitutionModifier => _constitutionModifier;
    public float perceptionModifier => _perceptionModifier;
    public float intelligenceModifier => _intelligenceModifier;
    [Header("BaseCharacter- CoreStats (read-only)")]
    public int startStrength => baseStrength;
    public int startDexterity => baseDexterity;
    public int startVitality => baseVitality;
    public int startConstitution => baseConstitution;
    public int startPerception => basePerception;
    public int startIntelligence => baseIntelligence;

    [Header("BaseCharacter-FlatStats Changed (read-only)")]


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

