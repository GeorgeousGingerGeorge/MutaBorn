using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MutaBornRescueKitInstaller
{
    private const string HubScene = "Assets/Scenes/Hub.unity";
    private const string StoragePrefab = "Assets/Prefabs/Player Data/ItemStorage.prefab";
    private const string KitPath = "Assets/Scriptable Objects/Equipment/Items/Rescue Kit.asset";

    [MenuItem("Tools/MutaBorn/Combat/Give 3 Rescue Kits (Hub + Storage Prefab)")]
    private static void SupplyRescueKits()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != HubScene)
        {
            EditorUtility.DisplayDialog("Rescue Kits", "Open Assets/Scenes/Hub.unity first.", "OK");

            return;

        }

        ConsumableData kit = AssetDatabase.LoadAssetAtPath<ConsumableData>(KitPath);
        if (kit == null)
        {
            EditorUtility.DisplayDialog("Rescue Kits", "Install the Rescue Kit asset before running this tool.", "OK");

            return;

        }

        ItemStorage hub = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            hub = root.GetComponentInChildren<ItemStorage>(true);
            if (hub != null)
            {
                break;

            }

        }

        if (hub == null)
        {
            EditorUtility.DisplayDialog("Rescue Kits", "Hub is missing ItemStorage.", "OK");

            return;

        }

        GameObject prefab = PrefabUtility.LoadPrefabContents(StoragePrefab);
        try
        {
            ItemStorage defaultStorage = prefab.GetComponent<ItemStorage>();
            if (defaultStorage == null)
            {
                Debug.LogError("ItemStorage prefab has no ItemStorage component.");

                return;

            }

            AddKits(defaultStorage, kit);
            AddKits(hub, kit);
            PrefabUtility.SaveAsPrefabAsset(prefab, StoragePrefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("Rescue Kits", "Three Rescue Kits added to Storage, ready to equip in Barracks.", "OK");

        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);

        }

    }

    private static void AddKits(ItemStorage storage, ConsumableData kit)
    {
        int existing = 0;
        foreach (StoredItem item in storage.Items)
        {
            if (item != null && item.Kind == StoredItemKind.Consumable &&
                item.Consumable.definition == kit)
            {
                existing++;

            }

        }

        for (int i = existing; i < 3; i++)
        {
            storage.TryStoreConsumable(ConsumableGenerator.CreateConsumable(kit));

        }

        EditorUtility.SetDirty(storage);

    }

}
