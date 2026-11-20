using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MissionObjectiveRow : MonoBehaviour
{
    [SerializeField] private Image typeIcon;
    [SerializeField] private Image statusIcon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text progressText;

    public void SetObjective(
        MissionObjectiveEntry objective,
        Sprite icon,
        Sprite passed,
        Sprite failed,
        Sprite inProgress)
    {
        typeIcon.sprite = icon;
        typeIcon.enabled = icon != null;
        titleText.text = objective.Title;

        switch (objective.Status)
        {
            case MissionObjectiveStatus.Completed:
            {
                statusIcon.sprite = passed;
                statusIcon.enabled = passed != null;
                statusIcon.color = Color.white;

                break;

            }
            case MissionObjectiveStatus.Failed:
            {
                statusIcon.sprite = failed;
                statusIcon.enabled = failed != null;
                statusIcon.color = Color.white;

                break;

            }
            case MissionObjectiveStatus.Locked:
            {
                statusIcon.sprite = inProgress;
                statusIcon.enabled = inProgress != null;
                statusIcon.color = new Color(0.45f, 0.47f, 0.53f, 1f);

                break;

            }
            default:
            {
                statusIcon.sprite = inProgress;
                statusIcon.enabled = inProgress != null;
                statusIcon.color = Color.white;

                break;

            }

        }

        if (objective.Type == MissionObjectiveType.ProtectUnits)
        {
            progressText.text = "Keep designated unit alive";

        }
        else if (objective.Type == MissionObjectiveType.EliminateUnits ||
                 objective.Type == MissionObjectiveType.CaptureUnits ||
                 objective.Type == MissionObjectiveType.DestroyObjects)
        {
            progressText.text = $"Remaining: {objective.Remaining} / {objective.Total}";

        }
        else
        {
            string prefix = objective.Type == MissionObjectiveType.ExtractPlayerUnits ? "In extraction" :
                objective.Type == MissionObjectiveType.ReachArea ? "In area" : "Completed";
            progressText.text = $"{prefix}: {objective.Progress} / {objective.Total}";

        }

    }

}
