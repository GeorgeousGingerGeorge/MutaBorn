
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
    public override bool IsMaxStage => currentStage >= 10; // The number max Number of Stages;
    public override MutationBonus bonus => currentStage switch
    {
        1 => new MutationBonus { }
    };

    public override void OnEquipped(UnitController unit) 
    {
        switch (currentStage) 
        {
            case 1:
                { 
                       
                }
                break;
        }
    }
    public override void OnRemoved(UnitController unit) 
    {
        switch (currentStage)
        {
            case 1:
                {

                }
                break;
        }
    }
    public void VampireHeal(UnitController unit) 
    {
        unit.data.ModifyHealth(5);
    }
}