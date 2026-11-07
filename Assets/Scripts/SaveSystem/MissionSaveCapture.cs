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

            UnitSaveData savedUnit = new UnitSaveData
            {
                unitId = unit.data.UnitId,
                unitName = unit.data.characterName,
                raceName = unit.data.raceName,

                position = unit.transform.position,
                rotation = unit.transform.rotation,

                floorId = unit.currentNode != null ? unit.currentNode.floorID : -1,

                currentHealth = unit.data.currentHealth,
                currentMana = unit.data.currentMana,
                currentActionPoints = unit.data.currentActionPoints,

                isActive = unit.gameObject.activeSelf
            };

            snapshot.units.Add(savedUnit);
        }

        return snapshot;
    }
}