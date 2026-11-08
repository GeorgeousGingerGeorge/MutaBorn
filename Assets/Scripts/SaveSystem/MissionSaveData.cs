using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class MissionSaveData
{
    public int saveVersion = 1;

    public string sceneName;
    public string saveTimeUTC;

    public List<UnitSaveData> units = new List<UnitSaveData>();

    public List<WorldObjectSaveData> worldObjects =
        new List<WorldObjectSaveData>();

    public TurnSaveData turnState = new TurnSaveData();
}

[Serializable] public class UnitSaveData
{
    public string unitId;
    public string persistentId; // Save system Identity
    public string unitName;
    public string raceName;

    public Vector3 position;
    public Quaternion rotation;

    public int floorId;
    public Vector2Int nodeCoords;

    public int currentHealth;
    public int currentMana;
    public int currentActionPoints;

    public bool isActive;
}

[Serializable] public class WorldObjectSaveData
{
    public string persistentId;

    public Vector3 position;
    public bool isActive;
}

[Serializable] public class TurnSaveData
{
    public int currentRound;
    public string activeFactionId;

    public int activeTurnIndex;

    public List<string> factionTurnOrder = new List<string>();

    public List<FactionInitiativeSaveData> initiativeRolls =
        new List<FactionInitiativeSaveData>();
}

[Serializable]
public class FactionInitiativeSaveData
{
    public string factionId;
    public int initiativeRoll;
}