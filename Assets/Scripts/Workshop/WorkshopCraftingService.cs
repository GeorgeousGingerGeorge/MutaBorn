using System;
using System.Collections.Generic;
using UnityEngine;

// Performs Workshop transactions. ResourceStockpile and ItemStorage stay the only owners of data.
public static class WorkshopCraftingService
{
    public static bool CanCraft(CraftingRecipe recipe, ResourceStockpile stockpile, ItemStorage storage,
        out string reason)
    {
        reason = string.Empty;
        if (recipe == null || stockpile == null || storage == null)
        {
            reason = "Workshop resources or item storage are not available.";
            return false;

        }

        if (!TryGetCosts(recipe, stockpile, out Dictionary<string, int> costs, out reason))
        {
            return false;

        }

        if (recipe.outputKind == CraftingOutputKind.EquipmentOrConsumable)
        {
            if (!(recipe.outputItem is EquipmentData) && !(recipe.outputItem is ConsumableData))
            {
                reason = "This item recipe has no valid equipment or consumable template.";
                return false;

            }

        }
        else
        {
            if (recipe.outputAmount <= 0 || stockpile.GetResourceData(recipe.outputResourceId) == null)
            {
                reason = "This material recipe has an invalid resource output.";
                return false;

            }

            long result = (long)stockpile.GetQuantity(recipe.outputResourceId) + recipe.outputAmount;
            if (costs.TryGetValue(recipe.outputResourceId, out int cost))
            {
                result -= cost;

            }

            if (result > int.MaxValue)
            {
                reason = "The output would exceed the resource quantity limit.";
                return false;

            }

        }

        foreach (KeyValuePair<string, int> cost in costs)
        {
            if (stockpile.GetQuantity(cost.Key) < cost.Value)
            {
                ResourceData definition = stockpile.GetResourceData(cost.Key);
                reason = $"Not enough {(definition == null ? cost.Key : definition.ResourceName)} " +
                    $"({stockpile.GetQuantity(cost.Key)}/{cost.Value}).";
                return false;

            }

        }

        return true;

    }

    public static bool TryCraft(CraftingRecipe recipe, ResourceStockpile stockpile, ItemStorage storage,
        out string message)
    {
        if (!CanCraft(recipe, stockpile, storage, out message))
        {
            return false;

        }

        if (!TryGetCosts(recipe, stockpile, out Dictionary<string, int> costs, out message))
        {
            return false;

        }

        List<KeyValuePair<string, int>> spent = new List<KeyValuePair<string, int>>();
        foreach (KeyValuePair<string, int> cost in costs)
        {
            if (!stockpile.RemoveResource(cost.Key, cost.Value))
            {
                Refund(stockpile, spent);
                message = "The stockpile changed; crafting was cancelled.";
                return false;

            }

            spent.Add(cost);

        }

        bool created = false;
        try
        {
            if (recipe.outputKind == CraftingOutputKind.Resource)
            {
                created = stockpile.AddResource(recipe.outputResourceId, recipe.outputAmount);
                if (created)
                {
                    stockpile.DiscoverResource(recipe.outputResourceId);

                }

            }
            else if (recipe.outputItem is EquipmentData equipment)
            {
                EquipmentInstance item = EquipmentGenerator.CreateEquipment(equipment);
                created = storage.TryStoreEquipment(item);

            }
            else if (recipe.outputItem is ConsumableData consumable)
            {
                ConsumableInstance item = ConsumableGenerator.CreateConsumable(consumable);
                created = storage.TryStoreConsumable(item);

            }

        }
        catch (Exception error)
        {
            Debug.LogException(error);

        }

        if (!created)
        {
            Refund(stockpile, spent);
            message = "Crafting failed. All spent materials were refunded.";
            return false;

        }

        message = $"Crafted {(string.IsNullOrWhiteSpace(recipe.displayName) ? recipe.recipeId : recipe.displayName)}.";
        return true;

    }

    private static bool TryGetCosts(CraftingRecipe recipe, ResourceStockpile stockpile,
        out Dictionary<string, int> costs, out string error)
    {
        costs = new Dictionary<string, int>(StringComparer.Ordinal);
        error = string.Empty;
        if (recipe.ingredients == null || recipe.ingredients.Count == 0)
        {
            error = "This recipe has no ingredients.";
            return false;

        }

        foreach (CraftingIngredient ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.amount <= 0 ||
                string.IsNullOrWhiteSpace(ingredient.resourceId) ||
                stockpile.GetResourceData(ingredient.resourceId) == null)
            {
                error = "Recipe contains an invalid ingredient resource or amount.";
                return false;

            }

            costs.TryGetValue(ingredient.resourceId, out int current);
            long combined = (long)current + ingredient.amount;
            if (combined > int.MaxValue)
            {
                error = "Ingredient quantity exceeds the supported limit.";
                return false;

            }

            costs[ingredient.resourceId] = (int)combined;

        }

        return true;

    }

    private static void Refund(ResourceStockpile stockpile, List<KeyValuePair<string, int>> spent)
    {
        foreach (KeyValuePair<string, int> entry in spent)
        {
            if (!stockpile.AddResource(entry.Key, entry.Value))
            {
                Debug.LogError($"CRITICAL: Could not refund crafting ingredient '{entry.Key}' x{entry.Value}.");

            }

        }

    }

}
