using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MissionLootPopupAuthoring
{
    private const string PrefabPath = "Assets/Resources/UI/Mission Loot Popup.prefab";

    [MenuItem("Tools/MutaBorn/Missions/Create Loot Popup Prefab")]
    public static void CreatePrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before creating the Loot Popup prefab.");

            return;

        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log("Loot Popup prefab already exists. It was not overwritten.");

            return;

        }

        Directory.CreateDirectory("Assets/Resources/UI");
        GameObject root = new GameObject("Mission Loot Popup", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MissionLootPopup));
        RectTransform canvasRect = root.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform overlay = Rect("Window", root.transform);
        Stretch(overlay);
        Image backdrop = overlay.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.7f);

        RectTransform panel = Rect("Loot Results", overlay);
        panel.anchorMin = Vector2.one * 0.5f;
        panel.anchorMax = Vector2.one * 0.5f;
        panel.pivot = Vector2.one * 0.5f;
        panel.sizeDelta = new Vector2(620, 440);
        panel.anchoredPosition = Vector2.zero;
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.10f, 0.12f, 0.16f, 1f);

        TMP_Text title = MakeText("Title", panel, "LOOT RESULTS", 33,
            TextAlignmentOptions.Center, new Vector2(0, 175), new Vector2(570, 60));

        RectTransform viewport = Rect("Items Viewport", panel);
        viewport.anchorMin = Vector2.one * 0.5f;
        viewport.anchorMax = Vector2.one * 0.5f;
        viewport.sizeDelta = new Vector2(545, 245);
        viewport.anchoredPosition = new Vector2(0, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image transparent = viewport.gameObject.AddComponent<Image>();
        transparent.color = new Color(0, 0, 0, 0.01f);

        RectTransform content = Rect("Items Text", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        TextMeshProUGUI body = content.gameObject.AddComponent<TextMeshProUGUI>();
        body.font = TMP_Settings.defaultFontAsset;
        body.fontSize = 26;
        body.color = Color.white;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.text = "No loot collected.";
        body.textWrappingMode = TextWrappingModes.Normal;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        RectTransform buttonRect = Rect("Close", panel);
        buttonRect.anchorMin = Vector2.one * 0.5f;
        buttonRect.anchorMax = Vector2.one * 0.5f;
        buttonRect.sizeDelta = new Vector2(240, 60);
        buttonRect.anchoredPosition = new Vector2(0, -168);
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = new Color(0.24f, 0.35f, 0.47f, 1f);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        MakeText("Button Label", buttonRect, "CLOSE", 24,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(240, 60));

        SerializedObject serialized = new SerializedObject(root.GetComponent<MissionLootPopup>());
        serialized.FindProperty("window").objectReferenceValue = overlay.gameObject;
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("bodyText").objectReferenceValue = body;
        serialized.FindProperty("closeButton").objectReferenceValue = button;
        serialized.FindProperty("scrollRect").objectReferenceValue = scroll;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Selection.activeObject = prefab;
        Debug.Log("Loot Popup prefab created. Runtime interactions load it automatically from Resources/UI.");

    }

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        return rect;

    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

    }

    private static TMP_Text MakeText(string name, Transform parent, string value, int fontSize,
        TextAlignmentOptions alignment, Vector2 position, Vector2 size)
    {
        RectTransform rect = Rect(name, parent);
        rect.anchorMin = Vector2.one * 0.5f;
        rect.anchorMax = Vector2.one * 0.5f;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.text = value;
        text.raycastTarget = false;

        return text;

    }

}
