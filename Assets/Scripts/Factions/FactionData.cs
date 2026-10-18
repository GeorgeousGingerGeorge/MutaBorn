using UnityEngine;



[CreateAssetMenu(fileName = "NewFaction", menuName = "Game/Factions/Faction Data")]
public class FactionData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique, stable identifier, such as 'blue' or 'muta-born'. Do not change it after using it in save data.")]

    [SerializeField] private string factionId = "new-faction";
    [SerializeField] private string displayName = "New Faction";


    [Header("Appearance")]

    [SerializeField] private Color factionColor = Color.white;
    [SerializeField] private Material factionMaterial;


    public string Id => factionId;
    public string DisplayName => displayName;
    public Color Color => factionColor;
    public Material Material => factionMaterial;



    private void OnValidate()
    {
        factionId = (factionId ?? string.Empty).Trim();

    }

}
