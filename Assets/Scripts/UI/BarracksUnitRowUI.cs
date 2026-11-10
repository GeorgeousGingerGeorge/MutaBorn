using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach to the UnitRow prefab authored in the Unity editor.
public sealed class BarracksUnitRowUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text raceText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image raceIcon;
    [SerializeField] private Image selectionBackground;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.63f, 0.83f, 1f, 1f);

    public void Bind(UnitData unit, Sprite icon, bool selected, bool deployed, Action<string> onSelected)
    {
        if (unit == null)
        {
            return;

        }

        if (nameText != null)
        {
            nameText.text = unit.characterName + (deployed ? " [Deployed]" : "");

        }

        if (raceText != null)
        {
            raceText.text = "";
            raceText.gameObject.SetActive(false);

        }

        if (levelText != null)
        {
            levelText.text = $"Lv {unit.Level}";

        }

        if (raceIcon != null)
        {
            raceIcon.sprite = icon;
            raceIcon.enabled = icon != null;
            raceIcon.raycastTarget = false;
            raceIcon.color = Color.white;
            raceIcon.preserveAspect = true;

        }

        // Preserve the row Image color set in the prefab. Selection is indicated
        // by bold text instead of tinting every row blue.
        if (nameText != null)
        {
            nameText.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;

        }

        if (button == null)
        {
            button = GetComponent<Button>();

        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            string id = unit.UnitId;
            button.onClick.AddListener(() => onSelected?.Invoke(id));
            button.interactable = true;

        }

    }
}
