using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// UI panels and the nine crew slot buttons are REAL Unity objects.
// The only objects instantiated at runtime are reusable roster candidate rows.
public sealed class CrewMissionMenu : MonoBehaviour
{
    [Serializable]
    public sealed class MissionSceneEntry
    {
        public string displayName;
        [TextArea(2, 3)] public string description;
        [SerializeField] private string scenePath;
#if UNITY_EDITOR
        [SerializeField] private UnityEditor.SceneAsset sceneAsset;

        public void RefreshScenePath()
        {
            scenePath = sceneAsset == null ? string.Empty :
                UnityEditor.AssetDatabase.GetAssetPath(sceneAsset);

        }
#endif

        public string ScenePath => scenePath;
        public string Title => string.IsNullOrWhiteSpace(displayName) ?
            System.IO.Path.GetFileNameWithoutExtension(scenePath) : displayName;

    }

    [Serializable]
    public sealed class CrewSlotView
    {
        public Button button;
        public TMP_Text unitName;
        public Image raceIcon;
        public Image lockIcon;

    }

    [Serializable]
    public sealed class CrewRaceIcon
    {
        public string raceName;
        public Sprite sprite;

    }

    [Header("Scene panels authored in Unity")]
    [SerializeField] private GameObject raceMapRoot;
    [SerializeField] private Button orcRegionButton;
    [SerializeField] private Button raceMapBackButton;
    [SerializeField] private GameObject missionSelectRoot;
    [SerializeField] private GameObject crewSelectRoot;
    [SerializeField] private GameObject returnFromBarracksRoot;
    [Header("Mission scene list")]
    [SerializeField] private List<MissionSceneEntry> missionScenes = new List<MissionSceneEntry>();
    [SerializeField] private Transform missionListContent;
    [SerializeField] private Button missionRowTemplate;
    [SerializeField] private TMP_Text emptyMissionText;
    [SerializeField] private Button testMissionButton;
    [SerializeField] private Button missionBackButton;
    [SerializeField] private Button crewBackButton;
    [SerializeField] private Button startMissionButton;
    [SerializeField] private Button clearSlotButton;
    [SerializeField] private Button openBarracksButton;
    [SerializeField] private Button returnFromBarracksButton;
    [SerializeField] private TMP_Text messageText;

    [Header("Nine real crew buttons - first four available")]
    [SerializeField] private CrewSlotView[] crewSlots = new CrewSlotView[UnitRoster.MaximumCrewSlots];
    [SerializeField] private Sprite lockedSlotSprite;
    [SerializeField] private List<CrewRaceIcon> raceIcons = new List<CrewRaceIcon>();

    [Header("Reserve crew picker (prefab-authored row)")]
    [SerializeField] private Transform candidateContent;
    [SerializeField] private CrewCandidateRowUI candidateRowPrefab;
    [SerializeField] private BarracksMenu barracks;
    [SerializeField] private string testMissionScene = "Main Test Scene";

    private readonly List<GameObject> generatedRows = new List<GameObject>();
    private readonly List<Button> generatedMissionRows = new List<Button>();
    private string selectedMissionScene;
    private string selectedMissionTitle;
    private UnitRoster observedRoster;
    private int selectedSlot;

    private void Awake()
    {
        if (orcRegionButton != null)
        {
            orcRegionButton.onClick.AddListener(ShowMissionList);

        }

        if (raceMapBackButton != null)
        {
            raceMapBackButton.onClick.AddListener(CloseAll);

        }

        if (testMissionButton != null)
        {
            testMissionButton.onClick.AddListener(OpenLegacyMission);
        }
        if (missionBackButton != null)
        {
            missionBackButton.onClick.AddListener(OpenRaceMap);
        }
        if (crewBackButton != null)
        {
            crewBackButton.onClick.AddListener(ShowMissionList);
        }
        if (startMissionButton != null)
        {
            startMissionButton.onClick.AddListener(LaunchSelectedMission);
        }
        if (clearSlotButton != null)
        {
            clearSlotButton.onClick.AddListener(ClearSelectedSlot);
        }
        if (openBarracksButton != null)
        {
            openBarracksButton.onClick.AddListener(OpenBarracks);
        }
        if (returnFromBarracksButton != null)
        {
            returnFromBarracksButton.onClick.AddListener(ReturnFromBarracks);
        }

        for (int i = 0; i < crewSlots.Length; i++)
        {
            CrewSlotView view = crewSlots[i];
            if (view == null || view.button == null)
            {
                continue;
            }
            int captured = i;
            view.button.onClick.AddListener(() => SelectCrewSlot(captured));

        }

        CloseAll();

    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (MissionSceneEntry mission in missionScenes)
        {
            if (mission != null)
            {
                mission.RefreshScenePath();

            }

        }

    }
#endif

    private void OnEnable()
    {
        Subscribe();

    }

    private void OnDisable()
    {
        if (observedRoster != null)
        {
            observedRoster.RosterChanged -= Refresh;
            observedRoster = null;

        }

    }

    private void Subscribe()
    {
        UnitRoster roster = UnitRoster.Instance;
        if (roster == observedRoster)
        {
            return;
        }
        if (observedRoster != null)
        {
            observedRoster.RosterChanged -= Refresh;
        }
        observedRoster = roster;
        if (observedRoster != null)
        {
            observedRoster.RosterChanged += Refresh;
        }

    }

    // Keep the Hub's existing OnClick connection; the map is now the entry screen.
    public void OpenMissionSelect()
    {
        OpenRaceMap();

    }

    public void OpenRaceMap()
    {
        Subscribe();
        CloseAll();
        if (raceMapRoot != null)
        {
            raceMapRoot.SetActive(true);
            if (orcRegionButton != null)
            {
                RaceMapRegionHover region = orcRegionButton.GetComponent<RaceMapRegionHover>();
                if (region != null)
                {
                    region.Display();

                }

            }

            return;

        }

        // Existing Hub prefabs still work until the one-time UI builder is run.
        ShowMissionList();

    }

    private void ShowMissionList()
    {
        Subscribe();
        RefreshMissionList();
        CloseAll();
        if (missionSelectRoot != null)
        {
            missionSelectRoot.SetActive(true);

        }

    }

    private void OpenLegacyMission()
    {
        selectedMissionScene = testMissionScene;
        selectedMissionTitle = testMissionScene;
        OpenCrewSelection();

    }

    private void SelectMission(MissionSceneEntry mission)
    {
        selectedMissionScene = mission.ScenePath;
        selectedMissionTitle = mission.Title;
        OpenCrewSelection();

    }

    private void RefreshMissionList()
    {
        if (missionListContent == null || missionRowTemplate == null)
        {

            return;

        }

        foreach (Button row in generatedMissionRows)
        {
            if (row != null)
            {
                Destroy(row.gameObject);

            }

        }

        generatedMissionRows.Clear();
        foreach (MissionSceneEntry mission in missionScenes)
        {
            if (mission == null || string.IsNullOrWhiteSpace(mission.ScenePath))
            {
                continue;

            }

            MissionSceneEntry selected = mission;
            Button row = Instantiate(missionRowTemplate, missionListContent);
            row.gameObject.SetActive(true);
            row.onClick.RemoveAllListeners();
            row.onClick.AddListener(() => SelectMission(selected));
            TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = string.IsNullOrWhiteSpace(mission.description) ?
                    mission.Title : mission.Title + "\n" + mission.description;

            }

            generatedMissionRows.Add(row);

        }

        if (emptyMissionText != null)
        {
            emptyMissionText.gameObject.SetActive(generatedMissionRows.Count == 0);

        }

    }

    public void OpenCrewSelection()
    {
        Subscribe();
        selectedSlot = Mathf.Clamp(selectedSlot, 0, UnitRoster.MaximumCrewSlots - 1);
        if (missionSelectRoot != null)
        {
            missionSelectRoot.SetActive(false);
        }
        if (crewSelectRoot != null)
        {
            crewSelectRoot.SetActive(true);
        }
        if (returnFromBarracksRoot != null)
        {
            returnFromBarracksRoot.SetActive(false);
        }
        Refresh();

    }

    public void CloseAll()
    {
        if (raceMapRoot != null)
        {
            raceMapRoot.SetActive(false);

        }

        if (missionSelectRoot != null)
        {
            missionSelectRoot.SetActive(false);
        }
        if (crewSelectRoot != null)
        {
            crewSelectRoot.SetActive(false);
        }
        if (returnFromBarracksRoot != null)
        {
            returnFromBarracksRoot.SetActive(false);
        }

    }

    private void SelectCrewSlot(int index)
    {
        selectedSlot = index;
        Refresh();

    }

    private Sprite FindRaceIcon(UnitData unit)
    {
        if (unit == null)
        {
            return null;
        }
        foreach (CrewRaceIcon race in raceIcons)
        {
            if (race != null &&
                (string.Equals(race.raceName, unit.raceName, StringComparison.OrdinalIgnoreCase) ||
                 (unit.RaceDefinition != null &&
                  string.Equals(race.raceName, unit.RaceDefinition.RaceName, StringComparison.OrdinalIgnoreCase))))
            {
                return race.sprite;

            }

        }

        return null;

    }

    private void Refresh()
    {
        UnitRoster roster = UnitRoster.Instance;
        if (roster == null)
        {
            SetMessage("No campaign roster loaded. Start from the Hub.");

            return;

        }

        for (int i = 0; i < crewSlots.Length; i++)
        {
            CrewSlotView view = crewSlots[i];
            if (view == null)
            {
                continue;
            }
            bool locked = i >= roster.UnlockedCrewSlots;
            UnitData selected = null;
            if (!locked)
            {
                roster.TryGetDeploymentUnit(i, out selected);
            }

            if (view.unitName != null)
            {
                view.unitName.text = locked ? "Research Locked" :
                    selected == null ? "+ Empty" :
                    selected.currentHealth <= 0 ? selected.characterName + " [Fallen]" : selected.characterName;
                view.unitName.color = i == selectedSlot ? new Color(1f, 0.88f, 0.44f) : Color.white;

            }

            if (view.raceIcon != null)
            {
                view.raceIcon.sprite = FindRaceIcon(selected);
                view.raceIcon.enabled = selected != null && view.raceIcon.sprite != null;
                view.raceIcon.color = Color.white;
                view.raceIcon.raycastTarget = false;

            }

            if (view.lockIcon != null)
            {
                view.lockIcon.sprite = lockedSlotSprite;
                view.lockIcon.enabled = locked && lockedSlotSprite != null;
                view.lockIcon.color = Color.white;
                view.lockIcon.raycastTarget = false;

            }

        }

        RefreshCandidates(roster);
        string missionName = string.IsNullOrWhiteSpace(selectedMissionTitle) ?
            testMissionScene : selectedMissionTitle;
        SetMessage(selectedSlot >= roster.UnlockedCrewSlots ?
            "This slot requires a future research unlock." :
            $"{missionName} - Slot {selectedSlot + 1}: choose your crew.");

    }

    private void RefreshCandidates(UnitRoster roster)
    {
        foreach (GameObject row in generatedRows)
        {
            if (row != null)
            {
                Destroy(row);
            }

        }

        generatedRows.Clear();
        if (candidateContent == null || candidateRowPrefab == null)
        {
            return;
        }

        foreach (UnitData unit in roster.Units)
        {
            if (unit == null)
            {
                continue;
            }
            CrewCandidateRowUI row = Instantiate(candidateRowPrefab, candidateContent);
            row.gameObject.SetActive(true);
            row.Bind(unit, FindRaceIcon(unit), IsInCrew(roster, unit.UnitId),
                () => AssignSelectedSlot(unit.UnitId));
            generatedRows.Add(row.gameObject);

        }

    }

    private static bool IsInCrew(UnitRoster roster, string unitId)
    {
        foreach (UnitRosterDeployment deployment in roster.Deployments)
        {
            if (deployment != null && deployment.SlotIndex < roster.UnlockedCrewSlots &&
                string.Equals(deployment.UnitId, unitId, StringComparison.Ordinal))
            {
                return true;

            }

        }

        return false;

    }

    private void AssignSelectedSlot(string unitId)
    {
        UnitRoster roster = UnitRoster.Instance;
        if (roster == null || !roster.TryChooseCrewMember(selectedSlot, unitId))
        {
            SetMessage("Unable to assign this unit (fallen, locked, or currently deployed).");

            return;

        }

        Refresh();

    }

    private void ClearSelectedSlot()
    {
        UnitRoster roster = UnitRoster.Instance;
        if (roster == null || selectedSlot >= roster.UnlockedCrewSlots ||
            !roster.TryUnassignDeploymentSlot(selectedSlot))
        {
            SetMessage("This slot is already empty, locked, or in use.");

            return;

        }

        Refresh();

    }

    private void LaunchSelectedMission()
    {
        UnitRoster roster = UnitRoster.Instance;
        if (roster == null)
        {
            SetMessage("UnitRoster is missing.");

            return;

        }

        bool hasCrew = false;
        foreach (UnitRosterDeployment deployment in roster.Deployments)
        {
            if (deployment.SlotIndex < roster.UnlockedCrewSlots &&
                roster.TryGetUnit(deployment.UnitId, out UnitData unit) && unit.currentHealth > 0)
            {
                hasCrew = true;

                break;

            }

        }

        if (!hasCrew)
        {
            SetMessage("Select at least one crew member before starting the mission.");

            return;

        }

        string scene = string.IsNullOrWhiteSpace(selectedMissionScene) ?
            testMissionScene : selectedMissionScene;
        if (scene.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
            !scene.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
        {
            scene += ".unity";

        }
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            SetMessage($"Mission '{scene}' is not in Build Settings / Build Profiles.");

            return;

        }

        SceneManager.LoadScene(scene);

    }

    private void OpenBarracks()
    {
        if (barracks == null)
        {
            barracks = FindAnyObjectByType<BarracksMenu>(FindObjectsInactive.Include);

        }

        if (barracks == null)
        {
            SetMessage("BarracksMenu was not found in the Hub scene.");

            return;

        }

        if (crewSelectRoot != null)
        {
            crewSelectRoot.SetActive(false);
        }
        barracks.Open();
        if (returnFromBarracksRoot != null)
        {
            returnFromBarracksRoot.SetActive(true);
        }

    }

    private void ReturnFromBarracks()
    {
        if (barracks != null)
        {
            barracks.Close();
        }
        OpenCrewSelection();

    }

    private void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

    }

#if UNITY_EDITOR
    public void ConfigureRaceMapUI(GameObject screen, Button orcButton, Button backButton)
    {
        raceMapRoot = screen;
        orcRegionButton = orcButton;
        raceMapBackButton = backButton;

    }

    public void ConfigureMissionListUI(Transform content, Button rowTemplate, TMP_Text emptyMessage)
    {
        missionListContent = content;
        missionRowTemplate = rowTemplate;
        emptyMissionText = emptyMessage;

    }

    public void AddMissionIfMissing(string name, string description, UnityEditor.SceneAsset scene)
    {
        if (scene == null)
        {

            return;

        }

        string path = UnityEditor.AssetDatabase.GetAssetPath(scene);
        foreach (MissionSceneEntry entry in missionScenes)
        {
            if (entry != null && entry.ScenePath == path)
            {

                return;

            }

        }

        // Serialized fields are set by the authoring tool to preserve Inspector editing.
        UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(this);
        UnityEditor.SerializedProperty entries = serialized.FindProperty("missionScenes");
        int index = entries.arraySize;
        entries.InsertArrayElementAtIndex(index);
        UnityEditor.SerializedProperty entryProperty = entries.GetArrayElementAtIndex(index);
        entryProperty.FindPropertyRelative("displayName").stringValue = name;
        entryProperty.FindPropertyRelative("description").stringValue = description;
        entryProperty.FindPropertyRelative("sceneAsset").objectReferenceValue = scene;
        entryProperty.FindPropertyRelative("scenePath").stringValue = path;
        serialized.ApplyModifiedPropertiesWithoutUndo();

    }

    // Called only by the editor authoring command when the persistent UI objects are created.
    public void ConfigureAuthoredUI(GameObject mission, GameObject crew, GameObject barracksReturn,
        Button missionButton, Button missionBack, Button crewBack, Button deploy,
        Button clearSlot, Button openBarracks, Button returnButton, TMP_Text message,
        CrewSlotView[] slots, Transform rosterContent, CrewCandidateRowUI rowPrefab,
        Sprite lockedSprite, List<CrewRaceIcon> icons)
    {
        missionSelectRoot = mission;
        crewSelectRoot = crew;
        returnFromBarracksRoot = barracksReturn;
        testMissionButton = missionButton;
        missionBackButton = missionBack;
        crewBackButton = crewBack;
        startMissionButton = deploy;
        clearSlotButton = clearSlot;
        openBarracksButton = openBarracks;
        returnFromBarracksButton = returnButton;
        messageText = message;
        crewSlots = slots;
        candidateContent = rosterContent;
        candidateRowPrefab = rowPrefab;
        lockedSlotSprite = lockedSprite;
        raceIcons = icons;

    }
#endif

}
