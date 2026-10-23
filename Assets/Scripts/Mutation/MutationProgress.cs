using UnityEngine;

public class MutationProgress
{
    public MutationID ID;
    public int currentStage = 1; 
    public int currentPoints;
    public int PointsRequired;


    public void SetStage(int stage) 
    {
        currentStage = stage; 
    }
}
