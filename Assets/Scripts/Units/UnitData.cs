using UnityEngine;
using System.Collections.Generic;



public class UnitData
{

    [Header("Character Information- Name/RaceName")]

    public string characterName;
    public string raceName;
    DynamicStats race;

    [Header("Character Information- startingStats")]
    private int startingStrength;
    private int startingDexterity;
    private int startingVitality;
    private int startingConstitution;
    private int startingPerception;
    private int startingIntelligence;

    [Header("Character Inforamtion- CoreStats/LevelUpStats")]

    public int leveledStrength { get; private set; }
    public int leveledDexterity { get; private set; }
    public int leveledVitality { get; private set; }
    public int leveledConstitution { get; private set; }
    public int leveledPerception { get; private set; }
    public int leveledIntelligence { get; private set; }

    [Header("Character Information- MutationBonus")]
    public int mutationStrength { get; set; }
    public int mutationDexterity { get; set; }
    public int mutationVitality { get; set; }
    public int mutationConstitution { get; set; }
    public int mutationPerception { get; set; }
    public int mutationIntelligence { get; set; }

    [Header("Character Information- EquipmentBonus")]
    public int equipmentStrength { get; set; }
    public int equipmentDexterity { get; set; }
    public int equipmentVitality { get; set; }
    public int equipmentConstitution { get; set; }
    public int equipmentPerception { get; set; }
    public int equipmentIntelligence { get; set; }

    [Header("Character Information- Total Core Stats")]
    public int totalStrength => leveledStrength + startingStrength + mutationStrength + equipmentStrength;
    public int totalDexterity => leveledDexterity + startingDexterity + mutationDexterity + equipmentDexterity;
    public int totalVitality => leveledVitality + startingVitality + mutationVitality + equipmentVitality;
    public int totalConstitution => leveledConstitution + startingConstitution + mutationConstitution + equipmentConstitution;
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

    //Local StatCalculations for the CurrentStatus
    public void CalcMaxHealth(DynamicStats race)
    {
        maxHealth = Mathf.CeilToInt((float)race.baseHealth + ((float)totalVitality * ((float)race.vitalityHealthBonus * race.vitalityModifier)) 
            + (float)mutationHealth + (float)equipmentHealth);
    }
    public void CalcMaxMana(DynamicStats race)
    {
        maxMana = Mathf.CeilToInt((float)race.baseMana + ((float)totalIntelligence * ((float)race.intelligenceManaBonus * race.intelligenceModifier)) 
            + (float) mutationMana + (float)equipmentMana);

    }
    public void CalcMaxActionPoints(DynamicStats race)
    {
        maxActionPoints = Mathf.CeilToInt((float)race.actionPoints + ((float)totalVitality * ((float)race.vitalityActionPointBonus * race.vitalityModifier)) 
            + (float)mutationActionPoints + (float)equipmentActionPoints);

    }
    public void CalcMeleeDamage(DynamicStats race) 
    {
        meleeDamage = Mathf.CeilToInt((float)race.meleeDamage + ((float)totalStrength * ((float)race.strengthMeleeDMGBonus * race.strengthModifier))
        + ((float)totalDexterity * ((float)race.dexterityMeleeDMGBonus * race.dexterityModifier)) + (float)mutationMeleeDamage + (float)equipmentMeleeDamage);
    }
    public void CalcRangeDamage(DynamicStats race) 
    {
        rangeDamage = Mathf.CeilToInt((float)race.rangeDamage + ((float)totalPerception * ((float)race.perceptionRangeDMGBonus * race.perceptionModifier))
        + ((float)totalDexterity * ((float)race.dexterityRangeDMGBonus * race.perceptionModifier)) + (float)mutationRangeDamage + (float)equipmentRangeDamage);
    }
    public void CalcMagicDamage(DynamicStats race) 
    {
        magicDamage = Mathf.CeilToInt((float)race.magicDamage + ((float)totalIntelligence * ((float)race.intelligenceMagicDMGBonus * race.intelligenceModifier))
     + (float)mutationMagicDamage + (float)equipmentMagicDamage);
    }

    public void CalcArmor(DynamicStats race) 
    {
        armor = Mathf.CeilToInt((float)race.armorDef + ((float)totalConstitution * ((float)race.intelligenceMagicDMGBonus * race.intelligenceModifier))
            + (float)mutationArmor + (float)equipmentMagicDamage);
    }
    public void CalcWill(DynamicStats race) 
    {
        will = race.will + mutationWill + equipmentWill;
    }

    //Takes both negative/positive values for health
    //if negative you lose the values. if positive you restore the value
    public void ModifyHealth(int amount) 
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);

    }
    public void ModifyMana(int amount) 
    {
        currentMana = Mathf.Clamp(currentHealth + amount, 0, maxMana);

    }
    public void ModifyActionPoints(int amount) 
    {
        currentActionPoints = Mathf.Clamp(currentActionPoints + amount, 0, maxActionPoints);

    }

    public void ApplyMutationBonuses(MutationBonus bonus, int multiplier) 
    {
        if (bonus == null)
            return;

        //CoreBonus
        mutationStrength += bonus.strength * multiplier;
        mutationDexterity += bonus.dexterity * multiplier;
        mutationVitality += bonus.vitality * multiplier;
        mutationConstitution += bonus.constitution * multiplier;
        mutationPerception += bonus.perception * multiplier;
        mutationIntelligence += bonus.intelligence * multiplier;

        //DerivedBonus 
        mutationHealth += bonus.health * multiplier;
        mutationMana += bonus.mana * multiplier;
        mutationActionPoints += bonus.actionPoints * multiplier;
        mutationMeleeDamage += bonus.meleeDamage * multiplier;
        mutationRangeDamage += bonus.rangeDamage * multiplier;
        mutationMagicDamage += bonus.magicDamage * multiplier;
        mutationArmor += bonus.armor * multiplier;
        mutationWill += bonus.will * multiplier;


        RecalculateStats();

    }


    public void RecalculateStats() 
    {
        CalcMaxHealth(race);
        CalcMaxMana(race);
        CalcMaxActionPoints(race);
        CalcMeleeDamage(race);
        CalcRangeDamage(race);
        CalcMagicDamage(race);
        CalcArmor(race);
        CalcWill(race);

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        currentActionPoints = Mathf.Clamp(currentActionPoints, 0, maxActionPoints);
    }


    //Initializes the unit
    public static UnitData CreateNewUnit(string name = "harry",DynamicStats race = null) 
    {
        //Setting Character name and race name for search later
        UnitData newUnit = new UnitData();
        newUnit.characterName = name;
        newUnit.raceName = race.name;
        newUnit.race = race; 
       

        //Setting Starting Stats 
        newUnit.startingStrength = race.startStrength;
        newUnit.startingDexterity = race.startDexterity;
        newUnit.startingVitality = race.startVitality;
        newUnit.startingConstitution = race.startConstitution;
        newUnit.startingPerception = race.startPerception;
        newUnit.startingIntelligence = race.startIntelligence;

        newUnit.CalcMaxHealth(race);
        newUnit.CalcMaxMana(race);
        newUnit.CalcMaxActionPoints(race);
        newUnit.CalcMeleeDamage(race);
        newUnit.CalcRangeDamage(race);
        newUnit.CalcMagicDamage(race);
        newUnit.CalcArmor(race);
        newUnit.CalcWill(race);

        newUnit.currentHealth = newUnit.maxHealth;
        newUnit.currentMana = newUnit.maxMana;
        newUnit.currentActionPoints = newUnit.maxActionPoints;
        return newUnit; 
        
    }
}
