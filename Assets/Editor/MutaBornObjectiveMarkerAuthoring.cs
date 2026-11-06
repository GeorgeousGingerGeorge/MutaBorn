#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Creates editable prefab UI objects in the Editor, not runtime-generated menus.
public static class MutaBornObjectiveMarkerAuthoring
{
    private const string Folder = "Assets/Prefabs/Missions";
    private const string MarkerPath = Folder + "/ObjectiveWorldMarker.prefab";
    private const string ManagerPath = Folder + "/MissionObjectiveWorldMarkers.prefab";
    private const string RuntimeResourceFolder = "Assets/Resources/MutaBorn";
    private const string RuntimeManagerPath = RuntimeResourceFolder + "/MissionObjectiveWorldMarkers.prefab";
    private const string Icons = "Assets/Textures/Mission Icons/";

    [MenuItem("Tools/MutaBorn/Build Objective World Marker Prefabs")]
    public static void BuildPrefabs()
    {
        if ((AssetDatabase.LoadAssetAtPath<GameObject>(MarkerPath) != null ||
             AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPath) != null) &&
            !EditorUtility.DisplayDialog("Replace Objective Marker Prefabs?",
                "Existing objective marker prefabs will be overwritten, including any UI adjustments you made. Continue?",
                "Replace Prefabs", "Cancel"))
        {
            return;

        }

        EnsureFolder();
        GameObject markerSource = BuildMarkerGameObject();
        GameObject markerAsset = PrefabUtility.SaveAsPrefabAsset(markerSource, MarkerPath);
        Object.DestroyImmediate(markerSource);

        if (markerAsset == null)
        {
            Debug.LogError("Failed to save ObjectiveWorldMarker prefab.");
            return;

        }

        GameObject managerSource = new GameObject("MissionObjectiveWorldMarkers", typeof(MissionObjectiveWorldMarkers));
        MissionObjectiveWorldMarkers controller = managerSource.GetComponent<MissionObjectiveWorldMarkers>();
        SerializedObject serialized = new SerializedObject(controller);
        SetReference(serialized, "markerPrefab", markerAsset.GetComponent<MissionObjectiveWorldMarkerUI>());
        SetReference(serialized, "eliminateIcon", LoadIcon("EliminateLogo"));
        SetReference(serialized, "priorityTargetIcon", LoadIcon("TargetLogo"));
        SetReference(serialized, "interactIcon", LoadIcon("InteractLogo"));
        SetReference(serialized, "rescueIcon", LoadIcon("RescueLogo"));
        SetReference(serialized, "extractionIcon", LoadIcon("EscapeLogo"));
        SetV3Defaults(serialized);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject managerAsset = PrefabUtility.SaveAsPrefabAsset(managerSource, ManagerPath);
        Object.DestroyImmediate(managerSource);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (managerAsset == null)
        {
            Debug.LogError("Failed to save MissionObjectiveWorldMarkers prefab.");
            return;

        }

        EnsureRuntimeMarkerPrefab(managerAsset);
        Selection.activeObject = managerAsset;
        Debug.Log("Built editable objective marker prefabs at " + MarkerPath + " and " + ManagerPath +
                  ". MissionObjectiveManager now creates the marker display automatically.");
    }

    // One-time project setup: create a prefab variant in Resources so any scene
    // with a MissionObjectiveManager can load it automatically. This variant
    // inherits later changes to the editable manager prefab.
    [MenuItem("Tools/MutaBorn/Enable Automatic Objective World Markers")]
    public static void EnableAutomaticMarkers()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPath);
        if (source == null)
        {
            Debug.LogWarning("Create the marker prefabs first with Tools > MutaBorn > Build Objective World Marker Prefabs.");
            return;

        }

        EnsureRuntimeMarkerPrefab(source);

    }

    private static void EnsureRuntimeMarkerPrefab(GameObject source)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeManagerPath) != null)
        {
            Debug.Log("Automatic objective markers already registered at " + RuntimeManagerPath +
                      ". No prefab overwritten.");
            return;

        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");

        }

        if (!AssetDatabase.IsValidFolder(RuntimeResourceFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "MutaBorn");

        }

        GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null)
        {
            Debug.LogError("Unable to instantiate objective marker source prefab for auto-registration.");
            return;

        }

        try
        {
            // Saving a prefab instance produces a variant, retaining the original
            // prefab as the editable source for styling/icons/visibility.
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(instance, RuntimeManagerPath);
            if (asset == null)
            {
                Debug.LogError("Unable to create the automatic objective marker prefab.");
                return;

            }

            AssetDatabase.SaveAssets();
            Debug.Log("Automatic objective markers enabled. MissionObjectiveManager will spawn " +
                      "the presentation layer when a mission initializes. No per-scene setup required.");

        }
        finally
        {
            Object.DestroyImmediate(instance);

        }

    }

    [MenuItem("Tools/MutaBorn/Add Objective Markers To Open Mission")]
    public static void AddToOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before adding objective markers.");
            return;

        }

        if (Object.FindObjectsByType<MissionObjectiveWorldMarkers>(FindObjectsInactive.Include).Length > 0)
        {
            Debug.LogWarning("This scene already contains MissionObjectiveWorldMarkers. No duplicate created.");
            return;

        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPath);
        if (prefab == null)
        {
            BuildPrefabs();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPath);

        }

        if (prefab == null)
        {
            return;

        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            Debug.LogError("Unable to place the objective marker manager in the open scene.");
            return;

        }

        Undo.RegisterCreatedObjectUndo(instance, "Add Objective Markers");
        EditorSceneManager.MarkSceneDirty(instance.scene);
        Selection.activeGameObject = instance;
        Debug.Log("Added MissionObjectiveWorldMarkers to the open scene. This is now optional: " +
                  "MissionObjectiveManager can create the markers automatically after the one-time setup.", instance);
    }

    private static GameObject BuildMarkerGameObject()
    {
        GameObject root = new GameObject("ObjectiveWorldMarker", typeof(RectTransform), typeof(Canvas), typeof(MissionObjectiveWorldMarkerUI));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(420f, 90f);
        rootRect.localScale = Vector3.one * 0.0035f;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 150;

        Image background = CreateImage(rootRect, "Background", new Vector2(420f, 90f), Vector2.zero,
            new Color(0.035f, 0.055f, 0.085f, 0.93f));
        Image accent = CreateImage(rootRect, "Primary/Optional Accent", new Vector2(7f, 84f), new Vector2(-204f, 0f),
            new Color(0.35f, 0.80f, 1f, 1f));
        Image icon = CreateImage(rootRect, "Objective Icon", new Vector2(67f, 67f), new Vector2(-159f, 0f), Color.white);
        icon.preserveAspect = true;
        Image pointer = CreateImage(rootRect, "Pointer Diamond", new Vector2(13f, 13f), new Vector2(0f, -48f),
            new Color(0.35f, 0.80f, 1f, 1f));
        pointer.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        TextMeshProUGUI title = CreateText(rootRect, "Objective Title", new Vector2(300f, 44f),
            new Vector2(38f, 8f), 27f, FontStyles.Bold, Color.white, "Find the target");
        TextMeshProUGUI category = CreateText(rootRect, "Objective Category", new Vector2(300f, 23f),
            new Vector2(38f, -27f), 17f, FontStyles.Normal, new Color(0.35f, 0.80f, 1f), "PRIMARY");

        MissionObjectiveWorldMarkerUI marker = root.GetComponent<MissionObjectiveWorldMarkerUI>();
        SerializedObject serialized = new SerializedObject(marker);
        SetReference(serialized, "background", background);
        SetReference(serialized, "accentStrip", accent);
        SetReference(serialized, "iconImage", icon);
        SetReference(serialized, "pointerDiamond", pointer);
        SetReference(serialized, "titleText", title);
        SetReference(serialized, "categoryText", category);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        ApplyV3Layout(root);

        return root;
    }

    private static Image CreateImage(RectTransform parent, string objectName, Vector2 size, Vector2 position, Color color)
    {
        GameObject item = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Image graphic = item.GetComponent<Image>();
        graphic.color = color;
        graphic.raycastTarget = false;
        return graphic;
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string objectName, Vector2 size,
        Vector2 position, float fontSize, FontStyles style, Color color, string value)
    {
        GameObject item = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        TextMeshProUGUI label = item.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;

        }

        label.text = value;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.enableAutoSizing = true;
        label.fontSizeMin = 15f;
        label.fontSizeMax = fontSize;
        label.raycastTarget = false;
        return label;
    }

    [MenuItem("Tools/MutaBorn/Upgrade Existing Objective Marker Prefabs to V3")]
    public static void UpgradeExistingPrefabsToV3()
    {
        GameObject existingMarker = AssetDatabase.LoadAssetAtPath<GameObject>(MarkerPath);
        if (existingMarker == null)
        {
            EditorUtility.DisplayDialog("No existing marker prefab",
                "Run Tools > MutaBorn > Build Objective World Marker Prefabs first, then retry this command.",
                "OK");
            return;

        }

        if (!EditorUtility.DisplayDialog("Upgrade objective markers to V3?",
            "This updates the existing banner's layout and the manager prefab's display defaults. " +
            "It preserves your prefab GUIDs, sprite references and world-marker scripts, " +
            "but any custom positions, text styling or marker scale will be replaced. " +
            "Back up customized prefabs if needed.", "Upgrade", "Cancel"))
        {
            return;

        }

        GameObject marker = PrefabUtility.LoadPrefabContents(MarkerPath);
        try
        {
            ApplyV3Layout(marker);
            PrefabUtility.SaveAsPrefabAsset(marker, MarkerPath);

        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(marker);

        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPath) != null)
        {
            GameObject manager = PrefabUtility.LoadPrefabContents(ManagerPath);
            try
            {
                MissionObjectiveWorldMarkers component = manager.GetComponent<MissionObjectiveWorldMarkers>();
                if (component != null)
                {
                    SerializedObject serialized = new SerializedObject(component);
                    SetV3Defaults(serialized);

                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(manager, ManagerPath);

                }

            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(manager);

            }

        }

        // Keep the auto-loaded Resources variant in sync even when it has
        // older serialized overrides from a previous version.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeManagerPath) != null)
        {
            GameObject runtimeManager = PrefabUtility.LoadPrefabContents(RuntimeManagerPath);
            try
            {
                MissionObjectiveWorldMarkers component = runtimeManager.GetComponent<MissionObjectiveWorldMarkers>();
                if (component != null)
                {
                    SerializedObject serialized = new SerializedObject(component);
                    SetV3Defaults(serialized);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(runtimeManager, RuntimeManagerPath);

                }

            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(runtimeManager);

            }

        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Objective World Marker V3 layout applied. Existing GUIDs and icon references retained. " +
                  "The auto-loaded Resources variant was updated if present.");

    }

    private static void SetV3Defaults(SerializedObject serialized)
    {
        SetNumber(serialized, "canvasScale", 0.0065f);
        SetNumber(serialized, "unitHeight", 1.65f);
        SetNumber(serialized, "interactableHeight", 1.9f);
        SetNumber(serialized, "extractionHeight", 2.1f);
        SetNumber(serialized, "cameraForwardOffset", 1.0f);
        SetNumber(serialized, "tetherWidth", 0.035f);
        SerializedProperty lines = serialized.FindProperty("showTetherLines");
        if (lines != null)
        {
            lines.boolValue = true;

        }

        SerializedProperty optional = serialized.FindProperty("showOptionalObjectives");
        if (optional != null)
        {
            optional.boolValue = true;

        }

        SerializedProperty key = serialized.FindProperty("optionalRevealKey");
        if (key != null)
        {
            key.intValue = (int)UnityEngine.InputSystem.Key.O;

        }

    }

    private static void SetNumber(SerializedObject serialized, string field, float value)
    {
        SerializedProperty property = serialized.FindProperty(field);
        if (property != null)
        {
            property.floatValue = value;

        }

    }

    private static void ApplyV3Layout(GameObject root)
    {
        RectTransform rootRect = root.GetComponent<RectTransform>();
        if (rootRect == null)
        {
            Debug.LogWarning("Objective marker prefab has no RectTransform.");
            return;

        }

        rootRect.sizeDelta = new Vector2(330f, 350f);
        rootRect.localScale = Vector3.one * 0.0065f;
        Canvas canvas = root.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 150;

        }
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = root.AddComponent<CanvasGroup>();

        }

        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        SetImageLayout(root.transform, "Background", new Vector2(330f, 350f), Vector2.zero);
        // Full-width top border: the older thin left-hand accent is replaced.
        SetImageLayout(root.transform, "Primary/Optional Accent", new Vector2(330f, 10f), new Vector2(0f, 170f));
        SetImageLayout(root.transform, "Objective Icon", new Vector2(216f, 216f), new Vector2(0f, 22f));
        SetImageLayout(root.transform, "Pointer Diamond", new Vector2(18f, 18f), new Vector2(0f, -184f));
        SetTextLayout(root.transform, "Objective Category", new Vector2(300f, 33f),
            new Vector2(0f, 143f), 23f, TextAlignmentOptions.Center);
        SetTextLayout(root.transform, "Objective Title", new Vector2(302f, 77f),
            new Vector2(0f, -123f), 31f, TextAlignmentOptions.Center);

        Transform tetherChild = root.transform.Find("Objective Tether");
        if (tetherChild == null)
        {
            GameObject tether = new GameObject("Objective Tether", typeof(LineRenderer));
            tether.transform.SetParent(root.transform, false);
            tetherChild = tether.transform;

        }

        LineRenderer line = tetherChild.GetComponent<LineRenderer>();
        if (line == null)
        {
            line = tetherChild.gameObject.AddComponent<LineRenderer>();

        }

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = 0.035f;
        line.endWidth = 0.023f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false; // Configured at runtime when attached to a real target.
        MissionObjectiveWorldMarkerUI markerUI = root.GetComponent<MissionObjectiveWorldMarkerUI>();
        if (markerUI != null)
        {
            SerializedObject serialized = new SerializedObject(markerUI);
            SetReference(serialized, "tetherLine", line);
            serialized.ApplyModifiedPropertiesWithoutUndo();

        }

    }

    private static Transform FindDirectChild(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == name)
            {
                return child;

            }

        }

        return root.Find(name);

    }

    private static void SetImageLayout(Transform root, string name, Vector2 size, Vector2 position)
    {
        // Transform.Find interprets '/' as a hierarchy separator, but the existing
        // "Primary/Optional Accent" object has a literal slash in its name.
        // Search direct children by exact name so the V2 side strip is truly updated.
        Transform child = FindDirectChild(root, name);
        if (child == null)
        {
            Debug.LogWarning("Objective marker prefab is missing " + name);
            return;

        }

        RectTransform rect = child.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
            rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

        }

        Image image = child.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = false;
            if (name == "Primary/Optional Accent")
            {
                image.type = Image.Type.Simple;
                image.preserveAspect = false;

            }
            if (name == "Objective Icon")
            {
                image.preserveAspect = true;

            }

        }

    }

    private static void SetTextLayout(Transform root, string name, Vector2 size,
        Vector2 position, float fontSize, TextAlignmentOptions alignment)
    {
        // Transform.Find interprets '/' as a hierarchy separator, but the existing
        // "Primary/Optional Accent" object has a literal slash in its name.
        // Search direct children by exact name so the V2 side strip is truly updated.
        Transform child = FindDirectChild(root, name);
        if (child == null)
        {
            Debug.LogWarning("Objective marker prefab is missing " + name);
            return;

        }

        RectTransform rect = child.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
            rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

        }

        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text != null)
        {
            text.fontSize = fontSize;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = 16f;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = true;
            text.raycastTarget = false;

        }

    }

    private static Sprite LoadIcon(string fileName)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Icons + fileName + ".png");
        if (sprite == null)
        {
            Debug.LogWarning("Objective icon missing: " + fileName + ".png. Assign it in the prefab Inspector later.");

        }

        return sprite;
    }

    private static void SetReference(SerializedObject serialized, string property, Object value)
    {
        SerializedProperty field = serialized.FindProperty(property);
        if (field == null)
        {
            Debug.LogError("Missing serialized objective marker field: " + property);
            return;

        }

        field.objectReferenceValue = value;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        }

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Missions");

        }

    }
}
#endif
