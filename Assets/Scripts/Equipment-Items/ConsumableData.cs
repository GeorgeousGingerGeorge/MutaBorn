using Unity.Collections.Tests.CoreCLR.TestJobs;
using UnityEngine;
using MutaBorn.Gameplay;

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
    [SerializeField, HideInInspector] private int apCost = 5;
    [SerializeField] private EffectGrant onUseEffect = new EffectGrant();


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
    
}
