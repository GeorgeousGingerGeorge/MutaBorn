using Unity.Collections.Tests.CoreCLR.TestJobs;
using UnityEngine;

[CreateAssetMenu(fileName = "ConsumableData", menuName = "Scriptable Objects/ConsumableData")]
public class ConsumableData : ItemData
{
    private ConsumableType _consumable = ConsumableType.None;
    private bool _consumeOnUse;

    private float _magnitude;
    private int _damage;
    private int _coverValue; 


    public ConsumableType consumable => _consumable;
    public bool consumeOnUse => _consumeOnUse;
    public float magnitude => _magnitude;
    public int damage => _damage;
    public int coverValue => _coverValue; 
    
}
