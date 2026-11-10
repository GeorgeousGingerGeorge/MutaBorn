using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HubStorageMenu : MonoBehaviour
{
    private enum Category
    {
        All,
        Weapons,
        Armor,
        Accessories,
        Consumables,
        Mutations

    }

    [Header("Storage Scroll")]
    [SerializeField] private Transform itemContent;
    [SerializeField] private BarracksStoredItemRowUI itemRowPrefab;

    [Header("Existing Category Buttons")]
    [SerializeField] private Button weaponsButton;
    [SerializeField] private Button armorButton;
    [SerializeField] private Button accessoriesButton;
    [SerializeField] private Button consumablesButton;
    [SerializeField] private Button mutationsButton;

    [Header("Existing Item Detail Screen")]
    [SerializeField] private BarracksItemPopup itemPopup;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemTypeText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private TMP_Text itemStatsText;
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image[] itemStars = new Image[5];
    [SerializeField] private Transform actionsContent;
    [SerializeField] private BarracksAbilityEntryUI actionRowPrefab;

    private readonly List<GameObject> generatedRows = new List<GameObject>();
    private readonly List<GameObject> generatedActions = new List<GameObject>();
    private ItemStorage subscribedStorage;
    private Category selectedCategory = Category.All;

    private void Awake()
    {
        if (weaponsButton != null)
        {
            weaponsButton.onClick.AddListener(() => SelectCategory(Category.Weapons));

        }

        if (armorButton != null)
        {
            armorButton.onClick.AddListener(() => SelectCategory(Category.Armor));

        }

        if (accessoriesButton != null)
        {
            accessoriesButton.onClick.AddListener(() => SelectCategory(Category.Accessories));

        }

        if (consumablesButton != null)
        {
            consumablesButton.onClick.AddListener(() => SelectCategory(Category.Consumables));

        }

        if (mutationsButton != null)
        {
            mutationsButton.onClick.AddListener(() => SelectCategory(Category.Mutations));

        }

        ConfigureGrid();
        if (itemPopup != null)
        {
            itemPopup.Hide();

        }

    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();

    }

    private void Start()
    {
        if (subscribedStorage == null)
        {
            Subscribe();
            Refresh();

        }

    }

    private void OnDisable()
    {
        if (subscribedStorage != null)
        {
            subscribedStorage.StorageChanged -= Refresh;
            subscribedStorage = null;

        }

    }

    public void ShowAll()
    {
        SelectCategory(Category.All);

    }

    private void Subscribe()
    {
        if (subscribedStorage == ItemStorage.Instance)
        {
            return;

        }

        if (subscribedStorage != null)
        {
            subscribedStorage.StorageChanged -= Refresh;

        }

        subscribedStorage = ItemStorage.Instance;
        if (subscribedStorage != null)
        {
            subscribedStorage.StorageChanged += Refresh;

        }

    }

    private void SelectCategory(Category category)
    {
        selectedCategory = category;
        if (itemPopup != null)
        {
            itemPopup.Hide();

        }

        Refresh();

    }

    private void Refresh()
    {
        Clear(generatedRows);
        if (itemContent == null || itemRowPrefab == null)
        {
            return;

        }

        ItemStorage storage = ItemStorage.Instance;
        if (storage == null)
        {
            AddMessage("Storage unavailable");

            return;

        }

        foreach (StoredItem item in storage.Items)
        {
            if (item == null || !Matches(item))
            {
                continue;

            }

            BarracksStoredItemRowUI row = Instantiate(itemRowPrefab, itemContent);
            row.gameObject.SetActive(true);
            row.Bind(item, ShowItem);
            generatedRows.Add(row.gameObject);

        }

        if (selectedCategory == Category.Mutations || selectedCategory == Category.All)
        {
            if (storage.MutationStorageUnlocked)
            {
                foreach (ActiveMutationRecord mutation in storage.Mutations)
                {
                    if (mutation != null)
                    {
                        AddMutationRow(mutation);

                    }

                }

            }
            else if (selectedCategory == Category.Mutations)
            {
                AddMessage("Mutation storage is locked");

            }

        }

        if (generatedRows.Count == 0)
        {
            AddMessage("No items in this category");

        }

    }

    private bool Matches(StoredItem item)
    {
        if (selectedCategory == Category.All)
        {
            return true;

        }

        if (selectedCategory == Category.Consumables)
        {
            return item.Kind == StoredItemKind.Consumable;

        }

        if (selectedCategory == Category.Mutations || item.Kind != StoredItemKind.Equipment)
        {
            return false;

        }

        EquipmentData equipment = item.Equipment.template;
        if (equipment == null)
        {
            return false;

        }

        EquipmentFitType fit = equipment.EquipmentFit;
        if (selectedCategory == Category.Weapons)
        {
            return fit == EquipmentFitType.Hand || equipment.weaponGear != WeaponCategory.None;

        }

        if (selectedCategory == Category.Accessories)
        {
            return fit == EquipmentFitType.Accessory;

        }

        return fit != EquipmentFitType.Hand && fit != EquipmentFitType.Accessory;

    }

    private void ShowItem(string instanceId)
    {
        if (ItemStorage.Instance == null ||
            !ItemStorage.Instance.TryGetItem(instanceId, out StoredItem item))
        {
            return;

        }

        ItemData definition = item.Definition;
        string name = definition == null ? "Missing Item" : definition.itemName;
        string description = definition == null ? "" : definition.description;
        Sprite sprite = definition == null ? null : definition.sprite;
        string type;
        string stats;
        int stars = 0;
        List<ActionDefinition> actions = new List<ActionDefinition>();

        if (item.Kind == StoredItemKind.Equipment)
        {
            EquipmentInstance equipment = item.Equipment;
            EquipmentData template = equipment.template;
            name = string.IsNullOrWhiteSpace(equipment.itemName) ? name : equipment.itemName;
            type = template == null ? "Equipment" : template.DisplayTypeLabel;
            stars = (int)equipment.rarity;
            EquipmentStatValues values = EquipmentGenerator.GetEffectiveStats(equipment, null);
            StringBuilder info = new StringBuilder();
            info.AppendLine("Stored values (before wielder scaling)");
            info.AppendLine("Armor: " + values.armor + "  Ward: " + values.ward);
            info.AppendLine("Melee: " + values.meleeDamage + "  Ranged: " + values.rangeDamage);
            info.AppendLine("Magic: " + values.magicDamage + "  Accuracy: " + values.accuracy);
            info.AppendLine("Movement penalty: " + values.movementPenalty);
            stats = info.ToString().TrimEnd();
            if (equipment.grantedActionSet != null && equipment.grantedActionSet.Actions != null)
            {
                actions.AddRange(equipment.grantedActionSet.Actions);

            }

            if (equipment.grantedActions != null)
            {
                actions.AddRange(equipment.grantedActions);

            }

        }
        else
        {
            ConsumableInstance consumable = item.Consumable;
            type = "Consumable";
            stats = "Uses: " + consumable.remainingUses + "/" +
                (consumable.definition == null ? 0 : consumable.definition.MaxUses) +
                "\nMagnitude: " + consumable.magnitude + "\nDamage: " + consumable.damage;
            if (consumable.definition != null && consumable.definition.UseAction != null)
            {
                actions.Add(consumable.definition.UseAction);

            }

        }

        SetDetails(name, type, description, stats, sprite, stars);
        ShowActions(actions);
        if (itemPopup != null)
        {
            itemPopup.Show();

        }

    }

    private void AddMutationRow(ActiveMutationRecord mutation)
    {
        BarracksStoredItemRowUI row = Instantiate(itemRowPrefab, itemContent);
        row.gameObject.SetActive(true);
        row.enabled = false;
        TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = mutation.MutationId.ToString();

        }

        Button button = row.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowMutation(mutation));

        }

        generatedRows.Add(row.gameObject);

    }

    private void ShowMutation(ActiveMutationRecord mutation)
    {
        SetDetails(mutation.MutationId.ToString(), "Stored Mutation",
            "Mutation kept in the separate mutation vault.",
            "Progress points: " + mutation.ProgressPoints, null, 0);
        ShowActions(null);
        if (itemPopup != null)
        {
            itemPopup.Show();

        }

    }

    private void AddMessage(string message)
    {
        BarracksStoredItemRowUI row = Instantiate(itemRowPrefab, itemContent);
        row.gameObject.SetActive(true);
        row.enabled = false;
        TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = message;

        }

        Button button = row.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = false;

        }

        generatedRows.Add(row.gameObject);

    }

    private void SetDetails(string name, string type, string description, string stats,
        Sprite sprite, int stars)
    {
        if (itemNameText != null)
        {
            itemNameText.text = name;

        }

        if (itemTypeText != null)
        {
            itemTypeText.text = type;

        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = description;

        }

        if (itemStatsText != null)
        {
            itemStatsText.text = stats;

        }

        if (itemIcon != null)
        {
            itemIcon.sprite = sprite;
            itemIcon.enabled = sprite != null;
            itemIcon.raycastTarget = false;

        }

        for (int i = 0; i < itemStars.Length; i++)
        {
            if (itemStars[i] != null)
            {
                itemStars[i].color = i < stars ?
                    new Color(1f, 0.78f, 0.10f) : Color.black;

            }

        }

    }

    private void ShowActions(List<ActionDefinition> actions)
    {
        Clear(generatedActions);
        if (actionsContent == null || actionRowPrefab == null || actions == null)
        {
            return;

        }

        foreach (ActionDefinition action in actions)
        {
            if (action != null)
            {
                BarracksAbilityEntryUI row = Instantiate(actionRowPrefab, actionsContent);
                row.gameObject.SetActive(true);
                row.Bind(action);
                generatedActions.Add(row.gameObject);

            }

        }

    }

    private void ConfigureGrid()
    {
        if (itemContent == null)
        {
            return;

        }

        GridLayoutGroup grid = itemContent.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = itemContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(106f, 106f);
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;

        }

        ContentSizeFitter fitter = itemContent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = itemContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        }

    }

    private static void Clear(List<GameObject> objects)
    {
        foreach (GameObject entry in objects)
        {
            if (entry != null)
            {
                Destroy(entry);

            }

        }

        objects.Clear();

    }

}
