using UnityEngine;
using MutaBorn.Gameplay;

public enum ConsumableUseKind
{
    LegacyEffect = 0,
    RestoreHealth = 1,
    RestoreActionPoints = 2,
    ApplyStatus = 3,
    TargetedAttack = 4,
    CaptureUnit = 5

}

[CreateAssetMenu(fileName = "ConsumableData", menuName = "Scriptable Objects/ConsumableData")]
public class ConsumableData : ItemData
{
    [SerializeField] private ConsumableType _consumable = ConsumableType.Eat;
    // Retained for existing assets; use counts now determine when an item is exhausted.
    [SerializeField, HideInInspector] private bool _consumeOnUse = true;

    [SerializeField] private float _magnitude;
    [SerializeField] private int _damage;
    [SerializeField] private int _coverValue;

    [Header("Action Menu")]
    [Tooltip("The ActionDefinition displayed when this consumable is carried. Must be an active Items action.")]
    [SerializeField] private ActionDefinition useAction;

    [Header("Combat Use")]
    [SerializeField, Min(1)] private int maxUses = 1;
    // Retained for existing assets. Consumables no longer cost AP.
    #pragma warning disable CS0414
    [SerializeField, HideInInspector] private int apCost = 5;
    #pragma warning restore CS0414
    [SerializeField] private EffectGrant onUseEffect = new EffectGrant();
    [Header("New Consumable Behavior")]
    [SerializeField] private ConsumableUseKind useKind = ConsumableUseKind.LegacyEffect;
    [SerializeField, Min(0)] private int restoreHealth;
    [SerializeField, Min(0)] private int restoreActionPoints;
    [SerializeField] private StatusDefinition appliedStatus;
    [Tooltip("Throwing knife: restore every charge after this item scores a killing blow.")]
    [SerializeField] private bool refillOnKill;
    [Tooltip("Boomerang: spend AP on the attack, not an item charge.")]
    [SerializeField] private bool usesActionPointsOnly;


    public ConsumableType consumable => _consumable;
    public bool consumeOnUse => _consumeOnUse;
    public float magnitude => _magnitude;
    public int damage => _damage;
    public int coverValue => _coverValue;
    public ActionDefinition UseAction => useAction;
    public int MaxUses => Mathf.Max(1, maxUses);
    // Legacy API: consumables now use their remaining uses instead of AP.
    public int ApCost => 0;
    public EffectGrant OnUseEffect => onUseEffect;
    public ConsumableUseKind UseKind => useKind;
    public int RestoreHealth => Mathf.Max(0, restoreHealth);
    public int RestoreActionPoints => Mathf.Max(0, restoreActionPoints);
    public StatusDefinition AppliedStatus => appliedStatus;
    public bool RefillOnKill => refillOnKill;
    public bool UsesActionPointsOnly => usesActionPointsOnly;
    
}
