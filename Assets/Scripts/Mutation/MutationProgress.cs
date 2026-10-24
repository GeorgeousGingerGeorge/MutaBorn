using UnityEngine;

public class MutationProgress
{

    [Header("Mutation Information- Type")]
    public MutationID ID { get; private set; }

    [Header("Mutation Information- Progression")]
    public int currentStage { get; private set; } = 0;
    public int currentPoints { get; private set; }

    [Header("Mutation Information- Requirements")]
    public int initialCost { get; private set; }
    public int pointsRequired { get; private set; }

    public float multiplier { get; private set; }

    

    public MutationProgress(MutationID id, int initialCost, float multiplier) 
    {
        ID = id;
        this.initialCost = initialCost;
        this.multiplier = multiplier;

        CalculatePointsRequired();
    }


    private void CalculatePointsRequired() 
    {
        int count = 0; 
        pointsRequired = initialCost;

        while (count < currentStage) 
        {
            pointsRequired = Mathf.CeilToInt(pointsRequired * multiplier);
        }
    }
    public void AddProgress(int points) 
    {
        if (points > 0) 
        {
            currentPoints += points;
        }
    }

    public void SetPointsRequired(int points) 
    {
        pointsRequired = Mathf.Max(1, points);
    }
    public void SetStage(int stage) 
    {
        currentStage = Mathf.Clamp(stage, 0, 10);
        CalculatePointsRequired();
    }
}
