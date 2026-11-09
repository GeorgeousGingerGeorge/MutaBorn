using System;
using UnityEngine;

public enum SoundType 
{
    Footsteps,
    Running,
    Scuffle,
    Explosion,
    Impact, 
    Interaction
}
[System.Serializable] //For future ScriptableObject Data
public class SoundAwareness 
{
    public GridNode soundOrigin;
    public SoundType soundType;
    public int loudness; //Maximum Grid Movement Cost at which this sound can be heard 

    public int pathCost; //Grid path cost from this AI Unit to sound origin

    public SoundAwareness(GridNode origin, SoundType type, int soundLoudness, int distance)
    {
        soundOrigin = origin;
        soundType = type;
        loudness = soundLoudness;
        pathCost = distance;
    }

    public void Clear() 
    {
        soundOrigin = null;
        pathCost = 0;
        loudness = 0;
    }

}
