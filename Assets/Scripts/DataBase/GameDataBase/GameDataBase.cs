using UnityEngine;

[CreateAssetMenu(fileName = "GameDataBase", menuName = "Scriptable Objects/GameDataBase")]
public class GameDataBase : ScriptableObject
{
    [SerializeField] private MutationDataBase mutationDataBase;
    [SerializeField] private RaceDataBase raceDataBase;
    [SerializeField] private EquipmentDataBase equipmentDataBase;



    public MutationDataBase mutationData => mutationDataBase;
    public RaceDataBase raceData => raceDataBase;
    public EquipmentDataBase equipData => equipmentDataBase;
}
