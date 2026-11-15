using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HubStorageTrading : MonoBehaviour
{
    [Header("Trading Menu (Hub scene)")]
    [SerializeField] private Button tradeButton;
    [SerializeField] private TMP_Text tradeButtonLabel;
    [SerializeField] private GameObject itemsScroll;
    [SerializeField] private GameObject tradingScroll;
    [SerializeField] private RectTransform offersContent;
    [SerializeField] private TMP_Text goldBalance;
    [SerializeField] private TMP_Text purchaseMessage;

    [Header("Editable Offer Prefab")]
    [SerializeField] private HubStorageTradeRow offerRowPrefab;
    [SerializeField] private ResourceTradeCatalog catalog;

    private readonly List<HubStorageTradeRow> rows = new List<HubStorageTradeRow>();
    private ResourceStockpile stockpile;
    private ResearchManager research;
    private Action openTrading;
    private Action closeTrading;
    private bool visible;
    private bool purchasing;
    private string lastPurchase = string.Empty;

    public void Configure(Action open, Action close)
    {
        openTrading = open;
        closeTrading = close;
        if (catalog == null)
        {
            catalog = Resources.Load<ResourceTradeCatalog>("ResourceTradeCatalog");

        }

        if (tradeButton != null)
        {
            tradeButton.onClick.AddListener(ToggleTrading);

        }

        SetVisible(false);
        Observe();

    }

    private void OnEnable()
    {
        Observe();
        if (visible)
        {
            Refresh();

        }

    }

    private void OnDisable()
    {
        if (stockpile != null)
        {
            stockpile.ResourceChanged -= OnResourcesChanged;
            stockpile = null;

        }

        if (research != null)
        {
            research.StateChanged -= OnResearchChanged;
            research = null;

        }

    }

    private void ToggleTrading()
    {
        if (visible)
        {
            closeTrading?.Invoke();

        }
        else
        {
            openTrading?.Invoke();

        }

    }

    public void SetVisible(bool show)
    {
        visible = show;
        if (itemsScroll != null)
        {
            itemsScroll.SetActive(!show);

        }

        if (tradingScroll != null)
        {
            tradingScroll.SetActive(show);

        }

        if (tradeButtonLabel != null)
        {
            tradeButtonLabel.text = show ? "ITEMS" : "TRADE";

        }

        if (show)
        {
            Observe();
            Refresh();

        }

    }

    private void Observe()
    {
        if (stockpile != ResourceStockpile.Instance)
        {
            if (stockpile != null)
            {
                stockpile.ResourceChanged -= OnResourcesChanged;

            }

            stockpile = ResourceStockpile.Instance;
            if (stockpile != null)
            {
                stockpile.ResourceChanged += OnResourcesChanged;

            }

        }

        if (research != ResearchManager.Instance)
        {
            if (research != null)
            {
                research.StateChanged -= OnResearchChanged;

            }

            research = ResearchManager.Instance;
            if (research != null)
            {
                research.StateChanged += OnResearchChanged;

            }

        }

    }

    private void OnResourcesChanged(ResourceEntry changed)
    {
        if (visible && !purchasing)
        {
            Refresh();

        }

    }

    private void OnResearchChanged()
    {
        if (visible)
        {
            Refresh();

        }

    }

    public void Refresh()
    {
        if (!visible || offersContent == null)
        {
            return;

        }

        foreach (HubStorageTradeRow row in rows)
        {
            if (row != null)
            {
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);

            }

        }

        rows.Clear();
        if (goldBalance != null)
        {
            goldBalance.text = "GOLD: " + (stockpile != null ? stockpile.GetQuantity("gold") : 0);

        }

        if (purchaseMessage != null)
        {
            purchaseMessage.text = string.IsNullOrEmpty(lastPurchase)
                ? "Buy resources to support your crew" : lastPurchase;

        }

        if (stockpile == null || stockpile.Database == null || catalog == null || offerRowPrefab == null)
        {
            if (purchaseMessage != null)
            {
                purchaseMessage.text = "Trading unavailable: missing stockpile, catalog, or offer prefab";

            }

            return;

        }

        int tier = research != null ? research.BenefitTotal(ResearchBenefitType.TradeTier) : 0;
        int gold = stockpile.GetQuantity("gold");
        foreach (ResourceTradeOffer offer in catalog.Offers)
        {
            if (offer == null || offer.goldCost <= 0 || offer.resourceId == "gold")
            {
                continue;

            }

            ResourceData resource = stockpile.GetResourceData(offer.resourceId);
            if (resource == null)
            {
                continue;

            }

            HubStorageTradeRow row = Instantiate(offerRowPrefab, offersContent, false);
            row.gameObject.SetActive(true);
            row.Bind(resource, offer, stockpile.GetQuantity(offer.resourceId), gold,
                tier, UnlockName(offer.requiredTradeTier), Buy);
            rows.Add(row);

        }

    }

    private void Buy(ResourceTradeOffer offer, int amount)
    {
        purchasing = true;
        bool bought = ResourceTradeCatalog.TryPurchase(offer, amount);
        purchasing = false;
        if (bought)
        {
            ResourceData resource = stockpile.GetResourceData(offer.resourceId);
            lastPurchase = "Purchased " + amount + " " + resource.ResourceName;

        }

        Refresh();

    }

    private static string UnlockName(int tier)
    {
        switch (tier)
        {
            case 1:
            {
                return "REGIONAL TRADE ROUTES";

            }
            case 2:
            {
                return "FACTION TRADE AGREEMENTS";

            }
            default:
            {
                return "FORBIDDEN COMMERCE";

            }

        }

    }

}
