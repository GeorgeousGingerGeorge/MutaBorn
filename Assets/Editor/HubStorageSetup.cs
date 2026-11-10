using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class HubStorageSetup
{
    [MenuItem("Tools/MutaBorn/Hub/Connect Selected Storage Screen")]
    private static void Connect()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null || root.name != "Storage" ||
            root.transform.Find("Storage Scrolls/Viewport/Content") == null)
        {
            EditorUtility.DisplayDialog("MutaBorn Storage",
                "Select TEMP CANVAS > Storage in the Hierarchy first.", "OK");

            return;

        }

        string rowPath = AssetDatabase.GUIDToAssetPath("b50fd6a9396b4e01b1d0dda9c89c470b");
        BarracksStoredItemRowUI row = AssetDatabase.LoadAssetAtPath<GameObject>(rowPath)
            ?.GetComponent<BarracksStoredItemRowUI>();
        BarracksAbilityEntryUI ability = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/UI/Barracks Ability Entry.prefab")?.GetComponent<BarracksAbilityEntryUI>();
        if (row == null)
        {
            EditorUtility.DisplayDialog("MutaBorn Storage",
                "Could not find your original Stored Item Row prefab. No changes were made.", "OK");

            return;

        }

        HubStorageMenu menu = root.GetComponent<HubStorageMenu>();
        if (menu == null)
        {
            menu = Undo.AddComponent<HubStorageMenu>(root);

        }

        Undo.RecordObject(menu, "Connect Storage UI");
        SerializedObject serialized = new SerializedObject(menu);
        Set(serialized, "itemContent", Find(root, "Storage Scrolls/Viewport/Content"));
        Set(serialized, "itemRowPrefab", row);
        Set(serialized, "weaponsButton", ButtonAt(root, "Storage Header/WeaponsButton"));
        Set(serialized, "armorButton", ButtonAt(root, "Storage Header/ArmorButton"));
        Set(serialized, "accessoriesButton", ButtonAt(root, "Storage Header/AcessoryButton"));
        Set(serialized, "consumablesButton", ButtonAt(root, "Storage Header/Consumables"));
        Set(serialized, "mutationsButton", ButtonAt(root, "Storage Header/Mutations"));
        Set(serialized, "itemPopup", ComponentAt<BarracksItemPopup>(root, "ItemDetailScreen"));
        Set(serialized, "itemNameText", ComponentAt<TMP_Text>(root, "ItemDetailScreen/Header/Item Name"));
        Set(serialized, "itemTypeText", ComponentAt<TMP_Text>(root, "ItemDetailScreen/Header/Item Type"));
        Set(serialized, "itemDescriptionText", ComponentAt<TMP_Text>(root,
            "ItemDetailScreen/Description/DescriptionScroll/Viewport/Content/ItemDesc"));
        Set(serialized, "itemStatsText", ComponentAt<TMP_Text>(root,
            "ItemDetailScreen/Stats/StatsScroll/Viewport/Content/ItemStats"));
        Set(serialized, "itemIcon", ComponentAt<Image>(root, "ItemDetailScreen/Header/WeaponTypeLogo"));
        Set(serialized, "actionsContent", Find(root,
            "ItemDetailScreen/Abilities/AbilitiesScroll/Viewport/Content"));
        Set(serialized, "actionRowPrefab", ability);

        SerializedProperty stars = serialized.FindProperty("itemStars");
        stars.arraySize = 5;
        for (int i = 0; i < 5; i++)
        {
            stars.GetArrayElementAtIndex(i).objectReferenceValue = ComponentAt<Image>(root,
                "ItemDetailScreen/Header/Stars/Star" + (i + 1));

        }

        serialized.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(menu);
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorUtility.DisplayDialog("MutaBorn Storage",
            "Storage item list, category buttons and item inspector are connected. " +
            "Save the scene, then enter Play Mode from the Hub to test.", "OK");

    }

    private static Transform Find(GameObject root, string path)
    {
        return root.transform.Find(path);

    }

    private static T ComponentAt<T>(GameObject root, string path) where T : Component
    {
        Transform target = Find(root, path);

        return target == null ? null : target.GetComponent<T>();

    }

    private static Button ButtonAt(GameObject root, string path)
    {
        return ComponentAt<Button>(root, path);

    }

    private static void Set(SerializedObject serialized, string field, Object value)
    {
        serialized.FindProperty(field).objectReferenceValue = value;

    }

}
