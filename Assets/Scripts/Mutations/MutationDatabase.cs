using UnityEngine;

public static class MutationDatabase
{
    //private static Dictionary<string, DynamicStats> raceData = new Dictionary<string, DynamicStats>();

    //private static Dictionary<MutationID, int> mutationBaseCost = new Dictionary<MutationID, int>(); 

    //private static void InitializeDataBase()
    //{
    //      raceData.Clear();
    //      mutationBaseCost.Clear();
    //      
    //      DynamicStats[] raceStats = Resources.LoadAll<DynamicStats>("Races")
    //      
    //      foreach(DynamicStats race in raceStats)
    //      {
    //          raceData[race.name] = race; 
    //      } 
    //      
    //      
    //
    //
    //}
    public static BaseMutation CreateInstance(MutationID ID) 
    {
        switch (ID) 
        {
            case MutationID.Vampirism:
                return new VampirismMutation();
                break;
            case MutationID.AssassinClaws:
                break;
            case MutationID.LaunchExplosive:
                break;
            case MutationID.Sage:
                break;
            default:
                break;
        }
        return null;
    }
}
