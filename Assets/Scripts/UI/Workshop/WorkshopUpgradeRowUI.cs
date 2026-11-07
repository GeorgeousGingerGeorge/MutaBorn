using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WorkshopUpgradeRowUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;

    public void Configure(Button targetButton, TMP_Text targetLabel, Image targetIcon)
    {
        button = targetButton;
        label = targetLabel;
        icon = targetIcon;
    }

    public void Bind(EquipmentInstance item, string ownerName, Action choose)
    {
        if (label != null)
        {
            string name = !string.IsNullOrWhiteSpace(item.itemName) ? item.itemName : item.template.itemName;
            label.text = name + "  " + (int)item.rarity + "/5" +
                (string.IsNullOrEmpty(ownerName) ? "  [Storage]" : "  [" + ownerName + "]");
        }
        if (icon != null)
        {
            icon.sprite = item.template != null ? item.template.sprite : null;
            icon.enabled = icon.sprite != null;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }
        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => choose?.Invoke());
        }
    }
}
