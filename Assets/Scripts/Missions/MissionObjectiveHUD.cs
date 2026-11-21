using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MissionObjectiveHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MissionObjectiveManager missionManager;
    [SerializeField] private TMP_Text objectiveText;

    [Header("Display")]
    [SerializeField] private bool showDescriptions;
    [SerializeField] private bool showMissionName = true;
    [SerializeField] private bool hideLockedObjectives = true;
    [SerializeField] private bool primaryStartsExpanded = true;
    [SerializeField] private bool optionalStartsExpanded;

    [Header("Section Layout")]
    [SerializeField] private float scrollHeight = 150f;
    [SerializeField] private float headerGap = 4f;
    [SerializeField] private float sectionGap = 4f;

    [Header("Editable Objective UI")]
    [SerializeField] private MissionObjectiveRow rowPrefab;
    [SerializeField] private Transform primaryContent;
    [SerializeField] private Transform optionalContent;
    [SerializeField] private GameObject primaryScroll;
    [SerializeField] private GameObject optionalScroll;
    [SerializeField] private Button primaryHeader;
    [SerializeField] private Button optionalHeader;
    [SerializeField] private TMP_Text primaryHeaderText;
    [SerializeField] private TMP_Text optionalHeaderText;

    [Header("Objective Type Icons")]
    [SerializeField] private Sprite eliminateIcon;
    [SerializeField] private Sprite targetIcon;
    [SerializeField] private Sprite defendIcon;
    [SerializeField] private Sprite breakIcon;
    [SerializeField] private Sprite interactIcon;
    [SerializeField] private Sprite lootIcon;
    [SerializeField] private Sprite rescueIcon;
    [SerializeField] private Sprite reachIcon;
    [SerializeField] private Sprite extractionIcon;

    [Header("Objective Status Icons")]
    [SerializeField] private Sprite passedIcon;
    [SerializeField] private Sprite failedIcon;
    [SerializeField] private Sprite inProgressIcon;

    private readonly Dictionary<string, MissionObjectiveRow> rows = new();
    private readonly StringBuilder content = new StringBuilder(512);
    private bool primaryExpanded = true;
    private bool optionalExpanded;
    private bool scrollPrimaryToBottom;

    private void Awake()
    {
        primaryExpanded = primaryStartsExpanded;
        optionalExpanded = optionalStartsExpanded;

        if (objectiveText == null)
        {
            objectiveText = GetComponentInChildren<TMP_Text>(true);

        }

    }

    private void OnEnable()
    {
        SubscribeToMission();
        if (primaryHeader != null)
        {
            primaryHeader.onClick.AddListener(TogglePrimary);

        }

        if (optionalHeader != null)
        {
            optionalHeader.onClick.AddListener(ToggleOptional);

        }

        RefreshSections();
        RefreshDisplay();

    }

    private void LateUpdate()
    {
        if (!scrollPrimaryToBottom || !primaryExpanded || primaryScroll == null ||
            !primaryScroll.activeInHierarchy)
        {
            return;

        }

        ScrollRect scroll = primaryScroll.GetComponent<ScrollRect>();
        RectTransform contentRect = primaryContent as RectTransform;
        if (scroll == null || contentRect == null)
        {
            scrollPrimaryToBottom = false;
            return;

        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = 0f;
        scrollPrimaryToBottom = false;

    }

    private void Start()
    {
        if (missionManager == null)
        {
            SubscribeToMission();

        }

        RefreshDisplay();

    }

    private void SubscribeToMission()
    {
        if (missionManager == null)
        {
            missionManager = MissionObjectiveManager.Instance;

        }

        if (missionManager == null)
        {
            missionManager = FindAnyObjectByType<MissionObjectiveManager>();

        }

        if (missionManager == null)
        {
            return;

        }

        missionManager.ObjectivesChanged -= RefreshDisplay;
        missionManager.ObjectivesChanged += RefreshDisplay;

    }

    public void RefreshDisplay()
    {
        if (objectiveText == null)
        {
            return;

        }

        if (missionManager == null)
        {
            objectiveText.text = "MISSION OBJECTIVES\nMission manager not found.";

            return;

        }

        if (rowPrefab != null && primaryContent != null && optionalContent != null)
        {
            RefreshRows();
            return;

        }

        content.Clear();

        if (showMissionName && missionManager.LevelMission != null &&
            !string.IsNullOrWhiteSpace(missionManager.LevelMission.MissionName))
        {
            content.Append("<b>").Append(missionManager.LevelMission.MissionName).AppendLine("</b>");

        }
        else
        {
            content.AppendLine("<b>MISSION OBJECTIVES</b>");

        }

        if (missionManager.Status == MissionStatus.NotStarted)
        {
            content.AppendLine("<color=#ABB4C0>Initializing mission...</color>");
            objectiveText.text = content.ToString();

            return;

        }

        if (missionManager.Status == MissionStatus.Victory)
        {
            content.AppendLine("<color=#83E3AB><b>MISSION COMPLETE</b></color>");

        }
        else if (missionManager.Status == MissionStatus.Defeat)
        {
            content.AppendLine("<color=#FF9494><b>MISSION FAILED</b></color>");

        }

        AppendGroup(false, "PRIMARY", "#9BCFFF");
        AppendGroup(true, "OPTIONAL", "#E9C88A");

        objectiveText.text = content.ToString();

    }

    private void RefreshRows()
    {
        if (missionManager.Status == MissionStatus.NotStarted)
        {
            objectiveText.text = "Initializing mission...";

            return;

        }

        string missionName = showMissionName && missionManager.LevelMission != null
            ? missionManager.LevelMission.MissionName : "MISSION OBJECTIVES";
        objectiveText.text = missionManager.Status == MissionStatus.Victory ? "MISSION COMPLETE" :
            missionManager.Status == MissionStatus.Defeat ? "MISSION FAILED" : missionName;

        HashSet<string> visibleIds = new();
        foreach (MissionObjectiveEntry objective in missionManager.Objectives)
        {
            if (objective == null || !objective.Visible)
            {
                continue;

            }

            if (hideLockedObjectives && objective.Status == MissionObjectiveStatus.Locked)
            {
                continue;

            }

            visibleIds.Add(objective.Id);
            if (!rows.TryGetValue(objective.Id, out MissionObjectiveRow row) || row == null)
            {
                row = Instantiate(rowPrefab, objective.Optional ? optionalContent : primaryContent);
                row.name = "Objective - " + objective.Id;
                rows[objective.Id] = row;

            }

            row.gameObject.SetActive(true);
            row.SetObjective(objective, GetTypeIcon(objective), passedIcon, failedIcon, inProgressIcon);

        }

        foreach (KeyValuePair<string, MissionObjectiveRow> item in rows)
        {
            if (item.Value != null && !visibleIds.Contains(item.Key))
            {
                item.Value.gameObject.SetActive(false);

            }

        }

        scrollPrimaryToBottom = true;

    }

    private Sprite GetTypeIcon(MissionObjectiveEntry objective)
    {
        if (objective.Template.WorldMarkerIcon != null)
        {
            return objective.Template.WorldMarkerIcon;

        }

        switch (objective.Type)
        {
            case MissionObjectiveType.ReachArea:
            {
                return reachIcon;

            }
            case MissionObjectiveType.ExtractPlayerUnits:
            {
                return extractionIcon;

            }
            case MissionObjectiveType.DestroyObjects:
            {
                return breakIcon;

            }
            case MissionObjectiveType.CaptureUnits:
            {
                return targetIcon;

            }
            case MissionObjectiveType.ProtectUnits:
            {
                return defendIcon;

            }
            case MissionObjectiveType.EliminateUnits:
            {
                return objective.Template.TargetFilter == MissionTargetFilter.TargetId ? targetIcon : eliminateIcon;

            }
            case MissionObjectiveType.InteractWithTargets:
            {
                if (objective.Template.InteractionId == "help")
                {
                    return rescueIcon;

                }

                if (objective.Title.ToLowerInvariant().Contains("loot") ||
                    objective.Title.ToLowerInvariant().Contains("chest"))
                {
                    return lootIcon;

                }

                return interactIcon;

            }
            default:
            {
                return null;

            }

        }

    }

    private void TogglePrimary()
    {
        primaryExpanded = !primaryExpanded;
        RefreshSections();
        if (primaryExpanded)
        {
            scrollPrimaryToBottom = true;

        }

    }

    private void ToggleOptional()
    {
        optionalExpanded = !optionalExpanded;
        RefreshSections();

    }

    private void RefreshSections()
    {
        RectTransform primaryHeaderRect = primaryHeader != null
            ? primaryHeader.GetComponent<RectTransform>() : null;
        RectTransform primaryScrollRect = primaryScroll != null
            ? primaryScroll.GetComponent<RectTransform>() : null;
        RectTransform optionalHeaderRect = optionalHeader != null
            ? optionalHeader.GetComponent<RectTransform>() : null;
        RectTransform optionalScrollRect = optionalScroll != null
            ? optionalScroll.GetComponent<RectTransform>() : null;

        float primaryTop = primaryHeaderRect != null ? primaryHeaderRect.anchoredPosition.y : -51f;
        float primaryHeaderHeight = primaryHeaderRect != null ? primaryHeaderRect.sizeDelta.y : 26f;
        float scrollTop = primaryTop - primaryHeaderHeight - headerGap;

        if (primaryScrollRect != null)
        {
            AlignTop(primaryScrollRect, scrollTop, scrollHeight);

        }

        if (primaryScroll != null)
        {
            primaryScroll.SetActive(primaryExpanded);

        }

        float optionalTop = primaryExpanded
            ? scrollTop - scrollHeight - sectionGap
            : primaryTop - primaryHeaderHeight - sectionGap;

        if (optionalHeaderRect != null)
        {
            AlignTop(optionalHeaderRect, optionalTop, optionalHeaderRect.sizeDelta.y);

        }

        float optionalHeaderHeight = optionalHeaderRect != null ? optionalHeaderRect.sizeDelta.y : 26f;

        if (optionalScrollRect != null)
        {
            AlignTop(optionalScrollRect, optionalTop - optionalHeaderHeight - headerGap, scrollHeight);

        }

        if (optionalScroll != null)
        {
            optionalScroll.SetActive(optionalExpanded);

        }

        if (primaryHeaderText != null)
        {
            primaryHeaderText.text = primaryExpanded ? "v  PRIMARY" : ">  PRIMARY";

        }

        if (optionalHeaderText != null)
        {
            optionalHeaderText.text = optionalExpanded ? "v  OPTIONAL" : ">  OPTIONAL";

        }

    }

    private static void AlignTop(RectTransform rect, float top, float height)
    {
        Vector2 min = rect.anchorMin;
        Vector2 max = rect.anchorMax;
        Vector2 pivot = rect.pivot;
        min.y = 1f;
        max.y = 1f;
        pivot.y = 1f;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, top);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);

    }

    // Use this from the prefab editor to preview the same default layout as Play Mode.
    public void ApplyDefaultSectionLayout()
    {
        primaryExpanded = primaryStartsExpanded;
        optionalExpanded = optionalStartsExpanded;
        RefreshSections();

    }

    private void AppendGroup(bool optional, string heading, string color)
    {
        bool hasVisibleEntries = false;

        foreach (MissionObjectiveEntry objective in missionManager.Objectives)
        {
            if (objective == null || !objective.Visible || objective.Optional != optional)
            {
                continue;

            }

            if (hideLockedObjectives && objective.Status == MissionObjectiveStatus.Locked)
            {
                continue;

            }

            if (!hasVisibleEntries)
            {
                content.AppendLine();
                content.Append("<color=").Append(color).Append("><b>")
                    .Append(heading).AppendLine("</b></color>");
                hasVisibleEntries = true;

            }

            AppendObjective(objective);

        }

    }

    private void AppendObjective(MissionObjectiveEntry objective)
    {
        string state;
        string titleColor;

        switch (objective.Status)
        {
            case MissionObjectiveStatus.Completed:
            {
                state = "[X]";
                titleColor = "#83E3AB";
                break;

            }
            case MissionObjectiveStatus.Failed:
            {
                state = "[!]";
                titleColor = "#FF9494";
                break;

            }
            case MissionObjectiveStatus.Locked:
            {
                state = "[-]";
                titleColor = "#9299A7";
                break;

            }
            default:
            {
                state = "[ ]";
                titleColor = "#F4F6FC";
                break;

            }

        }

        content.Append("<color=").Append(titleColor).Append('>')
            .Append(state).Append(' ').Append(objective.Title).AppendLine("</color>");

        if (showDescriptions && !string.IsNullOrWhiteSpace(objective.Description))
        {
            content.Append("    <color=#C7CDD8>")
                .Append(objective.Description).AppendLine("</color>");

        }

        content.Append("    <color=#B0BDCB>");

        if (objective.Type == MissionObjectiveType.ExtractPlayerUnits ||
            objective.Type == MissionObjectiveType.ReachArea)
        {
            content.Append(objective.Type == MissionObjectiveType.ReachArea ? "In area: " : "In extraction: ").Append(objective.Progress)
                .Append(" / ").Append(objective.Total);

            if (!objective.HasExtractionZone)
            {
                content.Append("  (Place matching zone)");

            }

        }
        else if (objective.Type == MissionObjectiveType.ProtectUnits)
        {
            content.Append("Keep designated unit alive");

        }
        else if (objective.Type == MissionObjectiveType.EliminateUnits ||
            objective.Type == MissionObjectiveType.CaptureUnits ||
            objective.Type == MissionObjectiveType.DestroyObjects)
        {
            content.Append("Remaining: ").Append(objective.Remaining)
                .Append(" / ").Append(objective.Total);

        }
        else
        {
            content.Append("Completed: ").Append(objective.Progress)
                .Append(" / ").Append(objective.Total);

        }

        if (objective.Status == MissionObjectiveStatus.Locked)
        {
            content.Append("  (Locked)");

        }

        content.AppendLine("</color>");

    }

    [ContextMenu("Test / Refresh Objective Display")]
    private void DebugRefresh()
    {
        RefreshDisplay();

    }

    private void OnDisable()
    {
        if (primaryHeader != null)
        {
            primaryHeader.onClick.RemoveListener(TogglePrimary);

        }

        if (optionalHeader != null)
        {
            optionalHeader.onClick.RemoveListener(ToggleOptional);

        }

        if (missionManager != null)
        {
            missionManager.ObjectivesChanged -= RefreshDisplay;

        }

    }

}
