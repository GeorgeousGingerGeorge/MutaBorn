using System;
using System.Collections.Generic;
using System.Text;
using MutaBorn.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Authored overlay within the existing WorkshopScreen; only list rows are spawned at runtime.
public sealed class WorkshopUpgradeMenu : MonoBehaviour
{
    [Header("Upgrade Costs - editable starter economy")]
    [SerializeField] private WorkshopUpgradeCosts costs = new WorkshopUpgradeCosts();

    [Header("Existing Workshop and real Unity UI objects")]
    [SerializeField] private WorkshopMenu craftingMenu;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button openUpgradeButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Transform itemContent;
    [SerializeField] private WorkshopUpgradeRowUI rowPrefab;
    [SerializeField] private TMP_Text itemTitle;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemStatsText;
    [SerializeField] private TMP_Text scalingText;
    [SerializeField] private TMP_Text itemEffectsText;

    private readonly List<WorkshopUpgradeRowUI> rows = new List<WorkshopUpgradeRowUI>();
    private string selectedInstanceId;
    private string selectedOwnerId; // null = ItemStorage, otherwise the roster owner.
    private ResourceStockpile stockpile;
    private ItemStorage storage;
    private ItemStorage subscribedStorage;
    private ResourceStockpile subscribedStockpile;

    public void Configure(WorkshopMenu menu, GameObject panel, Button opener, Button back,
        Button close, Button upgrade, Transform content, WorkshopUpgradeRowUI prefab,
        TMP_Text title, TMP_Text cost, TMP_Text result, Image icon)
    {
        craftingMenu = menu;
        upgradePanel = panel;
        openUpgradeButton = opener;
        backButton = back;
        closeButton = close;
        upgradeButton = upgrade;
        itemContent = content;
        rowPrefab = prefab;
        itemTitle = title;
        costText = cost;
        resultText = result;
        itemIcon = icon;
    }

    private void Awake()
    {
        if (craftingMenu == null) craftingMenu = GetComponentInParent<WorkshopMenu>();
        if (openUpgradeButton != null) openUpgradeButton.onClick.AddListener(Open);
        if (backButton != null) backButton.onClick.AddListener(BackToCrafting);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(UpgradeSelected);
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    private void OnEnable() { Subscribe(); }
    private void OnDisable()
    {
        Unsubscribe();
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    private void Subscribe()
    {
        stockpile = ResourceStockpile.Instance;
        storage = ItemStorage.Instance;
        if (subscribedStorage != storage)
        {
            if (subscribedStorage != null) subscribedStorage.StorageChanged -= OnInventoryChanged;
            subscribedStorage = storage;
            if (subscribedStorage != null) subscribedStorage.StorageChanged += OnInventoryChanged;
        }
        if (subscribedStockpile != stockpile)
        {
            if (subscribedStockpile != null) subscribedStockpile.ResourceChanged -= OnResourceChanged;
            subscribedStockpile = stockpile;
            if (subscribedStockpile != null) subscribedStockpile.ResourceChanged += OnResourceChanged;
        }
    }

    private void Unsubscribe()
    {
        if (subscribedStorage != null) subscribedStorage.StorageChanged -= OnInventoryChanged;
        if (subscribedStockpile != null) subscribedStockpile.ResourceChanged -= OnResourceChanged;
        subscribedStorage = null;
        subscribedStockpile = null;
    }

    private void OnInventoryChanged() { if (upgradePanel != null && upgradePanel.activeSelf) Refresh(); }
    private void OnResourceChanged(ResourceEntry changed) { if (upgradePanel != null && upgradePanel.activeSelf) RefreshDetails(); }

    public void Open()
    {
        if (craftingMenu == null || upgradePanel == null) return;
        craftingMenu.Open();
        Subscribe();
        upgradePanel.SetActive(true);
        if (resultText != null) resultText.text = "Select equipment to improve its stars.";
        Refresh();
    }

    public void OpenForEquipped(string unitId, string instanceId)
    {
        selectedOwnerId = unitId;
        selectedInstanceId = instanceId;
        Open();
    }

    public void BackToCrafting()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    public void Close()
    {
        BackToCrafting();
        if (craftingMenu != null) craftingMenu.Close();
    }

    private void Select(string instanceId, string ownerId)
    {
        selectedInstanceId = instanceId;
        selectedOwnerId = ownerId;
        if (resultText != null) resultText.text = string.Empty;
        RefreshDetails();
    }

    private bool TrySelected(out EquipmentInstance item, out UnitData owner)
    {
        item = default(EquipmentInstance);
        owner = null;
        if (string.IsNullOrWhiteSpace(selectedInstanceId)) return false;
        if (string.IsNullOrEmpty(selectedOwnerId))
        {
            if (storage != null && storage.TryGetItem(selectedInstanceId, out StoredItem stored) &&
                stored.Kind == StoredItemKind.Equipment && stored.Equipment.template != null)
            {
                item = stored.Equipment;
                return true;
            }
            return false;
        }

        UnitRoster roster = UnitRoster.Instance;
        if (roster == null || !roster.TryGetUnit(selectedOwnerId, out owner)) return false;
        foreach (EquipmentInstance gear in owner.equippedItems)
        {
            if (!string.Equals(gear.instanceId, selectedInstanceId, StringComparison.Ordinal)) continue;
            item = gear;
            return gear.template != null;
        }
        return false;
    }

    private void Refresh()
    {
        Subscribe();
        foreach (WorkshopUpgradeRowUI row in rows)
            if (row != null) Destroy(row.gameObject);
        rows.Clear();

        if (rowPrefab == null || itemContent == null)
        {
            if (resultText != null) resultText.text = "Upgrade row prefab / Content is not assigned.";
            return;
        }

        if (storage != null)
        {
            foreach (StoredItem stored in storage.Items)
            {
                if (stored == null || stored.Kind != StoredItemKind.Equipment || stored.Equipment.template == null) continue;
                EquipmentInstance item = stored.Equipment;
                AddRow(item, null, null);
            }
        }

        UnitRoster roster = UnitRoster.Instance;
        if (roster != null)
        {
            foreach (UnitData unit in roster.Units)
            {
                if (unit == null) continue;
                foreach (EquipmentInstance item in unit.equippedItems)
                {
                    if (item.template != null) AddRow(item, unit.UnitId, unit.characterName);
                }
            }
        }
        RefreshDetails();
    }

    private void AddRow(EquipmentInstance item, string ownerId, string ownerName)
    {
        WorkshopUpgradeRowUI row = Instantiate(rowPrefab, itemContent);
        row.gameObject.SetActive(true);
        string capturedId = item.instanceId;
        string capturedOwner = ownerId;
        row.Bind(item, ownerName, () => Select(capturedId, capturedOwner));
        rows.Add(row);
    }

    private void RefreshDetails()
    {
        if (!TrySelected(out EquipmentInstance item, out UnitData owner))
        {
            if (itemTitle != null) itemTitle.text = "Select an equipment instance";
            if (costText != null) costText.text = string.Empty;
            if (itemIcon != null) itemIcon.enabled = false;
            if (itemStatsText != null) itemStatsText.text = string.Empty;
            if (scalingText != null) scalingText.text = string.Empty;
            if (itemEffectsText != null) itemEffectsText.text = string.Empty;
            if (upgradeButton != null) upgradeButton.interactable = false;
            return;
        }
        if (itemTitle != null)
            itemTitle.text = item.itemName + "  (" + (int)item.rarity + "/5)" +
                (owner != null ? "\nEquipped by " + owner.characterName : "\nIn Storage");
        if (itemIcon != null)
        {
            itemIcon.sprite = item.template.sprite;
            itemIcon.enabled = itemIcon.sprite != null;
            itemIcon.preserveAspect = true;
        }
        if (costText != null) costText.text = WorkshopUpgradeService.CostDescription(item, stockpile, costs);
        EquipmentStatValues baseStats = item.baseStats;
        EquipmentStatValues effective = EquipmentGenerator.GetEffectiveStats(item, owner);
        if (itemStatsText != null)
        {
            itemStatsText.text = "PERMANENT / " + (owner != null ? "EQUIPPED" : "STORED") + " STATS\n" +
                "Armor " + baseStats.armor + " / " + effective.armor +
                "    Ward " + baseStats.ward + " / " + effective.ward +
                "\nMelee " + baseStats.meleeDamage + " / " + effective.meleeDamage +
                "    Ranged " + baseStats.rangeDamage + " / " + effective.rangeDamage +
                "\nMagic " + baseStats.magicDamage + " / " + effective.magicDamage +
                "    Accuracy " + baseStats.accuracy + " / " + effective.accuracy;
        }
        if (scalingText != null)
        {
            EquipmentAttributeScaling grades = item.attributeScaling;
            string lines = "SCALING  " + (owner != null ?
                "+" + grades.TotalPercent(owner).ToString("0.#") + "%" : "equip to calculate") + "\n";
            float[] chances = grades.UpgradeChances();
            for (int i = 0; i < 6; i++)
                lines += EquipmentAttributeScaling.Name(i) + ": " + grades.Get(i) +
                    "  [" + (chances[i] * 100f).ToString("0.#") + "%]" +
                    (i < 5 ? "\n" : string.Empty);
            scalingText.text = lines;
        }
        if (itemEffectsText != null)
            itemEffectsText.text = FormatItemEffects(item);

        bool canUpgrade = WorkshopUpgradeService.CanUpgrade(item, stockpile, costs, out _);
        if (owner != null && UnitRoster.Instance != null && UnitRoster.Instance.IsUnitDeployed(owner.UnitId))
            canUpgrade = false;
        if (upgradeButton != null) upgradeButton.interactable = canUpgrade;
    }

    private static string FormatItemEffects(EquipmentInstance item)
    {
        StringBuilder details = new StringBuilder();
        AppendEffectGrants(details, item.grantedEffects);

        if (item.grantedEffectCollection != null)
            AppendEffectGrants(details, item.grantedEffectCollection.Effects);

        if (item.resistances != null)
        {
            foreach (DamageResistance resistance in item.resistances)
            {
                if (Mathf.Abs(resistance.Percent) > 0.001f)
                    details.Append(resistance.Type).Append(" Resistance: ")
                        .Append(resistance.Percent.ToString("+0.##;-0.##;0")).AppendLine("%");
            }
        }

        if (item.bonusDamage != null)
        {
            foreach (DamageComponent damage in item.bonusDamage)
            {
                if (damage.Amount != 0)
                    details.Append("Extra ").Append(damage.Type).Append(" Damage: ")
                        .AppendLine(damage.Amount.ToString());
            }
        }

        return details.Length == 0 ? "No effects or bonuses." : details.ToString().TrimEnd();
    }

    private static void AppendEffectGrants(StringBuilder details, IEnumerable<EffectGrant> grants)
    {
        if (grants == null) return;

        foreach (EffectGrant grant in grants)
        {
            if (grant != null && grant.Definition != null)
                details.Append("Effect: ").AppendLine(grant.Definition.DisplayName);
        }
    }

    public void UpgradeSelected()
    {
        Subscribe();
        if (!TrySelected(out EquipmentInstance item, out UnitData owner)) return;
        string message;
        bool success = owner == null
            ? WorkshopUpgradeService.TryUpgradeStored(item.instanceId, stockpile, storage, costs, out message)
            : WorkshopUpgradeService.TryUpgradeEquipped(owner, item.instanceId, stockpile, costs, out message);
        // Results persist after list refresh and contain EXACT values from the generated roll.
        Refresh();
        if (resultText != null) resultText.text = message;
        if (!success) Debug.LogWarning("[Workshop Upgrade] " + message, this);
    }
}
