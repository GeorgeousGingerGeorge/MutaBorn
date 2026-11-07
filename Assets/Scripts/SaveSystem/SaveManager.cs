using System;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static readonly string savePath = Path.Combine(Application.persistentDataPath, "mission_save.json");
    
    public static bool SaveMission(MissionSaveData data)
    {
        if (data == null)
            return false;

        try
        {
            string json = JsonUtility.ToJson(data, true);

            File.WriteAllText(savePath, json);

            Debug.Log($"Mission saved successfully: {savePath}"); // Shows location of save via console

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to save mission: {exception.Message}"); // Displays error via console

            return false;
        }
    }

    public static bool TryLoadMission(out MissionSaveData data)
    {
        data = null;

        if (!File.Exists(savePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(savePath);

            data = JsonUtility.FromJson<MissionSaveData>(json);

            return data != null;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to load mission: {exception.Message}");

            return false;
        }
    }
    public static bool SaveCurrentMission()
    {
        MissionSaveData snapshot = MissionSaveCapture.Capture();

        return SaveMission(snapshot);
    }
}