
using UnityEngine;
using MutaBorn.Gameplay;

public enum MutationID 
{
    none,

    //Brute Strain

    //Vile Strain
    Vampirism,
    AssassinClaws,
    //Eldritch Strain
    LaunchExplosive,
    //MindBore Strain

    //Warden Strain
    Sage,

    // Fixed evolution nodes keep stable IDs across save files.
    VampirismII = 5,
    VampirismIII = 6,
    BloodHunter = 7,
    BioLauncher = 8,
    CloakSkin = 9,
    DodgeJet = 10,
    HealingHands = 11,

    // Biological mutation roster. Never renumber serialized IDs.
    OsteodermalCarapace = 12,
    MyotendinousOverdrive = 13,
    BiomineralExtrusion = 14,
    VibrosensoryLattice = 15,
    MyxospindleGlands = 16,
    HyperreflexiveArc = 17,
    PneumaticOsteospines = 18,
    HypertrophicGrasp = 19,
    MyostatinSuppression = 20,
    TendonElasticity = 21,
    CorticalDensification = 22,
    HematopoieticExpansion = 23,
    NeuralMyelination = 24,
    SensoryHypertrophy = 25,
    OsteodermalCarapaceII = 26,
    OsteodermalCarapaceIII = 27,
    MyotendinousOverdriveII = 28,
    MyotendinousOverdriveIII = 29,
    BiomineralExtrusionII = 30,
    BiomineralExtrusionIII = 31,
    VibrosensoryLatticeII = 32,
    VibrosensoryLatticeIII = 33,
    MyxospindleGlandsII = 34,
    MyxospindleGlandsIII = 35,
    HyperreflexiveArcII = 36,
    HyperreflexiveArcIII = 37,
    PneumaticOsteospinesII = 38,
    PneumaticOsteospinesIII = 39,
    HypertrophicGraspII = 40,
    HypertrophicGraspIII = 41,
    MyostatinSuppressionII = 42,
    MyostatinSuppressionIII = 43,
    TendonElasticityII = 44,
    TendonElasticityIII = 45,
    CorticalDensificationII = 46,
    CorticalDensificationIII = 47,
    HematopoieticExpansionII = 48,
    HematopoieticExpansionIII = 49,
    NeuralMyelinationII = 50,
    NeuralMyelinationIII = 51,
    SensoryHypertrophyII = 52,
    SensoryHypertrophyIII = 53,
    SageII = 54,
    SageIII = 55
}
[System.Serializable]
public class MutationBonus 
{
    // Core stat bonuses
    public int strength;
    public int dexterity;
    public int vitality;
    public int constitution;
    public int perception;
    public int intelligence;

    // Derived stat bonuses
    public int health;
    public int mana;
    public int actionPoints;
    public int meleeDamage;
    public int rangeDamage;
    public int magicDamage;
    public int armor;
    public int will;
}
public abstract class BaseMutation : IMutationCombatReaction
{
    public abstract MutationID ID { get; }
    public abstract string name { get; }
    public abstract BodySlots slot { get; }
    public abstract MutationStrain strain { get; }

    [Header("Improve Stage Progression")]
    public abstract int currentStage { get; }
    public abstract bool IsMaxStage { get;  } // The number of stages
    public abstract MutationBonus bonus{ get; }

    public abstract void OnEquipped(UnitController unit);
    public abstract void OnRemoved(UnitController unit);

    public virtual void OnDamageDealt(UnitController owner, CombatDamageEvent damageEvent)
    {

    }

    public virtual void OnDamageReceived(UnitController owner, CombatDamageEvent damageEvent)
    {

    }
}

public class VampirismMutation : BaseMutation 
{
    public override MutationID ID => MutationID.Vampirism;
    public override string name => "Blood Siphon";
    public override BodySlots slot => BodySlots.TorsoFront;
    public override MutationStrain strain => MutationStrain.Vile;

    [Header("Improve Stage Progression")]
    public override int currentStage => 1;
    public override bool IsMaxStage => currentStage >= 5; // The number max Number of Stages;
    public override MutationBonus bonus => currentStage switch
    {
        1 => new MutationBonus 
        {strength = 1, dexterity = 2, vitality = 2, constitution = 0, perception = 2, intelligence = 1, 
            health = 20, mana = 15, actionPoints = 5, meleeDamage = 5, rangeDamage = 3, magicDamage = 8, armor = 2, will = 10},
        2 => new MutationBonus 
        {strength = 2, dexterity = 3, vitality = 3, constitution = -1, perception = 3, intelligence = 2, 
            health = 35, mana = 25, actionPoints = 10, meleeDamage = 10, rangeDamage = 6, magicDamage = 16, armor = 4, will = 12},
        3 => new MutationBonus 
        {strength = 1, dexterity = 2, vitality = 2, constitution = 0, perception = 2, intelligence = 1, 
            health = 20, mana = 15, actionPoints = 5, meleeDamage = 5, rangeDamage = 3, magicDamage = 8, armor = 2, will = 10},
        4 => new MutationBonus 
        {strength = 1, dexterity = 2, vitality = 2, constitution = 0, perception = 2, intelligence = 1, 
            health = 20, mana = 15, actionPoints = 5, meleeDamage = 5, rangeDamage = 3, magicDamage = 8, armor = 2, will = 10},
        _ => new MutationBonus()
    };

    public override void OnEquipped(UnitController unit) 
    {
        Debug.Log($"Equipping {name}. Health bonus: {bonus.health}");
        unit.data.ApplyMutationBonuses(bonus, 1); //One determines whether all the stats would be removed or added
    }
    public override void OnRemoved(UnitController unit) 
    {
        unit.data.ApplyMutationBonuses(bonus, -1);
    }
    public void VampireHeal(UnitController unit) 
    {
        unit.data.ModifyHealth(5);
    }

    public override void OnDamageDealt(UnitController owner, CombatDamageEvent damageEvent)
    {
        if (owner == null || owner.data == null || damageEvent.ActualDamage <= 0)
        {
            return;

        }

        // Keep the legacy reaction as a fallback until the new mutation effect
        // has been verified in Unity, but never process both for the same hit.
        UnitEffectSource effectSource = owner.GetComponent<UnitEffectSource>();
        if (effectSource != null && effectSource.HasMutationEffects(ID))
        {
            return;

        }

        int healthBefore = owner.data.currentHealth;
        VampireHeal(owner);
        int restoredHealth = owner.data.currentHealth - healthBefore;

        Debug.Log($"{owner.data.characterName}'s {name} activated: restored {restoredHealth} HP.", owner);

    }
}
public class AssassinClawMutation : BaseMutation
{
    public override MutationID ID => MutationID.AssassinClaws;
    public override string name => "Scissor Hands";
    public override BodySlots slot => BodySlots.BothArms;
    public override MutationStrain strain => MutationStrain.Vile;

    [Header("Improve Stage Progression")]
    public override int currentStage => 1;
    public override bool IsMaxStage => currentStage >= 5; // The number max Number of Stages;
    public override MutationBonus bonus => currentStage switch
    {
        1 => new MutationBonus
        {
            strength = 2,
            dexterity = 2,
            vitality = 1,
            constitution = 0,
            perception = 0,
            intelligence = 0,
            health = 15,
            mana = 10,
            actionPoints = 15,
            meleeDamage = 8,
            rangeDamage = 0,
            magicDamage = 5,
            armor = 5,
            will = 0
        },
        2 => new MutationBonus
        {
            strength = 3,
            dexterity = 3,
            vitality = 2,
            constitution = 0,
            perception = -1,
            intelligence = -1,
            health = 17,
            mana = 5,
            actionPoints = 18,
            meleeDamage = 10,
            rangeDamage = 0,
            magicDamage = 7,
            armor = 8,
            will = 0
        },
        3 => new MutationBonus
        {
            strength = 4,
            dexterity = 3,
            vitality = 2,
            constitution = 1,
            perception = -1,
            intelligence = -1,
            health = 17,
            mana = 0,
            actionPoints = 20,
            meleeDamage = 14,
            rangeDamage = 0,
            magicDamage = 10,
            armor = 12,
            will = 0
        },
        4 => new MutationBonus
        {
            strength = 4,
            dexterity = 4,
            vitality = 3,
            constitution = 2,
            perception = -2,
            intelligence = -2,
            health = 20,
            mana = -5,
            actionPoints = 25,
            meleeDamage = 20,
            rangeDamage = 0,
            magicDamage = 12,
            armor = 17,
            will = 0
        },
        _ => new MutationBonus()
    };

    public override void OnEquipped(UnitController unit)
    {
        unit.data.ApplyMutationBonuses(bonus, 1); //One determines whether all the stats would be removed or added
        //Delegates for damage should go here
    }
    public override void OnRemoved(UnitController unit)
    {
        unit.data.ApplyMutationBonuses(bonus, -1);
    }
    public void LaceratedSkirmish(UnitController unit,UnitController targetUnit) //Would like to add action type as a last parameter
    {
        int totalDamage = Mathf.Clamp((targetUnit.data.armor - (unit.data.meleeDamage + 10)), 5, targetUnit.data.maxHealth);
        totalDamage *= -1; 

        targetUnit.data.ModifyHealth(totalDamage);
        
    }
}