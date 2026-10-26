using System.Text;
using TMPro;
using UnityEngine;

public sealed class MissionObjectiveHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MissionObjectiveManager missionManager;
    [SerializeField] private TMP_Text objectiveText;

    [Header("Display")]
    [SerializeField] private bool showDescriptions;
    [SerializeField] private bool showMissionName = true;

    private readonly StringBuilder content = new StringBuilder(512);

    private void Awake()
    {
        if (objectiveText == null)
        {
            objectiveText = GetComponentInChildren<TMP_Text>(true);

        }

    }

    private void OnEnable()
    {
        SubscribeToMission();
        RefreshDisplay();

    }

    private void Start()
    {
        // MissionObjectiveManager initializes its scene targets after UnitController.Start.
        // The ObjectivesChanged event will refresh the HUD when that completes.
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

        // Prevent duplicate subscriptions if OnEnable and Start both try to bind.
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

    private void AppendGroup(bool optional, string heading, string color)
    {
        bool hasVisibleEntries = false;

        foreach (MissionObjectiveEntry objective in missionManager.Objectives)
        {
            if (objective == null || !objective.Visible || objective.Optional != optional)
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

        if (objective.Type == MissionObjectiveType.EliminateUnits)
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
        if (missionManager != null)
        {
            missionManager.ObjectivesChanged -= RefreshDisplay;

        }

    }

}
