using UnityEngine;



public class FactionDebugTester : MonoBehaviour
{
    [SerializeField] private FactionManager manager;
    [SerializeField] private FactionData factionA;
    [SerializeField] private FactionData factionB;



    //Context menu is the 3 dots on the side of the inspector module I showed everyone week 2
    //First select 2 factions to test with comands
    //To test select the object with the tester script, on the scripts module press the stacked 3 dots,
    //on the bottom of the popup menu you will see a factions option,
    //go to that menu and you will see all tests registered here!
    [ContextMenu("Factions / Print Relationship")]
    private void PrintRelationship()
    {
        Debug.Log($"{factionA.DisplayName} <-> {factionB.DisplayName}: " + manager.GetRelation(factionA, factionB), this);
    }

    [ContextMenu("Factions / Declare War")]
    private void DeclareWar()
    {
        Debug.Log($"Declare War changed relationship: {manager.DeclareWar(factionA, factionB)}", this);

    }

    [ContextMenu("Factions / Make Peace")]
    private void MakePeace()
    {
        Debug.Log($"Make Peace changed relationship: {manager.MakePeace(factionA, factionB)}", this);

    }

    [ContextMenu("Factions / Form Alliance")]
    private void FormAlliance()
    {
        Debug.Log($"Form Alliance changed relationship: {manager.FormAlliance(factionA, factionB)}", this);

    }

}
