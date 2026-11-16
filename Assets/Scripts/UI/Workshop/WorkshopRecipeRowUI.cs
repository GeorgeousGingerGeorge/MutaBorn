using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WorkshopRecipeRowUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Image typeBadge;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text category;

    public void Configure(Button buttonField, Image iconField, TMP_Text titleField, TMP_Text categoryField)
    {
        button = buttonField;
        icon = iconField;
        title = titleField;
        category = categoryField;

    }

    public void Bind(CraftingRecipe recipe, Sprite sprite, Action<CraftingRecipe> select)
    {
        title.text = string.IsNullOrWhiteSpace(recipe.displayName) ? recipe.recipeId : recipe.displayName;
        category.text = recipe.outputKind == CraftingOutputKind.Resource ? "MATERIAL" :
            recipe.outputItem is ConsumableData ? "CONSUMABLE" : "EQUIPMENT";
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        if (typeBadge != null)
        {
            Sprite typeIcon = recipe.outputKind == CraftingOutputKind.EquipmentOrConsumable &&
                recipe.outputEquipment.equipmentType != null
                ? recipe.outputEquipment.equipmentType.TypeIcon : null;
            typeBadge.sprite = typeIcon;
            typeBadge.enabled = typeIcon != null;

        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => select?.Invoke(recipe));

    }

}
