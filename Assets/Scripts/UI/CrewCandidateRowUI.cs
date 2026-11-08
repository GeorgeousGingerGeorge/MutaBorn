using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CrewCandidateRowUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image raceIcon;

    public void Bind(UnitData unit, Sprite icon, bool assigned, Action clicked)
    {
        if (nameText != null)
        {
            nameText.text = unit.characterName +
                (unit.currentHealth <= 0 ? "  [Fallen]" : assigned ? "  [Crew]" : "");

        }

        if (raceIcon != null)
        {
            raceIcon.sprite = icon;
            raceIcon.enabled = icon != null;
            raceIcon.color = Color.white;
            raceIcon.raycastTarget = false;

        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clicked?.Invoke());
            button.interactable = unit.currentHealth > 0;

        }

    }

#if UNITY_EDITOR
    public void ConfigureAuthoredUI(Button rowButton, TMP_Text label, Image icon)
    {
        button = rowButton;
        nameText = label;
        raceIcon = icon;

    }
#endif

}
