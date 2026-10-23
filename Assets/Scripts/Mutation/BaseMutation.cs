using Unity.VisualScripting;
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
public abstract class BaseMutation
{
    public abstract MutationID ID { get; }
    public abstract string name { get; }
    public abstract BodySlots slot { get; }
    public abstract MutationStrain strain { get; }

    [Header("Improve Stage Progression")]
    public abstract int currentStage { get; }
    public bool IsMaxStage => currentStage < 10; // The number of stages

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
    public bool IsMaxStage => currentStage < 10; // The number max Number of Stages;

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
        
    }
}