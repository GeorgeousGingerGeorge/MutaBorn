using UnityEngine;

[System.Serializable]
public class ChunkPlacementCandidate
{
    [Header("Available Connections")]
    public ChunkSocketInstance sourceSocket;

    [Header("Generation Weight")]
    public float currentWeight;

    [Header("Placement Condition")]
    public bool isPlaced; 

    public ChunkPlacementCandidate(ChunkSocketInstance socket, float weight) 
    {
        sourceSocket = socket;
        currentWeight = weight;
        isPlaced = false;
    }

    public bool IsAvailable => !isPlaced && sourceSocket != null && !sourceSocket.isConnected && sourceSocket.AllowedConnections != SocketConnectionType.None;

    public void ApplyInfluence(float influence) 
    {
        currentWeight = Mathf.Max(0f, currentWeight - Mathf.Max(0f, influence));

    }

    public void Placed() 
    {
        isPlaced = true; 
    }
}
