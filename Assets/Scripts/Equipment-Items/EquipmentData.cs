using UnityEngine;
using MutaBorn.Gameplay;


[CreateAssetMenu(fileName = "EquipmentData", menuName = "Scriptable Objects/EquipmentData")]
public class EquipmentData : ItemData
{
   
    [Header("ItemInforamtion- equipmentType/Grade/Scalers")]
    [SerializeField] private ArmorCategory armor = ArmorCategory.None;
    [SerializeField] private WeaponCategory weapon = WeaponCategory.None;
    [SerializeField] private AccessoryCategory accessory = AccessoryCategory.None; 
    [SerializeField] private EquipmentSlots targetSlot;
    [SerializeField] private EquipmentGrade currentRarity;
    [SerializeField] private EquipmentGenerationType type;
    [SerializeField] private EquipmentStatBonus statModifier; //StatModifier

    [Header("Granted Actions")]
    [SerializeField] private ActionSet grantedActionSet;



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
    public AccessoryCategory accessoryGear => accessory;
    public EquipmentSlots slot => targetSlot;
    public EquipmentGrade rarity => currentRarity;
    public EquipmentGenerationType genType => type;
    public EquipmentStatBonus statMod => statModifier;
    public int armorRating => _armorRating;
    public int wardRating => _wardRating;
    public DamageResistance[] Resistances => resistances;
    public int movementPenalty => _movementPenalty;
    public int meleeDamage => _meleeDamage;
    public int rangeDamage => _rangeDamage;
    public int magicDamage => _magicDamage;
    public int accuracyBonus => _accuracyBonus;
    public ActionSet GrantedActionSet => grantedActionSet;


}
