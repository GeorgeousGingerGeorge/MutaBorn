using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Optional prefab for the equipment picker opened by Change.
public sealed class BarracksStoredItemRowUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image secondaryIcon;

    public void Bind(StoredItem item, Action<string> onSelected)
    {
        if (item == null)
        {
            return;

        }

        ItemData definition = item.Definition;
        if (itemNameText != null)
        {
            string name = item.Kind == StoredItemKind.Equipment &&
                !string.IsNullOrWhiteSpace(item.Equipment.itemName)
                ? item.Equipment.itemName
                : definition != null ? definition.itemName : "Missing Item";
            itemNameText.text = name;

        }

        if (itemIcon != null)
        {
            itemIcon.sprite = definition == null ? null : definition.sprite;
            itemIcon.enabled = itemIcon.sprite != null;
            itemIcon.raycastTarget = false;
            itemIcon.color = Color.white;
            itemIcon.preserveAspect = true;

        }

        if (secondaryIcon != null)
        {
            // Placeholder: same sprite as the main icon until separate category
            // artwork is authored. No new item data or extra inventory is created.
            secondaryIcon.sprite = definition == null ? null : definition.sprite;
            secondaryIcon.enabled = secondaryIcon.sprite != null;
            secondaryIcon.color = Color.white;
            secondaryIcon.preserveAspect = true;
            secondaryIcon.raycastTarget = false;

        }

        if (button == null)
        {
            button = GetComponent<Button>();

        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            string id = item.InstanceId;
            button.onClick.AddListener(() => onSelected?.Invoke(id));

        }

    }
}
