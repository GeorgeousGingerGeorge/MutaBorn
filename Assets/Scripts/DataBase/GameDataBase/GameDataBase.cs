using UnityEngine;

[CreateAssetMenu(fileName = "GameDataBase", menuName = "Scriptable Objects/GameDataBase")]
public class GameDataBase : ScriptableObject
{
    [Header("Databases")]

    [SerializeField] private MutationDataBase mutationDataBase;
    [SerializeField] private RaceDataBase raceDataBase;
    [SerializeField] private EquipmentDataBase equipmentDataBase;

    [Header("Actions")]

    [Tooltip("Registry of all action definitions.")]
    [SerializeField] private ActionDataBase actionDataBase;

    [Tooltip("Actions available to every deployed unit.")]
    [SerializeField] private ActionSet universalActionSet;


    public MutationDataBase mutationData => mutationDataBase;
    public RaceDataBase raceData => raceDataBase;
    public EquipmentDataBase equipData => equipmentDataBase;

    public ActionDataBase actionData => actionDataBase;
    public ActionSet universalActions => universalActionSet;
}
