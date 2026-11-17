using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameDataBase", menuName = "Scriptable Objects/GameDataBase")]
public class GameDataBase : ScriptableObject
{
    [Header("Databases")]

    [SerializeField] private MutationDataBase mutationDataBase;
    [SerializeField] private RaceDataBase raceDataBase;
    [SerializeField] private List<EquipmentType> equipmentTypes = new List<EquipmentType>();

    [SerializeField] private WeaponCombinationCatalog weaponCombinationCatalog;
    [SerializeField] private Material[] SelectionCircle;
    [Header("Actions")]

    [Tooltip("Registry of all action definitions.")]
    [SerializeField] private ActionDataBase actionDataBase;

    [Tooltip("Actions available to every deployed unit.")]
    [SerializeField] private ActionSet universalActionSet;

    public MutationDataBase mutationData => mutationDataBase;
    public RaceDataBase raceData => raceDataBase;
    public WeaponCombinationCatalog weaponCombos => weaponCombinationCatalog;

    public ActionDataBase actionData => actionDataBase;
    public ActionSet universalActions => universalActionSet;
    public Material[] selectionCircle => SelectionCircle;

    public EquipmentType GetEquipmentType(string typeId)
    {
        if (string.IsNullOrWhiteSpace(typeId))
        {
            return null;
        }

        foreach (EquipmentType equipmentType in equipmentTypes)
        {
            if (equipmentType != null && equipmentType.TypeId == typeId)
            {
                return equipmentType;
            }
        }

        return null;
    }
}
