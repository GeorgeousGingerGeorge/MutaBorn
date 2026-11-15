using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ResourceTradeOffer
{
    public string resourceId;
    [Min(1)] public int goldCost = 1;
    [Range(0, 3)] public int requiredTradeTier;
    public string seller;

}

[CreateAssetMenu(fileName = "ResourceTradeCatalog", menuName = "MutaBorn/Resource Trade Catalog")]
public sealed class ResourceTradeCatalog : ScriptableObject
{
    [SerializeField] private List<ResourceTradeOffer> offers = new List<ResourceTradeOffer>();

    public IReadOnlyList<ResourceTradeOffer> Offers => offers;

    public static bool TryPurchase(ResourceTradeOffer offer, int amount)
    {
        ResourceStockpile stockpile = ResourceStockpile.Instance;
        if (stockpile == null || offer == null || amount <= 0 || offer.goldCost <= 0 ||
            string.IsNullOrWhiteSpace(offer.resourceId) || offer.resourceId == "gold" ||
            (ResearchManager.Instance != null ?
                ResearchManager.Instance.BenefitTotal(ResearchBenefitType.TradeTier) : 0) < offer.requiredTradeTier)
        {
            return false;

        }

        ResourceData resource = stockpile.GetResourceData(offer.resourceId);
        ResourceEntry entry = stockpile.GetEntry(offer.resourceId);
        long totalCost = (long)offer.goldCost * amount;
        if (resource == null || totalCost > int.MaxValue ||
            (entry != null && entry.Quantity > int.MaxValue - amount) ||
            !stockpile.HasResource("gold", (int)totalCost))
        {
            return false;

        }

        if (!stockpile.RemoveResource("gold", (int)totalCost))
        {
            return false;

        }

        if (!stockpile.AddResource(offer.resourceId, amount))
        {
            stockpile.AddResource("gold", (int)totalCost);

            return false;

        }

        stockpile.DiscoverResource(offer.resourceId);

        return true;

    }

}
