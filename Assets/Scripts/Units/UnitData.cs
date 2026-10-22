using UnityEngine;
using System.Collections.Generic;
public class UnitData
{
    
    
    [Header("Character Information- Name/RaceName")]

    public string characterName;
    public string raceName;

    [Header("Character Inforamtion- CoreStats/LevelUpStats")]


    public int leveledStrength { get; private set; }
    public  int leveledDexterity { get; private set; }
    public  int leveledVitality {get; private set;}
    public  int leveledConstitution {get; private set;}
    public  int leveledPerception {get; private set;}
    public int leveledIntelligence {get; private set;}

    public int totalStrength;// => leveledStrength + racedata.baseStrength;
    public int totalDexterity;// => leveledDexterity + racedata.baseDexterity;
    public int totalVitality;// => leveledVitality + racedata.baseVitality;
    public int totalConstitution;// => leveledConstitution + racedata.baseConstitution;
    public int totalPerception;// => leveledPerception + racedata.basePerception;
    public int totalIntelligence;// => leveledIntelligence + racedata.baseIntelligence;

    [Header("Character Information- Equipped Mutations/Gear Plus Tracker")]

    Dictionary<BodySlots, BaseMutation> activeMutations;
    Dictionary<BodySlots, Dictionary<MutationStrain, Dictionary<MutationID, MutationProgress>>> mutationTracker; 
    Dictionary<BodySlots, EquipmentSlots> equippedItems;
}
