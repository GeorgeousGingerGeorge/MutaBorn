using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MutaBornMenuPolishInstaller
{
    private static readonly HashSet<string> MenuNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Barracks Screen", "BrigPrisonerScreen", "WorkshopScreen", "Storage",
        "Research Lab Panel", "UnitDetailScreen", "ItemDetailScreen",
        "MissionSelectScreen", "CrewSelectScreen", "RaceMapScreen", "Unit Inspect",
        "Pause Menu", "Options Menu", "Controls Panel", "Expanded Action Panel",
        "MainMenu Panel", "Credits Panel", "hub_DatabaseScreen",
        "UpgradePanel", "Research Complete Notification", "ResourcesDropdown"
    };

    private static readonly HashSet<string> GrowMenus = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Research Lab Panel", "RaceMapScreen", "MissionSelectScreen", "CrewSelectScreen",
        "Barracks Screen", "BrigPrisonerScreen", "WorkshopScreen", "Storage",
        "hub_DatabaseScreen", "ResourcesDropdown"

    };

    private static Sprite lockSprite;
    private static Sprite twoHandedSprite;
    private static Sprite barracksSprite;
    private static Sprite jailSprite;
    private static Sprite labSprite;
    private static Sprite craftSprite;
    private static Sprite storageSprite;
    private static Sprite consumableSprite;

    [MenuItem("Tools/MutaBorn/UI/Install Motion and Equipment Badges (Scene + UI Prefabs)")]
    private static void InstallCurrentSceneAndPrefabs()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before installing editable UI components.");

            return;

        }

        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.isLoaded)
        {
            Debug.LogWarning("Open the scene you want to update first.");

            return;

        }

        RefreshSprites();
        int count = InstallPrefabs();
        foreach (GameObject root in active.GetRootGameObjects())
        {
            count += InstallComponents(root);

        }

        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MutaBorn UI] Installed {count} editable motion / badge components in the open scene and UI prefabs.");

    }

    [MenuItem("Tools/MutaBorn/UI/Install Motion and Equipment Badges (All Game Scenes)")]
    private static void InstallAllScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;

        }

        RefreshSprites();
        int count = InstallPrefabs();
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            }

            int sceneCount = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                sceneCount += InstallComponents(root);

            }

            if (sceneCount > 0)
            {
                count += sceneCount;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

            }

            if (!wasLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);

            }

        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MutaBorn UI] Installed {count} editable motion / badge components in game scenes and UI prefabs.");

    }

    private static int InstallPrefabs()
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int installed = InstallComponents(root);
            if (installed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                count += installed;

            }

            PrefabUtility.UnloadPrefabContents(root);

        }

        return count;

    }

    private static int InstallComponents(GameObject root)
    {
        int installed = 0;
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponent<MenuButtonMotion>() == null)
            {
                button.gameObject.AddComponent<MenuButtonMotion>();
                installed++;

            }

        }

        // These are always-active containers, not the screens that are toggled.
        // Older installs attached motion here and skipped their actual panels.
        foreach (RectTransform panel in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (panel.name == "CrewMissionMenus" || panel.name == "Research Hub UI")
            {
                MenuMotion containerMotion = panel.GetComponent<MenuMotion>();
                if (containerMotion != null)
                {
                    UnityEngine.Object.DestroyImmediate(containerMotion, true);
                    installed++;

                }

            }

        }

        foreach (RectTransform panel in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (!MenuNames.Contains(panel.name) || panel.GetComponent<Button>() != null)
            {
                continue;

            }

            MenuMotion motion = panel.GetComponent<MenuMotion>();
            if (motion == null && panel.GetComponentInParent<MenuMotion>() != null)
            {
                continue;

            }

            if (motion == null)
            {
                motion = panel.gameObject.AddComponent<MenuMotion>();
                installed++;

            }

            if (GrowMenus.Contains(panel.name))
            {
                SerializedObject data = new SerializedObject(motion);
                SerializedProperty style = data.FindProperty("transition");
                if (style != null && style.enumValueIndex != (int)MenuMotion.TransitionStyle.Grow)
                {
                    style.enumValueIndex = (int)MenuMotion.TransitionStyle.Grow;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    installed++;

                }

            }

        }

        foreach (BarracksMenu menu in root.GetComponentsInChildren<BarracksMenu>(true))
        {
            installed += WireBadge(menu, "itemIcon", "itemTypeBadge");

        }

        foreach (HubStorageMenu menu in root.GetComponentsInChildren<HubStorageMenu>(true))
        {
            installed += WireBadge(menu, "itemIcon", "itemTypeBadge");

        }

        foreach (WorkshopMenu menu in root.GetComponentsInChildren<WorkshopMenu>(true))
        {
            installed += WireBadge(menu, "selectedIcon", "selectedTypeBadge");

        }

        foreach (WorkshopUpgradeMenu menu in root.GetComponentsInChildren<WorkshopUpgradeMenu>(true))
        {
            installed += WireBadge(menu, "itemIcon", "itemTypeBadge");

        }

        foreach (WorkshopRecipeRowUI row in root.GetComponentsInChildren<WorkshopRecipeRowUI>(true))
        {
            installed += WireBadge(row, "icon", "typeBadge");

        }

        foreach (WorkshopUpgradeRowUI row in root.GetComponentsInChildren<WorkshopUpgradeRowUI>(true))
        {
            installed += WireBadge(row, "icon", "typeBadge");

        }

        foreach (BarracksStoredItemRowUI row in root.GetComponentsInChildren<BarracksStoredItemRowUI>(true))
        {
            installed += WireBadge(row, "itemIcon", "secondaryIcon");

        }

        foreach (BarracksMenu menu in root.GetComponentsInChildren<BarracksMenu>(true))
        {
            installed += WireSprite(menu, "lockedSlotSprite", lockSprite);
            installed += WireSprite(menu, "twoHandedLockSprite", twoHandedSprite);

        }

        foreach (CrewMissionMenu menu in root.GetComponentsInChildren<CrewMissionMenu>(true))
        {
            installed += WireSprite(menu, "lockedSlotSprite", lockSprite);

        }

        foreach (BarracksUnitRowUI row in root.GetComponentsInChildren<BarracksUnitRowUI>(true))
        {
            installed += WireSprite(row, "lockedCellSprite", lockSprite);

        }

        foreach (WorkshopMenu menu in root.GetComponentsInChildren<WorkshopMenu>(true))
        {
            installed += WireSprite(menu, "lockedRecipeSprite", lockSprite);

        }

        foreach (BarracksMenu menu in root.GetComponentsInChildren<BarracksMenu>(true))
        {
            installed += WireSprite(menu, "emptyConsumableSprite", consumableSprite);

        }

        installed += InstallMenuIcons(root);

        return installed;

    }

    private static int WireBadge(Component component, string iconField, string badgeField)
    {
        SerializedObject data = new SerializedObject(component);
        SerializedProperty primary = data.FindProperty(iconField);
        SerializedProperty secondary = data.FindProperty(badgeField);
        if (primary == null || secondary == null || primary.objectReferenceValue == null)
        {
            return 0;

        }

        Image itemImage = primary.objectReferenceValue as Image;
        if (itemImage == null)
        {
            return 0;

        }

        Image badge = secondary.objectReferenceValue as Image;
        int installed = 0;
        if (badge == null)
        {
            Transform existing = itemImage.transform.Find("Equipment Type Badge");
            if (existing != null)
            {
                badge = existing.GetComponent<Image>();

            }

            if (badge == null)
            {
                GameObject badgeObject = new GameObject("Equipment Type Badge", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                badgeObject.transform.SetParent(itemImage.transform, false);
                badge = badgeObject.GetComponent<Image>();
                badge.enabled = false;
                installed++;

            }

            secondary.objectReferenceValue = badge;
            data.ApplyModifiedPropertiesWithoutUndo();

        }

        RectTransform rect = badge.rectTransform;
        rect.SetParent(itemImage.transform, false);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-2f, 2f);
        rect.sizeDelta = new Vector2(28f, 28f);
        badge.preserveAspect = true;
        badge.raycastTarget = false;

        return installed;

    }

    private static int WireSprite(Component component, string propertyName, Sprite sprite)
    {
        if (sprite == null)
        {
            return 0;

        }

        SerializedObject data = new SerializedObject(component);
        SerializedProperty property = data.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == sprite)
        {
            return 0;

        }

        property.objectReferenceValue = sprite;
        data.ApplyModifiedPropertiesWithoutUndo();

        return 1;

    }

    private static void RefreshSprites()
    {
        lockSprite = FindLockSprite(false);
        twoHandedSprite = FindLockSprite(true);
        barracksSprite = FindPreferredSprite("BarracksLogo");
        jailSprite = FindPreferredSprite("JailLogo");
        labSprite = FindPreferredSprite("LabLogo");
        craftSprite = FindPreferredSprite("CraftLogo");
        storageSprite = FindPreferredSprite("StorageLogo");
        consumableSprite = FindPreferredSprite("ConsumablesType", "Consumable", "Consumables");
        Debug.Log("[MutaBorn UI] Lock sprite: " + (lockSprite == null ? "not found" :
            AssetDatabase.GetAssetPath(lockSprite)) + "; Two-handed lock: " +
            (twoHandedSprite == null ? " not found" : AssetDatabase.GetAssetPath(twoHandedSprite)) +
            "; Consumable icon: " + (consumableSprite == null ? "not found" :
            AssetDatabase.GetAssetPath(consumableSprite)));

    }

    [MenuItem("Tools/MutaBorn/UI/Fix Menu Logo Placement (Open Scene + UI Prefabs)")]
    private static void FixMenuLogoPlacement()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before changing UI objects.");

            return;

        }

        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.isLoaded)
        {
            Debug.LogWarning("Open Hub.unity first.");

            return;

        }

        RefreshSprites();
        int changed = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            int prefabChanges = InstallMenuIcons(prefab);
            if (prefabChanges > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                changed += prefabChanges;

            }

            PrefabUtility.UnloadPrefabContents(prefab);

        }

        foreach (GameObject root in active.GetRootGameObjects())
        {
            changed += InstallMenuIcons(root);

        }

        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MutaBorn UI] Fixed menu logos ({changed} changes). " +
            "Only Hub navigation buttons and main menu panels receive logos.");

    }

    private static int InstallMenuIcons(GameObject root)
    {
        int changed = 0;
        // Remove only the decorations created by the previous broad name matcher.
        // Existing buttons, artwork, and user-authored icons are left alone.
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "Menu Button Icon" && child.name != "Menu Header Icon")
            {
                continue;

            }

            if (child.GetComponent<Image>() == null || child.childCount != 0)
            {
                continue;

            }

            UnityEngine.Object.DestroyImmediate(child.gameObject);
            changed++;

        }

        // Only the five top-level buttons on the Hub bottom navigation bar.
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (!string.Equals(child.name, "BottomBar", StringComparison.OrdinalIgnoreCase))
            {
                continue;

            }

            foreach (Transform buttonTransform in child)
            {
                if (buttonTransform.GetComponent<Button>() == null)
                {
                    continue;

                }

                Sprite sprite = ResolveNavigationIcon(buttonTransform.name);
                if (sprite == null)
                {
                    continue;

                }

                changed += EnsureDecorIcon(buttonTransform, "Hub Navigation Logo", sprite,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(5f, 0f),
                    new Vector2(23f, 23f));

            }

        }

        // Exactly one larger logo on each main menu, never on its nested panels.
        foreach (RectTransform panel in root.GetComponentsInChildren<RectTransform>(true))
        {
            Sprite sprite = ResolveMainMenuIcon(panel);
            if (sprite == null)
            {
                continue;

            }

            changed += EnsureDecorIcon(panel, "Main Menu Logo", sprite,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(15f, -12f),
                new Vector2(54f, 54f));

        }

        return changed;

    }

    private static int EnsureDecorIcon(Transform parent, string name, Sprite sprite,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Transform existing = parent.Find(name);
        Image image = existing == null ? null : existing.GetComponent<Image>();
        int changed = 0;
        if (image == null)
        {
            GameObject iconObject = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            image = iconObject.GetComponent<Image>();
            changed++;

        }

        if (image.sprite != sprite)
        {
            image.sprite = sprite;
            changed++;

        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        image.preserveAspect = true;
        image.raycastTarget = false;

        return changed;

    }

    private static Sprite ResolveNavigationIcon(string name)
    {
        switch (name)
        {
            case "Barracks":
                return barracksSprite;
            case "Jail":
                return jailSprite;
            case "Lab":
                return labSprite;
            case "Workshop":
                return craftSprite;
            case "Storage":
                return storageSprite;
            default:
                return null;

        }

    }

    private static Sprite ResolveMainMenuIcon(RectTransform panel)
    {
        string name = panel.name;
        string parent = panel.parent == null ? "" : panel.parent.name;
        if (name == "Barracks Screen")
        {
            return barracksSprite;

        }

        if (name == "Jail" && parent == "TEMP CANVAS")
        {
            return jailSprite;

        }

        if (name == "Research Lab Panel")
        {
            return labSprite;

        }

        if (name == "WorkshopScreen")
        {
            return craftSprite;

        }

        if (name == "Storage" && parent == "TEMP CANVAS")
        {
            return storageSprite;

        }

        if (name == "ResourcesDropdown")
        {
            return storageSprite;

        }

        return null;

    }

    private static Sprite FindPreferredSprite(params string[] keywords)
    {
        Sprite best = null;
        int bestScore = int.MinValue;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) ||
                path.IndexOf("/TEST", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;

            }

            string normalized = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant()
                .Replace(" ", "").Replace("-", "").Replace("_", "");
            bool matches = false;
            foreach (string keyword in keywords)
            {
                if (string.IsNullOrEmpty(keyword))
                {
                    continue;

                }

                string normalizedKeyword = keyword.ToLowerInvariant()
                    .Replace(" ", "").Replace("-", "").Replace("_", "");
                if (normalized.Contains(normalizedKeyword))
                {
                    matches = true;
                    break;

                }

            }

            if (!matches)
            {
                continue;

            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                continue;

            }

            int score = 0;
            if (path.IndexOf("/Textures/UI/", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 200;

            }

            if (path.IndexOf("/Equipment Types/", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 120;

            }

            if (path.IndexOf("/Textures/", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 30;

            }

            if (score > bestScore)
            {
                best = sprite;
                bestScore = score;

            }

        }

        return best;

    }

    private static Sprite FindLockSprite(bool twoHanded)
    {
        Sprite best = null;
        int bestScore = -1;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) ||
                path.IndexOf("/TEST", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;

            }

            string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant()
                .Replace(" ", "").Replace("-", "").Replace("_", "");
            bool special = name.Contains("twohand") || name.Contains("bothhand") ||
                name.Contains("dualwield") || name.Contains("2hand");
            bool normal = name == "lock" || name == "locked" ||
                name == "simplelock" || name == "lockicon" || name == "slotlock";
            if (twoHanded ? !special : !normal)
            {
                continue;

            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                continue;

            }

            int score = 0;
            if (path.IndexOf("Lock Icons", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 100;

            }

            if (path.IndexOf("Equipment Icons", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 50;

            }

            if (path.IndexOf("/UI/", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 20;

            }

            if (score > bestScore)
            {
                best = sprite;
                bestScore = score;

            }

        }

        return best;

    }

}
