using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentDataBase", menuName = "Scriptable Objects/EquipmentDataBase")]
public class EquipmentDataBase : ScriptableObject
{
    [Header("EquipmentData Lists by type")]
    [SerializeField] private List<EquipmentData> _weapons = new List<EquipmentData>();
    [SerializeField] private List<EquipmentData> _armors = new List<EquipmentData>();
    //[SerializeField] List<EquipmentData> accessories = new List<EquipmentData>();

    [Header("Public Access to the list")]
    public IReadOnlyList<EquipmentData> armors => _armors;
    public IReadOnlyList<EquipmentData> weapons => _weapons;

}
