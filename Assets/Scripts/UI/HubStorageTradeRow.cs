using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HubStorageTradeRow : MonoBehaviour
{
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TMP_Text resourceName;
    [SerializeField] private TMP_Text sellerName;
    [SerializeField] private TMP_Text ownedQuantity;
    [SerializeField] private TMP_Text unitPrice;
    [SerializeField] private Button buyOneButton;
    [SerializeField] private TMP_Text buyOneLabel;
    [SerializeField] private Button buyFiveButton;
    [SerializeField] private TMP_Text buyFiveLabel;

    public void Bind(ResourceData resource, ResourceTradeOffer offer, int owned, int gold,
        int unlockedTier, string unlockName, Action<ResourceTradeOffer, int> purchase)
    {
        bool unlocked = unlockedTier >= offer.requiredTradeTier;
        if (resourceIcon != null)
        {
            resourceIcon.sprite = resource.Icon;

        }

        resourceName.text = resource.ResourceName;
        sellerName.text = unlocked ? offer.seller : "RESEARCH: " + unlockName;
        sellerName.color = unlocked ? Color.white : new Color(0.95f, 0.74f, 0.45f);
        ownedQuantity.text = "OWNED: " + owned;
        unitPrice.text = "PRICE: " + offer.goldCost + "G";

        buyOneButton.interactable = unlocked && gold >= offer.goldCost;
        buyFiveButton.interactable = unlocked && (long)gold >= (long)offer.goldCost * 5;
        buyOneLabel.text = "BUY 1";
        buyFiveLabel.text = "BUY 5";
        buyOneButton.onClick.RemoveAllListeners();
        buyFiveButton.onClick.RemoveAllListeners();
        buyOneButton.onClick.AddListener(() => purchase(offer, 1));
        buyFiveButton.onClick.AddListener(() => purchase(offer, 5));

    }

}
