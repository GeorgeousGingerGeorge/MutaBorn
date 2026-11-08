using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissionSaveCapture
{
    public static MissionSaveData Capture()
    {
        MissionSaveData snapshot = new MissionSaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            saveTimeUTC = DateTime.UtcNow.ToString("O")
        };

        UnitController[] units = UnityEngine.Object.FindObjectsByType<UnitController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (UnitController unit in units)
        {
            if (unit.data == null)
                continue;

            MissionUnitIdentity identity = unit.GetComponent<MissionUnitIdentity>();

            string persistentId = identity != null ? identity.PersistentId : "";

            if (string.IsNullOrEmpty(persistentId) && unit.InitializationMode != UnitInitializationMode.LegacyScene)
            {
                persistentId = $"CrewSlot_{unit.RosterDeploymentSlot}"; // 	EnemySpawn_01:group0:unit0 for enemies (& civ) - CrewSlot_0 for our players chars
            }

            UnitSaveData savedUnit = new UnitSaveData
            {
                unitId = unit.data.UnitId,
                persistentId = persistentId,
                unitName = unit.data.characterName,
                raceName = unit.data.raceName,

                position = unit.transform.position,
                rotation = unit.transform.rotation,

                floorId = unit.currentNode != null ? unit.currentNode.floorID : -1,
                nodeCoords = unit.currentNode != null ? unit.currentNode.coords : Vector2Int.zero,

                currentHealth = unit.data.currentHealth,
                currentMana = unit.data.currentMana,
                currentActionPoints = unit.data.currentActionPoints,

                isActive = unit.gameObject.activeSelf
            };

            snapshot.units.Add(savedUnit);
        } // END of the unit foreach loop

        // Capture current turn state
        TurnManager turnManager = TurnManager.Instance;

        if (turnManager != null)
        {
            snapshot.turnState.currentRound = turnManager.Round;

            snapshot.turnState.activeFactionId = turnManager.ActiveFaction != null ? turnManager.ActiveFaction.Id : "";
        }

        return snapshot;
    } // END of Capture()
} // END of MissionSaveCapture class
