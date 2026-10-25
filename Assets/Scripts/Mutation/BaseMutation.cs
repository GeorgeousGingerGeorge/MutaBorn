
using UnityEngine;

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
    Sage
}
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
public abstract class BaseMutation
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