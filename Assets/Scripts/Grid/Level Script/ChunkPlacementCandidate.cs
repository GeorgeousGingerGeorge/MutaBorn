using UnityEngine;

[System.Serializable]
public class ChunkPlacementCandidate
{
    [Header("Available Connections")]
    public ChunkSocketInstance sourceSocket;

    [Header("Generation Weight")]
    public float currentWeight;

    [Header("Generation Depth")]
    public int generationDepth;

    [Header("Placement Condition")]
    public bool isPlaced; 

    public ChunkPlacementCandidate(ChunkSocketInstance socket, float weight, int depth) 
    {
        sourceSocket = socket;
        currentWeight = weight;
        generationDepth = depth;
        isPlaced = false;
    }

    public bool IsAvailable => !isPlaced && sourceSocket != null && !sourceSocket.isConnected;

    public void ApplyInfluence(float influence) 
    {
        currentWeight = Mathf.Max(0f, currentWeight - Mathf.Max(0f, influence));

    }

    public void Placed() 
    {
        isPlaced = true; 
    }
}
