using UnityEngine;
using System.Collections.Generic;


public class UnitData
{

    [Header("Character Information- Name/RaceName")]

    public string characterName;
    public string raceName;

    [Header("Character Information- startingStats")]
    public int startingStrength;
    public int startingDexterity;
    public int startingVitality;
    public int startingConstitution;
    public int startingPerception;
    public int startingIntelligence;

    [Header("Character Inforamtion- CoreStats/LevelUpStats")]

    public int leveledStrength { get; private set; }
    public int leveledDexterity { get; private set; }
    public int leveledVitality { get; private set; }
    public int leveledConstitution { get; private set; }
    public int leveledPerception { get; private set; }
    public int leveledIntelligence { get; private set; }

    [Header("Character Information- MutationBonus")]
    public int mutationStrength;
    public int mutationDexterity;
    public int mutationVitality;
    public int mutationConstitution;
    public int mutationPerception;
    public int mutationIntelligence;

    [Header("Character Information- EquipmentBonus")]
    public int equipmentStrength;
    public int equipmentDexterity;
    public int equipmentVitality;
    public int equipmentConstitution;
    public int equipmentPerception;
    public int equipmentIntelligence;

    [Header("Character Information- Total Core Stats")]
    public int totalStrength => leveledStrength + startingStrength + mutationStrength + equipmentStrength;
    public int totalDexterity => leveledDexterity + startingDexterity + mutationDexterity + equipmentDexterity;
    public int totalVitality => leveledVitality + startingVitality + mutationVitality + equipmentVitality;
    public int totalConstitutio => leveledConstitution + startingConstitution + mutationConstitution + equipmentConstitution;
    public int totalPerception => leveledPerception + startingPerception + mutationPerception + equipmentPerception;
    public int totalIntelligence => leveledIntelligence + startingIntelligence + mutationIntelligence + equipmentIntelligence;

    [Header("Character Information- Equipped Mutations/Gear Plus Tracker")]
    Dictionary<BodySlots, BaseMutation> activeMutations;
    Dictionary<BodySlots, Dictionary<MutationStrain, Dictionary<MutationID, MutationProgress>>> mutationTracker;
    Dictionary<BodySlots, EquipmentSlots> equippedItems;

    [Header("Character Information- Current Status (mutationBonus)")]
    public int mutationHealth;
    public int mutationMana;
    public int mutationActionPoints;
    public int mutationMeleeDamage;
    public int mutationRangeDamage;
    public int mutationMagicDamage;
    public int mutationArmor;
    public int mutationWill;
    
    [Header("Character Information- Current Status (equipmentBonus)")]
    public int equipmentHealth;
    public int equipmentMana;
    public int equipmentActionPoints;
    public int equipmentMeleeDamage;
    public int equipmentRangeDamage;
    public int equipmentMagicDamage;
    public int equipmentArmor;
    public int equipmentWill;

    [Header("Character Information- Current Status")]
    public int maxHealth { get; private set; }
    public int currentHealth { get; private set; }
    public int maxMana { get; private set; }
    public int currentMana { get; private set; }
    public int maxActionPoints { get; private set; }
    public int currentActionPoints { get; private set; }
    public int meleeDamage { get; private set; }
    public int rangeDamage { get; private set; }
    public int magicDamage { get; private set; }
    public int armor { get; private set; }
    public int will { get; private set; }


    public void GetMaxHealth(DynamicStats race)
    {
        maxHealth = Mathf.CeilToInt((float)race.baseHealth + ((float)totalVitality * ((float)race.vitalityHealthBonus * race.vitalityModifier)) 
            + (float)mutationHealth + (float)equipmentHealth);
    }
    public void GetMaxMana(DynamicStats race)
    {
        maxMana = Mathf.CeilToInt((float)race.baseMana + ((float)totalIntelligence * ((float)race.intelligenceManaBonus * race.intelligenceModifier)) 
            + (float) mutationMana + (float)equipmentMana);

    }
    public void GetMaxActionPoints(DynamicStats race)
    {
        maxActionPoints = Mathf.CeilToInt((float)race.actionPoints + ((float)totalVitality * ((float)race.vitalityActionPointBonus * race.vitalityModifier)) 
            + (float)mutationActionPoints + (float)equipmentActionPoints);

    }
    public void GetMeleeDamage(DynamicStats race) 
    {
        meleeDamage = Mathf.CeilToInt((float)race.meleeDamage + ((float)totalStrength * ((float)race.strengthMeleeDMGBonus * race.strengthModifier))
        + ((float)totalDexterity * ((float)race.dexterityMeleeDMGBonus * race.dexterityModifier)) + (float)mutationMeleeDamage + (float)equipmentMeleeDamage);
    }
    public void GetRangeDamage(DynamicStats race) 
    {
        rangeDamage = Mathf.CeilToInt((float)race.meleeDamage + ((float)totalPerception * ((float)race.perceptionRangeDMGBonus * race.perceptionModifier))
        + ((float)totalDexterity * ((float)race.dexterityRangeDMGBonus * race.perceptionModifier)) + (float)mutationRangeDamage + (float)equipmentRangeDamage);
    }
    public void GetMagicDamage(DynamicStats race) 
    {
        magicDamage = Mathf.CeilToInt((float)race.meleeDamage + ((float)totalIntelligence* ((float)race.intelligenceMagicDMGBonus * race.intelligenceModifier))
     + (float)mutationMagicDamage + (float)equipmentMagicDamage);
    }

    public void GetArmor(DynamicStats race) 
    {
    
    }
    public void GetWill(DynamicStats race) 
    {
    
    }

    //Initializes the unit
    public static UnitData CreateNewUnit(string name,DynamicStats race) 
    {
        //Setting Character name and race name for search later
        UnitData newUnit = new UnitData();
        newUnit.characterName = name;
        newUnit.raceName = race.name;

       

        //Setting Starting Stats 
        newUnit.startingStrength = race.startStrength;
        newUnit.startingDexterity = race.startDexterity;
        newUnit.startingVitality = race.startVitality;
        newUnit.startingConstitution = race.startConstitution;
        newUnit.startingPerception = race.startPerception;
        newUnit.startingIntelligence = race.startIntelligence;

        newUnit.GetMaxHealth(race);
        newUnit.GetMaxMana(race);
        newUnit.GetMaxActionPoints(race);

        return newUnit; 
        
    }
}
