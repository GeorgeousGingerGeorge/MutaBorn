using System;
using System.Collections.Generic;
using UnityEngine;

public enum CraftingOutputKind
{
    EquipmentOrConsumable,
    Resource

}

[Serializable]
public sealed class CraftingIngredient
{
    public string resourceId;
    [Min(1)] public int amount = 1;

}

[Serializable]
public sealed class CraftingRecipe
{
    [Tooltip("Stable ID; existing saves should refer to this instead of the display name.")]
    public string recipeId;
    public string displayName;
    [TextArea(2, 4)] public string description;
    public CraftingOutputKind outputKind;
    public ItemData outputItem;
    public string outputResourceId;
    [Min(1)] public int outputAmount = 1;
    public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();

}

[CreateAssetMenu(fileName = "CraftingRecipeDatabase", menuName = "MutaBorn/Crafting Recipe Database")]
public sealed class CraftingRecipeDatabase : ScriptableObject
{
    [SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    public IReadOnlyList<CraftingRecipe> Recipes => recipes;

    public CraftingRecipe Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;

        }

        foreach (CraftingRecipe recipe in recipes)
        {
            if (recipe != null && string.Equals(recipe.recipeId, id, StringComparison.Ordinal))
            {
                return recipe;

            }

        }

        return null;

    }

#if UNITY_EDITOR
    public List<CraftingRecipe> EditorRecipes => recipes;
#endif

    private void OnValidate()
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (CraftingRecipe recipe in recipes)
        {
            if (recipe == null || string.IsNullOrWhiteSpace(recipe.recipeId))
            {
                Debug.LogWarning("Crafting recipe has no stable ID.", this);
                continue;

            }

            if (!ids.Add(recipe.recipeId))
            {
                Debug.LogWarning($"Duplicate crafting recipe ID: {recipe.recipeId}", this);

            }

        }

    }

}
