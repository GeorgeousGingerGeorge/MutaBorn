#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// One-time editor repair for the serialized GameObject roots of race prefab variants.
// Never runs in Play Mode or in a built game.
public static class MutaBornRepairRaceModelReferences
{
    [MenuItem("Tools/MutaBorn/Repair Race Model References")]
    private static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[MutaBorn] Stop Play Mode before repairing race-model references.");

            return;
        }

        string[] racePaths =
        {
            "Assets/Scriptable Objects/Units/Races/Humans.asset",
            "Assets/Scriptable Objects/Units/Races/Orcs.asset",
            "Assets/Scriptable Objects/Units/Races/Goblins.asset",
            "Assets/Scriptable Objects/Units/Races/Elves.asset",
            "Assets/Scriptable Objects/Units/Races/Dwarfs.asset"
        };

        string[] modelPaths =
        {
            "Assets/Models/Races/playermodel_human.prefab",
            "Assets/Models/Races/playermodel_Orc.prefab",
            "Assets/Models/Races/playermodel_Goblin.prefab",
            "Assets/Models/Races/playermodel_Elf.prefab",
            "Assets/Models/Races/playermodel_Dwarf.prefab"
        };

        int repaired = 0;

        for (int i = 0; i < racePaths.Length; i++)
        {
            DynamicStats race = AssetDatabase.LoadAssetAtPath<DynamicStats>(racePaths[i]);
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(modelPaths[i]);

            if (race == null || prefabRoot == null)
            {
                Debug.LogError($"[MutaBorn] Cannot repair '{racePaths[i]}': race asset or model prefab missing.");

                continue;
            }

            SerializedObject raceSerialized = new SerializedObject(race);
            SerializedProperty modelField = raceSerialized.FindProperty("raceModelPrefab");
            if (modelField == null || modelField.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogError($"[MutaBorn] raceModelPrefab GameObject field missing on '{racePaths[i]}'.");

                continue;
            }

            raceSerialized.Update();
            modelField.objectReferenceValue = prefabRoot;
            raceSerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(race);
            repaired++;

            Debug.Log($"[MutaBorn] Assigned {prefabRoot.name} to {race.name}.", race);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MutaBorn] Race model references repaired: {repaired}/{racePaths.Length}. Review each race asset Inspector.");
    }
}
#endif
