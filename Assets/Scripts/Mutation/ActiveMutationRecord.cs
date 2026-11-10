using System;
using UnityEngine;

[Serializable]
public sealed class ActiveMutationRecord
{
    [SerializeField] private MutationID mutationId;
    [Min(0)]
    [SerializeField] private int progressPoints;

    public MutationID MutationId => mutationId;
    public int ProgressPoints => progressPoints;

    public ActiveMutationRecord(MutationID id)
    {
        mutationId = id;

    }

    public void AddProgress(int points)
    {
        if (points > 0)
        {
            progressPoints += points;

        }

    }

    internal void EvolveTo(MutationID next)
    {
        mutationId = next;
        progressPoints = 0;

    }

}
