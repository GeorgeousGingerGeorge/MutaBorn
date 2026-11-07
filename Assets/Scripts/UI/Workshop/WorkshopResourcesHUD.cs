using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Place on a real Canvas. ResourceStockpile persists; the display does not own currency or materials.
public sealed class WorkshopResourcesHUD : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button nextCategoryButton;
    [SerializeField] private TMP_Text categoryLabel;
    [SerializeField] private GameObject dropdownPanel;
    [SerializeField] private Transform listContent;
    [SerializeField] private WorkshopResourceRowUI rowPrefab;
    [SerializeField] private bool showUndiscovered = true;

    private readonly List<WorkshopResourceRowUI> rows = new List<WorkshopResourceRowUI>();
    private ResourceStockpile subscribed;
    private int categoryIndex;

    public void Configure(Button toggle, Button close, Button next, TMP_Text label,
        GameObject panel, Transform content, WorkshopResourceRowUI prefab)
    {
        toggleButton = toggle;
        closeButton = close;
        nextCategoryButton = next;
        categoryLabel = label;
        dropdownPanel = panel;
        listContent = content;
        rowPrefab = prefab;

    }

    private void Awake()
    {
        if (toggleButton != null) toggleButton.onClick.AddListener(ToggleDropdown);
        if (closeButton != null) closeButton.onClick.AddListener(CloseDropdown);
        if (nextCategoryButton != null) nextCategoryButton.onClick.AddListener(NextCategory);
        if (dropdownPanel != null) dropdownPanel.SetActive(false);

    }

    private void OnEnable()
    {
        ObserveStockpile();
        Refresh();

    }

    private void Update()
    {
        if (subscribed != ResourceStockpile.Instance)
        {
            ObserveStockpile();
            Refresh();

        }

    }

    private void ObserveStockpile()
    {
        if (subscribed != null)
        {
            subscribed.ResourceChanged -= OnResourceChanged;

        }

        subscribed = ResourceStockpile.Instance;
        if (subscribed != null)
        {
            subscribed.ResourceChanged += OnResourceChanged;

        }

    }

    private void OnDisable()
    {
        if (subscribed != null) subscribed.ResourceChanged -= OnResourceChanged;
        subscribed = null;

    }

    private void OnResourceChanged(ResourceEntry changed)
    {
        Refresh();

    }

    public void ToggleDropdown()
    {
        if (dropdownPanel == null) return;
        dropdownPanel.SetActive(!dropdownPanel.activeSelf);
        if (dropdownPanel.activeSelf) Refresh();

    }

    public void CloseDropdown()
    {
        if (dropdownPanel != null) dropdownPanel.SetActive(false);

    }

    public void NextCategory()
    {
        categoryIndex = (categoryIndex + 1) % 6;
        Refresh();

    }

    public void Refresh()
    {
        if (categoryLabel != null)
        {
            categoryLabel.text = categoryIndex == 0 ? "ALL RESOURCES" : ((ResourceType)(categoryIndex - 1)).ToString().ToUpperInvariant();

        }

        // Hidden drawers do not need to rebuild all rows for every resource change.
        if (dropdownPanel != null && !dropdownPanel.activeSelf)
        {
            return;

        }

        foreach (WorkshopResourceRowUI row in rows)
        {
            if (row != null) Destroy(row.gameObject);

        }
        rows.Clear();

        if (subscribed == null || subscribed.Database == null || rowPrefab == null || listContent == null)
        {
            return;

        }

        foreach (ResourceData data in subscribed.Database.Resources)
        {
            if (data == null || (categoryIndex > 0 && data.ResourceType != (ResourceType)(categoryIndex - 1)))
            {
                continue;

            }

            ResourceEntry entry = subscribed.GetEntry(data.ResourceID);
            if (!showUndiscovered && (entry == null || !entry.IsDiscovered))
            {
                continue;

            }

            WorkshopResourceRowUI row = Instantiate(rowPrefab, listContent);
            row.gameObject.SetActive(true);
            row.Bind(data, entry, showUndiscovered);
            rows.Add(row);

        }

    }

}
