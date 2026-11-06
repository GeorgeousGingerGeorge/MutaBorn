#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-time EDITOR tool. Creates real serialized Unity UI panels/buttons + prefab.
// It does not build menus in Play Mode and is not part of combat gameplay.
public static class MutaBornCrewMenuAuthoring
{
    [MenuItem("Tools/MutaBorn/Build Mission and Crew Menus on Selected Canvas")]
    private static void Build()
    {
        Canvas canvas = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        }
        if (canvas == null || !canvas.gameObject.scene.IsValid())
        {
            EditorUtility.DisplayDialog("MutaBorn", "Select the Main Hub's Canvas in the Hierarchy first.", "OK");

            return;

        }

        if (canvas.transform.Find("CrewMissionMenus") != null)
        {
            EditorUtility.DisplayDialog("MutaBorn", "CrewMissionMenus already exists. Delete it first if you want to rebuild.", "OK");

            return;

        }

        string prefabDirectory = "Assets/Prefabs/UI";
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(prefabDirectory))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        CrewCandidateRowUI candidatePrefab = CreateCandidatePrefab(prefabDirectory);
        if (candidatePrefab == null)
        {
            return;
        }

        RectTransform root = CreateRect("CrewMissionMenus", canvas.transform);
        Stretch(root);
        CrewMissionMenu menu = root.gameObject.AddComponent<CrewMissionMenu>();

        RectTransform mission = Panel("MissionSelectScreen", root, new Color(0.07f, 0.09f, 0.11f, 0.95f));
        Stretch(mission);
        Text("SELECT MISSION", mission, new Vector2(0, 175), new Vector2(440, 48), 32);
        Text("Test mission (temporary mission list)", mission, new Vector2(0, 90), new Vector2(500, 30), 18);
        Button missionTest = MakeButton("Test Mission", mission, new Vector2(0, 0), new Vector2(280, 54), "Main Test Scene");
        Button missionBack = MakeButton("Back", mission, new Vector2(0, -75), new Vector2(180, 42), "Back to Hub");

        RectTransform crew = Panel("CrewSelectScreen", root, new Color(0.065f, 0.075f, 0.085f, 0.97f));
        Stretch(crew);
        Text("SELECT CREW", crew, new Vector2(0, 280), new Vector2(600, 45), 30);
        Text("4 crew slots available  |  5 more through research", crew,
            new Vector2(0, 243), new Vector2(690, 32), 16);

        RectTransform gridHolder = Panel("NineCrewSlots", crew, new Color(0.16f, 0.17f, 0.19f, 0.95f));
        Place(gridHolder, new Vector2(-170, 14), new Vector2(465, 365));
        GridLayoutGroup slotGrid = gridHolder.gameObject.AddComponent<GridLayoutGroup>();
        slotGrid.cellSize = new Vector2(142, 110);
        slotGrid.spacing = new Vector2(8, 8);
        slotGrid.padding = new RectOffset(9, 9, 9, 9);
        slotGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        slotGrid.constraintCount = 3;

        CrewMissionMenu.CrewSlotView[] slotViews = new CrewMissionMenu.CrewSlotView[9];
        for (int i = 0; i < slotViews.Length; i++)
        {
            Button slot = MakeButton("CrewSlot" + (i + 1), gridHolder, Vector2.zero, new Vector2(140, 108), "");
            Image race = CreateImage("RaceIcon", slot.transform, new Vector2(0, 16), new Vector2(46, 46));
            TMP_Text name = Text("CrewName", slot.transform, new Vector2(0, -35), new Vector2(130, 26), 14);
            name.text = i < 4 ? "+ Empty" : "Research Locked";
            Image padlock = CreateImage("ResearchLockIcon", slot.transform, new Vector2(0, 17), new Vector2(37, 37));
            padlock.enabled = false;
            slotViews[i] = new CrewMissionMenu.CrewSlotView
            {
                button = slot,
                raceIcon = race,
                unitName = name,
                lockIcon = padlock
            };

        }

        RectTransform picker = Panel("AvailableRoster", crew, new Color(0.18f, 0.20f, 0.22f, 0.96f));
        Place(picker, new Vector2(315, 14), new Vector2(270, 365));
        Text("Owned Units", picker, new Vector2(0, 158), new Vector2(245, 28), 20);

        RectTransform scroll = CreateRect("RosterScroll", picker);
        Place(scroll, new Vector2(0, -15), new Vector2(242, 285));
        Image scrollImage = scroll.gameObject.AddComponent<Image>();
        scrollImage.color = new Color(0.095f, 0.105f, 0.12f, 0.95f);
        ScrollRect scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        RectTransform viewport = CreateRect("Viewport", scroll);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(0, 0);
        content.offsetMax = new Vector2(0, 0);
        VerticalLayoutGroup vertical = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(4, 4, 4, 4);
        vertical.spacing = 4;
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandHeight = false;
        vertical.childForceExpandWidth = true;
        ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = content;
        scrollRect.viewport = viewport;

        TMP_Text status = Text("CrewMessage", crew, new Vector2(0, -205), new Vector2(720, 30), 15);
        Button crewBack = MakeButton("CrewBack", crew, new Vector2(-295, -251), new Vector2(150, 41), "Back");
        Button clear = MakeButton("ClearCrewSlot", crew, new Vector2(-115, -251), new Vector2(150, 41), "Clear Slot");
        Button barracks = MakeButton("CrewBarracks", crew, new Vector2(78, -251), new Vector2(170, 41), "Barracks / Gear");
        Button deploy = MakeButton("DeployMission", crew, new Vector2(290, -251), new Vector2(185, 41), "Start Mission");

        RectTransform returnPanel = CreateRect("ReturnFromBarracks", root);
        Stretch(returnPanel);
        Button returnButton = MakeButton("ReturnToCrewButton", returnPanel,
            new Vector2(-360, 275), new Vector2(205, 40), "Return to Crew");

        List<CrewMissionMenu.CrewRaceIcon> icons = new List<CrewMissionMenu.CrewRaceIcon>();
        foreach (string raceName in new[] { "Human", "Orc", "Elf", "Dwarf", "Goblin" })
        {
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Textures/Race Icons/{raceName}Logo.png");
            icons.Add(new CrewMissionMenu.CrewRaceIcon { raceName = raceName, sprite = icon });

        }

        Sprite lockIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Equipment Icons/Locked.png");
        menu.ConfigureAuthoredUI(mission.gameObject, crew.gameObject, returnPanel.gameObject,
            missionTest, missionBack, crewBack, deploy, clear, barracks, returnButton,
            status, slotViews, content, candidatePrefab, lockIcon, icons);

        mission.gameObject.SetActive(false);
        crew.gameObject.SetActive(false);
        returnPanel.gameObject.SetActive(false);
        string menuPath = prefabDirectory + "/Crew Mission Menus.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, menuPath, InteractionMode.UserAction);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        EditorUtility.DisplayDialog("MutaBorn", "Crew & mission menus created as real Canvas objects and saved as a prefab. Save the Hub scene (Ctrl+S).", "OK");

    }

    private static CrewCandidateRowUI CreateCandidatePrefab(string directory)
    {
        string path = directory + "/Crew Candidate Row.prefab";
        GameObject previous = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (previous != null)
        {
            return previous.GetComponent<CrewCandidateRowUI>();
        }

        Button button = MakeButton("CrewCandidateRow", null, Vector2.zero, new Vector2(230, 49), "");
        Image icon = CreateImage("RaceIcon", button.transform, new Vector2(-88, 0), new Vector2(33, 33));
        TMP_Text label = Text("UnitName", button.transform, new Vector2(12, 0), new Vector2(172, 40), 16);
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 49;
        CrewCandidateRowUI row = button.gameObject.AddComponent<CrewCandidateRowUI>();
        row.ConfigureAuthoredUI(button, label, icon);
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(button.gameObject, path);
        Object.DestroyImmediate(button.gameObject);

        return saved == null ? null : saved.GetComponent<CrewCandidateRowUI>();

    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        if (parent != null)
        {
            rt.SetParent(parent, false);
        }
        rt.localScale = Vector3.one;

        return rt;

    }

    private static RectTransform Panel(string name, Transform parent, Color color)
    {
        RectTransform rt = CreateRect(name, parent);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;

        return rt;

    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

    }

    private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = rt.anchorMin;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

    }

    private static TMP_Text Text(string name, Transform parent, Vector2 pos, Vector2 size, float fontSize)
    {
        RectTransform rt = CreateRect(name, parent);
        Place(rt, pos, size);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = name;
        tmp.color = Color.white;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return tmp;

    }

    private static Image CreateImage(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        RectTransform rt = CreateRect(name, parent);
        Place(rt, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;
        img.enabled = false;

        return img;

    }

    private static Button MakeButton(string name, Transform parent, Vector2 pos, Vector2 size, string label)
    {
        RectTransform rt = CreateRect(name, parent);
        Place(rt, pos, size);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = new Color(0.28f, 0.30f, 0.33f, 1);
        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text text = Text("Label", rt, Vector2.zero, size, 17);
            text.text = label;

        }

        return button;

    }

}
#endif
