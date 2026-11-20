using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MutaBornTutorialMovementPanelSetup
{
    private const string LevelPath = "Assets/Prefabs/Environment/AuthoredLevels(PrefabLevels/Tutorial/Tutorial - First Steps.prefab";
    private const string PanelPath = "Assets/Prefabs/Missions/Tutorial Movement Info Panel.prefab";
    private const string SpawnerPath = "Assets/Scripts/Missions/TutorialMovementHintSpawner.cs";
    private const string BillboardPath = "Assets/Scripts/Missions/FaceCameraBillboard.cs";

    [MenuItem("Tools/MutaBorn/Tutorial/Create Editable Movement Panel")]
    private static void CreateEditableMovementPanel()
    {
        if (!File.Exists(LevelPath))
        {
            Debug.LogError("Tutorial - First Steps.prefab was not found. No files were changed.");
            return;

        }

        GameObject tutorial = PrefabUtility.LoadPrefabContents(LevelPath);
        try
        {
            foreach (MonoBehaviour component in tutorial.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component != null && component.GetType().Name == "TutorialMovementHintSpawner")
                {
                    Object.DestroyImmediate(component);

                }

            }

            PrefabUtility.SaveAsPrefabAsset(tutorial, LevelPath);

        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(tutorial);

        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath) == null)
        {
            CreatePanelPrefab();

        }

        if (AssetDatabase.LoadAssetAtPath<MonoScript>(SpawnerPath) != null)
        {
            AssetDatabase.DeleteAsset(SpawnerPath);

        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("Removed the old tutorial hint spawner and prepared one editable prefab at " + PanelPath + ". Drag it into Tutorial - First Steps to place it.");

    }

    private static void CreatePanelPrefab()
    {
        GameObject panel = new GameObject("Tutorial Movement Info Panel", typeof(RectTransform), typeof(Canvas));
        try
        {
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(520f, 220f);
            panel.transform.position = new Vector3(9f, 3.5f, 12f);
            panel.transform.localScale = Vector3.one * 0.009f;

            Canvas canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30;

            MonoScript billboardScript = AssetDatabase.LoadAssetAtPath<MonoScript>(BillboardPath);
            if (billboardScript != null && billboardScript.GetClass() != null)
            {
                panel.AddComponent(billboardScript.GetClass());

            }

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            background.transform.SetParent(panel.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.09f, 0.11f, 0.15f, 0.92f);
            backgroundImage.raycastTarget = false;

            CreateText(panel.transform, "Heading", "MOVEMENT & TERRAIN", 30f, -16f, 43f, true);
            CreateText(panel.transform, "Explanation",
                "Open ground: cheapest route\nGrass / rubble: +1 movement cost\nMud: +2 movement cost\nMoving uses AP. Plan your route!",
                24f, -67f, 138f, false);

            PrefabUtility.SaveAsPrefabAsset(panel, PanelPath);

        }
        finally
        {
            Object.DestroyImmediate(panel);

        }

    }

    private static void CreateText(Transform parent, string name, string message, float fontSize,
        float top, float height, bool bold)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, top);
        rect.sizeDelta = new Vector2(-36f, height);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = message;
        text.fontSize = fontSize;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.color = Color.white;
        text.raycastTarget = false;

    }

}
