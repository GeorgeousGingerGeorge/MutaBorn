using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WorkshopResourceRowUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text resourceName;
    [SerializeField] private TMP_Text quantity;

    public void Configure(Image image, TMP_Text nameText, TMP_Text quantityText)
    {
        icon = image;
        resourceName = nameText;
        quantity = quantityText;

    }

    public void Bind(ResourceData data, ResourceEntry entry, bool showUndiscovered)
    {
        bool known = entry != null && entry.IsDiscovered;
        resourceName.text = known || showUndiscovered ? data.ResourceName : "UNKNOWN";
        quantity.text = known || showUndiscovered ? (entry == null ? "0" : entry.Quantity.ToString()) : "?";
        icon.sprite = known || showUndiscovered ? data.Icon : null;
        icon.enabled = icon.sprite != null;

    }

}
