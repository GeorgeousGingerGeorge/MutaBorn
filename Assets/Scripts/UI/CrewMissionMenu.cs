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
    [SerializeField] private GameObject missionSelectRoot;
    [SerializeField] private GameObject crewSelectRoot;
    [SerializeField] private GameObject returnFromBarracksRoot;
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
    private UnitRoster observedRoster;
    private int selectedSlot;

    private void Awake()
    {
        if (testMissionButton != null)
        {
            testMissionButton.onClick.AddListener(OpenCrewSelection);
        }
        if (missionBackButton != null)
        {
            missionBackButton.onClick.AddListener(CloseAll);
        }
        if (crewBackButton != null)
        {
            crewBackButton.onClick.AddListener(OpenMissionSelect);
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

    public void OpenMissionSelect()
    {
        Subscribe();
        if (missionSelectRoot != null)
        {
            missionSelectRoot.SetActive(true);
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
                    selected == null ? "+ Empty" : selected.characterName;
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
        SetMessage(selectedSlot >= roster.UnlockedCrewSlots ?
            "This slot requires a future research unlock." :
            $"Slot {selectedSlot + 1}: choose a unit from your roster, or clear it.");

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
            SetMessage("Unable to assign this unit (slot locked or unit currently deployed).");

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
                roster.TryGetUnit(deployment.UnitId, out UnitData unit))
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

        SceneManager.LoadScene(testMissionScene);

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
