using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The screen is authored and saved in Unity; only repeated research rows are instantiated.
public sealed class ResearchMenu : MonoBehaviour
{
    [Header("Authored menu")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button equipmentTab;
    [SerializeField] private Button progressionTab;
    [SerializeField] private Button mutationTab;
    [SerializeField] private Button specialTab;
    [SerializeField] private Transform rowContent;
    [SerializeField] private Button rowPrefab;
    [SerializeField] private TMP_Text headingText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button startButton;

    [Header("Debug (Editor / Development Build)")]
    [SerializeField] private Button unlockAllResearchButton;

    [Header("Research completed notification (outside hidden panel)")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private Button notificationLabButton;
    [SerializeField] private Button notificationCloseButton;

    private readonly List<Button> rows = new List<Button>();
    private readonly StringBuilder details = new StringBuilder();
    private ResearchManager manager;
    private ResearchNode selected;
    private ResearchCategory category = ResearchCategory.EquipmentAndConsumables;

    private void Awake()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(Open);

        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);

        }
        if (equipmentTab != null)
        {
            equipmentTab.onClick.AddListener(() => ChangeTab(ResearchCategory.EquipmentAndConsumables));

        }
        if (progressionTab != null)
        {
            progressionTab.onClick.AddListener(() => ChangeTab(ResearchCategory.Progression));

        }
        if (mutationTab != null)
        {
            mutationTab.onClick.AddListener(() => ChangeTab(ResearchCategory.Mutations));

        }
        if (specialTab != null)
        {
            specialTab.gameObject.SetActive(false);

        }
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartSelected);

        }
        if (unlockAllResearchButton != null)
        {
            if (Application.isEditor || Debug.isDebugBuild)
            {
                unlockAllResearchButton.onClick.AddListener(DebugUnlockAllResearch);

            }
            else
            {
                unlockAllResearchButton.gameObject.SetActive(false);

            }

        }
        if (notificationLabButton != null)
        {
            notificationLabButton.onClick.AddListener(OpenFromNotification);

        }
        if (notificationCloseButton != null)
        {
            notificationCloseButton.onClick.AddListener(CloseNotification);

        }
        if (panel != null)
        {
            panel.SetActive(false);

        }
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);

        }

    }

    private void OnEnable()
    {
        Observe();

    }

    private void Start()
    {
        Observe();
        Refresh();

    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.StateChanged -= Refresh;
            manager = null;

        }

    }

    private void Observe()
    {
        if (manager == ResearchManager.Instance)
        {

            return;

        }

        if (manager != null)
        {
            manager.StateChanged -= Refresh;

        }

        manager = ResearchManager.Instance;
        if (manager != null)
        {
            manager.StateChanged += Refresh;

        }

    }

    public void Open()
    {
        Observe();
        if (panel != null)
        {
            panel.SetActive(true);

        }

        Refresh();

    }

    public void Close()
    {
        if (panel != null)
        {
            panel.SetActive(false);

        }

    }

    private void ChangeTab(ResearchCategory next)
    {
        category = next;
        selected = null;
        Refresh();

    }

    private void Select(ResearchNode node)
    {
        selected = node;
        RefreshDetails();

    }

    private void Refresh()
    {
        Observe();
        if (mutationTab != null)
        {
            bool unlocked = manager == null || manager.HasBenefit(ResearchBenefitType.MutationResearch);
            mutationTab.gameObject.SetActive(unlocked);
            if (!unlocked && category == ResearchCategory.Mutations)
            {
                category = ResearchCategory.Progression;
                selected = null;

            }

        }

        RefreshNotification();
        if (panel == null || !panel.activeSelf)
        {

            return;

        }

        foreach (Button row in rows)
        {
            if (row != null)
            {
                Destroy(row.gameObject);

            }

        }

        rows.Clear();
        if (manager == null || manager.Database == null || rowContent == null || rowPrefab == null)
        {
            if (headingText != null)
            {
                headingText.text = "RESEARCH UNAVAILABLE";

            }
            if (statusText != null)
            {
                statusText.text = "Assign ResearchManager and its database.";

            }
            if (startButton != null)
            {
                startButton.interactable = false;

            }

            return;

        }

        if (headingText != null)
        {
            headingText.text = category == ResearchCategory.EquipmentAndConsumables
                ? "EQUIPMENT / CONSUMABLES" : category.ToString().ToUpperInvariant();

        }

        foreach (ResearchNode node in manager.Database.Nodes)
        {
            if (node == null || node.category != category)
            {

                continue;

            }

            if (selected == null)
            {
                selected = node;

            }

            Button row = Instantiate(rowPrefab, rowContent);
            row.gameObject.SetActive(true);
            TMP_Text label = row.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                string state = manager.IsComplete(node.id) ? "[DONE] " :
                    node.id == manager.ActiveResearchId ? "[ACTIVE] " :
                    manager.CanStart(node.id, out _) ? "[READY] " : "[LOCKED] ";
                label.text = state + node.displayName;

            }

            if (row.GetComponent<Image>() is Image background)
            {
                background.color = manager.IsComplete(node.id)
                    ? new Color(0.37f, 0.37f, 0.37f) : new Color(0.19f, 0.19f, 0.19f);

            }

            ResearchNode current = node;
            row.onClick.AddListener(() => Select(current));
            rows.Add(row);

        }

        RefreshDetails();

    }

    private void RefreshDetails()
    {
        if (selected == null)
        {
            if (detailText != null)
            {
                detailText.text = "Select a research project.";

            }
            if (startButton != null)
            {
                startButton.interactable = false;

            }

            return;

        }

        details.Clear();
        details.AppendLine(selected.displayName.ToUpperInvariant());
        details.AppendLine(selected.description);
        details.Append("\nTier: ").Append(selected.requiredTier);
        details.Append("    Battles: ").Append(selected.battlesRequired).AppendLine();
        if (selected.prerequisiteIds.Count > 0)
        {
            details.AppendLine("\nPREREQUISITES");
            foreach (string id in selected.prerequisiteIds)
            {
                ResearchNode prerequisite = manager.Database.Find(id);
                details.Append(manager.IsComplete(id) ? "[x] " : "[ ] ")
                    .AppendLine(prerequisite != null ? prerequisite.displayName : id);

            }

        }

        details.AppendLine("\nRESOURCES");
        if (selected.costs.Count == 0)
        {
            details.AppendLine("None");

        }

        foreach (ResearchCost cost in selected.costs)
        {
            ResourceStockpile stockpile = ResourceStockpile.Instance;
            ResourceData resource = stockpile != null ? stockpile.GetResourceData(cost.resourceId) : null;
            details.Append(resource != null ? resource.ResourceName : cost.resourceId)
                .Append(": ").Append(stockpile != null ? stockpile.GetQuantity(cost.resourceId) : 0)
                .Append(" / ").Append(cost.amount).AppendLine();

        }

        details.AppendLine("\nUNLOCKS");
        foreach (ResearchBenefit benefit in selected.benefits)
        {
            if (benefit == null)
            {

                continue;

            }

            details.Append("• ").Append(benefit.type);
            if (benefit.type == ResearchBenefitType.Recipe)
            {
                if (benefit.equipmentTemplate.TryResolve(out EquipmentTemplate template))
                {
                    details.Append(": ").Append(template.DisplayName);

                }
                else
                {
                    details.Append(": ").Append(benefit.recipeId);

                }

            }
            else if (benefit.type == ResearchBenefitType.UnitSlot)
            {
                details.Append(": ").Append(benefit.slot);

            }
            else if (benefit.amount > 1)
            {
                details.Append(" +").Append(benefit.amount);

            }

            details.AppendLine();

        }

        if (detailText != null)
        {
            detailText.text = details.ToString();

        }
        bool available = manager.CanStart(selected.id, out string reason);
        if (startButton != null)
        {
            startButton.interactable = available;

        }
        if (statusText != null)
        {
            ResearchNode active = manager.Database.Find(manager.ActiveResearchId);
            statusText.text = active != null
                ? "ACTIVE: " + active.displayName + " — " + manager.RemainingBattles + " victories remaining"
                : available ? "Ready to research" : reason;

        }

    }

    private void DebugUnlockAllResearch()
    {
        Observe();
        if (manager == null)
        {
            Debug.LogWarning("Research debug unlock failed: ResearchManager was not found.", this);

            return;

        }

        int unlocked = manager.DebugUnlockAllResearch();
        Debug.Log($"Research debug unlock: {unlocked} new projects completed.", this);

    }

    private void StartSelected()
    {
        if (manager == null || selected == null)
        {

            return;

        }

        manager.TryStart(selected.id, out string message);
        if (statusText != null)
        {
            statusText.text = message;

        }
        Refresh();

    }

    private void RefreshNotification()
    {
        if (notificationPanel == null)
        {

            return;

        }

        bool visible = manager != null && manager.HasPendingNotification;
        notificationPanel.SetActive(visible);
        if (visible && notificationText != null)
        {
            notificationText.text = "RESEARCH COMPLETE\n" + manager.CurrentNotificationTitle();

        }

    }

    private void CloseNotification()
    {
        manager?.DismissNotification();

    }

    private void OpenFromNotification()
    {
        manager?.DismissNotification();
        Open();

    }

}

