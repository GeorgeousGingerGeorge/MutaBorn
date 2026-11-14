using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Authored Canvas objects; only repeated recipe rows are instantiated from a real UI prefab.
public sealed class WorkshopMenu : MonoBehaviour
{
    [Header("Recipes")]
    [SerializeField] private CraftingRecipeDatabase recipeDatabase;
    [Tooltip("Equipment Types whose embedded template recipes appear in this Workshop.")]
    [SerializeField] private List<EquipmentType> equipmentTypes = new List<EquipmentType>();
    [SerializeField] private Transform recipeContent;
    [SerializeField] private WorkshopRecipeRowUI recipeRowPrefab;

    [Header("Real Canvas UI")]
    [SerializeField] private GameObject workshopRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button craftButton;
    [SerializeField] private Button allFilterButton;
    [SerializeField] private Button equipmentFilterButton;
    [SerializeField] private Button consumableFilterButton;
    [SerializeField] private Button materialFilterButton;
    [SerializeField] private TMP_Text selectedTitle;
    [SerializeField] private TMP_Text selectedDescription;
    [SerializeField] private TMP_Text ingredientText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Image selectedIcon;

    private readonly List<WorkshopRecipeRowUI> rows = new List<WorkshopRecipeRowUI>();
    private readonly StringBuilder builder = new StringBuilder(300);
    private CraftingRecipe selectedRecipe;
    private ResourceStockpile stockpile;
    private ItemStorage storage;
    private int filter;

    public void Configure(CraftingRecipeDatabase recipes, GameObject root, Button open, Button close,
        Button craft, Button all, Button gear, Button consumables, Button materials,
        Transform content, WorkshopRecipeRowUI rowPrefab,
        TMP_Text title, TMP_Text description, TMP_Text ingredients, TMP_Text message, Image icon)
    {
        recipeDatabase = recipes;
        workshopRoot = root;
        openButton = open;
        closeButton = close;
        craftButton = craft;
        allFilterButton = all;
        equipmentFilterButton = gear;
        consumableFilterButton = consumables;
        materialFilterButton = materials;
        recipeContent = content;
        recipeRowPrefab = rowPrefab;
        selectedTitle = title;
        selectedDescription = description;
        ingredientText = ingredients;
        feedbackText = message;
        selectedIcon = icon;

    }

    private void Awake()
    {
        if (recipeDatabase == null) recipeDatabase = Resources.Load<CraftingRecipeDatabase>("CraftingRecipeDatabase");
        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (craftButton != null) craftButton.onClick.AddListener(CraftSelected);
        if (allFilterButton != null) allFilterButton.onClick.AddListener(() => ChangeFilter(0));
        if (equipmentFilterButton != null) equipmentFilterButton.onClick.AddListener(() => ChangeFilter(1));
        if (consumableFilterButton != null) consumableFilterButton.onClick.AddListener(() => ChangeFilter(2));
        if (materialFilterButton != null) materialFilterButton.onClick.AddListener(() => ChangeFilter(3));
        if (workshopRoot != null) workshopRoot.SetActive(false);

    }

    private void OnEnable()
    {
        Connect();

    }

    private void OnDisable()
    {
        Disconnect();

    }

    private void Update()
    {
        if (stockpile != ResourceStockpile.Instance || storage != ItemStorage.Instance)
        {
            Connect();
            if (workshopRoot != null && workshopRoot.activeSelf) RefreshDetails();

        }

    }

    private void Connect()
    {
        Disconnect();
        stockpile = ResourceStockpile.Instance;
        storage = ItemStorage.Instance;
        if (stockpile != null) stockpile.ResourceChanged += OnResourceChanged;
        if (storage != null) storage.StorageChanged += OnStorageChanged;

    }

    private void Disconnect()
    {
        if (stockpile != null) stockpile.ResourceChanged -= OnResourceChanged;
        if (storage != null) storage.StorageChanged -= OnStorageChanged;

    }

    private void OnResourceChanged(ResourceEntry changed) { RefreshDetails(); }
    private void OnStorageChanged() { RefreshDetails(); }

    public void Open()
    {
        Connect();
        if (workshopRoot == null) return;
        workshopRoot.SetActive(true);
        RebuildRows();
        RefreshDetails();

    }

    public void Close()
    {
        if (workshopRoot != null) workshopRoot.SetActive(false);

    }

    private void ChangeFilter(int next)
    {
        filter = next;
        RebuildRows();

    }

    private bool Matches(CraftingRecipe recipe)
    {
        if (filter == 0) return true;
        if (filter == 3) return recipe.outputKind == CraftingOutputKind.Resource;
        if (recipe.outputKind != CraftingOutputKind.EquipmentOrConsumable) return false;
        return filter == 1 ? recipe.outputEquipment.equipmentType != null
            : recipe.outputItem is ConsumableData;

    }

    private Sprite GetIcon(CraftingRecipe recipe)
    {
        if (recipe == null) return null;
        if (recipe.outputKind == CraftingOutputKind.EquipmentOrConsumable)
        {
            if (recipe.outputEquipment.TryResolve(out EquipmentTemplate template))
            {
                return template.TemplateIcon != null ? template.TemplateIcon
                    : recipe.outputEquipment.equipmentType.TypeIcon;

            }

            return recipe.outputItem == null ? null : recipe.outputItem.sprite;

        }

        ResourceData definition = stockpile != null ? stockpile.GetResourceData(recipe.outputResourceId) : null;
        return definition == null ? null : definition.Icon;

    }

    private void RebuildRows()
    {
        foreach (WorkshopRecipeRowUI row in rows)
        {
            if (row != null) Destroy(row.gameObject);

        }
        rows.Clear();

        if (recipeDatabase == null || recipeContent == null || recipeRowPrefab == null)
        {
            if (feedbackText != null) feedbackText.text = "CraftingRecipeDatabase or list prefab not assigned.";
            return;

        }

        if (selectedRecipe != null && !Matches(selectedRecipe)) selectedRecipe = null;

        List<EquipmentType> availableTypes = new List<EquipmentType>();
        foreach (EquipmentType configuredType in equipmentTypes)
        {
            if (configuredType != null && !availableTypes.Contains(configuredType))
            {
                availableTypes.Add(configuredType);

            }

        }
        foreach (EquipmentType type in Resources.LoadAll<EquipmentType>("MutaBorn/EquipmentTypes"))
        {
            if (type != null && !availableTypes.Contains(type))
            {
                availableTypes.Add(type);

            }

        }

        HashSet<string> migratedRecipes = new HashSet<string>();
        foreach (EquipmentType type in availableTypes)
        {
            if (type == null)
            {
                continue;

            }

            foreach (EquipmentTemplate template in type.Templates)
            {
                if (template != null && template.CraftingRecipe?.ingredients?.Count > 0 &&
                    !string.IsNullOrWhiteSpace(template.LegacyRecipeId))
                {
                    migratedRecipes.Add(template.LegacyRecipeId);

                }

            }

        }

        if (selectedRecipe != null && migratedRecipes.Contains(selectedRecipe.recipeId))
        {
            selectedRecipe = null;

        }

        foreach (CraftingRecipe recipe in recipeDatabase.Recipes)
        {
            if (recipe == null || migratedRecipes.Contains(recipe.recipeId) || !Matches(recipe)) continue;
            if (selectedRecipe == null) selectedRecipe = recipe;
            WorkshopRecipeRowUI row = Instantiate(recipeRowPrefab, recipeContent);
            row.gameObject.SetActive(true);
            row.Bind(recipe, GetIcon(recipe), SelectRecipe);
            rows.Add(row);

        }
        foreach (EquipmentType type in availableTypes)
        {
            if (type == null)
            {
                continue;

            }

            foreach (EquipmentTemplate template in type.Templates)
            {
                EquipmentCraftingRecipe embedded = template?.CraftingRecipe;
                if (embedded == null || embedded.ingredients == null || embedded.ingredients.Count == 0)
                {
                    continue;

                }

                CraftingRecipe recipe = new CraftingRecipe
                {
                    recipeId = "embedded." + type.TypeId + "." + template.TemplateId,
                    displayName = template.DisplayName,
                    description = template.Description,
                    outputKind = CraftingOutputKind.EquipmentOrConsumable,
                    outputEquipment = new EquipmentTemplateReference
                    {
                        equipmentType = type,
                        templateId = template.TemplateId
                    },
                    requiredCraftTier = embedded.requiredCraftTier,
                    ingredients = embedded.ingredients
                };

                if (!Matches(recipe))
                {
                    continue;

                }

                if (selectedRecipe == null)
                {
                    selectedRecipe = recipe;

                }

                WorkshopRecipeRowUI row = Instantiate(recipeRowPrefab, recipeContent);
                row.gameObject.SetActive(true);
                row.Bind(recipe, GetIcon(recipe), SelectRecipe);
                rows.Add(row);

            }

        }

        RefreshDetails();

    }

    private void SelectRecipe(CraftingRecipe recipe)
    {
        selectedRecipe = recipe;
        if (feedbackText != null) feedbackText.text = string.Empty;
        RefreshDetails();

    }

    private void RefreshDetails()
    {
        if (selectedRecipe == null)
        {
            if (selectedTitle != null) selectedTitle.text = "Select a recipe";
            if (selectedDescription != null) selectedDescription.text = string.Empty;
            if (ingredientText != null) ingredientText.text = "";
            if (selectedIcon != null) selectedIcon.enabled = false;
            if (craftButton != null) craftButton.interactable = false;
            return;

        }

        if (selectedTitle != null) selectedTitle.text = selectedRecipe.displayName;
        if (selectedDescription != null) selectedDescription.text = selectedRecipe.description;
        if (selectedIcon != null)
        {
            selectedIcon.sprite = GetIcon(selectedRecipe);
            selectedIcon.enabled = selectedIcon.sprite != null;

        }

        builder.Clear();
        if (selectedRecipe.ingredients != null)
        {
            foreach (CraftingIngredient ingredient in selectedRecipe.ingredients)
            {
                if (ingredient == null) continue;
                ResourceData data = stockpile != null ? stockpile.GetResourceData(ingredient.resourceId) : null;
                string name = data != null ? data.ResourceName : ingredient.resourceId;
                int owned = stockpile != null ? stockpile.GetQuantity(ingredient.resourceId) : 0;
                int amount = ResearchManager.Instance != null
                    ? ResearchManager.Instance.DiscountedCraftingCost(ingredient.amount) : ingredient.amount;
                builder.Append(owned >= amount ? "<color=#98E3AE>" : "<color=#FFAD99>")
                    .Append(name).Append(": ").Append(owned).Append(" / ")
                    .Append(amount).AppendLine("</color>");

            }

        }

        if (ingredientText != null) ingredientText.text = builder.ToString();
        bool canCraft = WorkshopCraftingService.CanCraft(selectedRecipe, stockpile, storage, out string reason);
        if (craftButton != null) craftButton.interactable = canCraft;
        if (feedbackText != null && !canCraft) feedbackText.text = reason;

    }

    public void CraftSelected()
    {
        if (selectedRecipe == null) return;
        bool success = WorkshopCraftingService.TryCraft(selectedRecipe, stockpile, storage, out string message);
        RefreshDetails();
        if (feedbackText != null) feedbackText.text = message;
        if (!success) Debug.LogWarning($"[Workshop] {message}", this);

    }

}
