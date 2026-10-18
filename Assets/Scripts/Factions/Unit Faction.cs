using UnityEngine;



public class UnitFaction : MonoBehaviour
{
    [SerializeField] private FactionData faction;

    public FactionData Faction => faction;

    public void SetFaction(FactionData newFaction)
    {
        faction = newFaction;

    }

}
