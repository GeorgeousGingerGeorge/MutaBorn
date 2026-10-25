using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UIButton = UnityEngine.UI.Button;
using UIImage = UnityEngine.UI.Image;


public class ActionMenuUI : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private SelectionManager selectionManager;

    [Tooltip("Optional: only assign a CONTENT container, not the menu background/root.")]
    [SerializeField] private GameObject unitContent;

    [Tooltip("Default OFF keeps the bottom menu visible when nothing is selected.")]
    [SerializeField] private bool hideUnitContentWhenNoSelection = false;

    [Header("Selected Unit / AP")]
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text apText;
    [SerializeField] private UIImage apFillImage;

    [Header("Main Buttons")]
    [SerializeField] private UIButton basicAttacksButton;
    [SerializeField] private UIButton defenseButton;
    [SerializeField] private UIButton interactButton;
    [SerializeField] private UIButton abilitiesButton;
    [SerializeField] private UIButton mostUsedButton;
    [SerializeField] private TMP_Text mostUsedText;
    [SerializeField] private UIButton lastUsedButton;
    [SerializeField] private TMP_Text lastUsedText;
    [SerializeField] private UIButton unitInfoButton;
    [SerializeField] private UIButton inspectUnitButton;
    [SerializeField] private ActionDefinition inspectUnitAction;

    [Header("Optional Expanded Menu (Not Collapsible Groups Yet)")]
    [SerializeField] private GameObject expandedPanel;
    [SerializeField] private TMP_Text expandedTitle;
    [SerializeField] private Transform expandedContent;
    [SerializeField] private ActionButtonUI actionButtonPrefab;
    [SerializeField] private UIButton closeExpandedButton;
    [SerializeField] private TMP_Text emptyMessageText;

    [Header("Combat Execution")]
    [SerializeField] private CombatActionExecutor combatExecutor;

    [Header("Unit Inspection")]
    [SerializeField] private UnitInspectionUI unitInspectionUI;

    [Header("Contextual Interactions")]
    [Tooltip("How often to refresh actions from nearby targets (seconds). Uses grid-range checks from ObjectiveInteractable.")]
    [SerializeField, Min(0.05f)] private float interactionScanInterval = 0.2f;

    private UnitController displayedUnit;
    private UnitActionLoadout displayedLoadout;
    private readonly List<ObjectiveInteractable> nearbyInteractions = new();
    private ActionMenuCategory openCategory;
    private bool categoryIsOpen;
    private int lastLoadoutVersion = -1;
    private float nextInteractionScanTime;
    private float lastExpandedAP = float.NaN;

    private void Awake()
    {
        if (selectionManager == null)
        {
            selectionManager = FindAnyObjectByType<SelectionManager>();

        }

        // Connect in code. Leave these buttons' Inspector On Click lists EMPTY.
        if (basicAttacksButton != null)
        {
            basicAttacksButton.onClick.AddListener(() => OpenCategory(ActionMenuCategory.BasicAttacks));

        }
        if (defenseButton != null)
        {
            defenseButton.onClick.AddListener(() => OpenCategory(ActionMenuCategory.Defense));

        }
        if (abilitiesButton != null)
        {
            abilitiesButton.onClick.AddListener(() => OpenCategory(ActionMenuCategory.Abilities));

        }
        if (interactButton != null)
        {
            interactButton.onClick.AddListener(() => OpenCategory(ActionMenuCategory.Interact));

        }
        if (unitInfoButton != null)
        {
            unitInfoButton.onClick.AddListener(RequestUnitInfo);

        }
        if (inspectUnitButton != null)
        {
            inspectUnitButton.onClick.AddListener(() => RequestAction(inspectUnitAction));

        }
        if (closeExpandedButton != null)
        {
            closeExpandedButton.onClick.AddListener(CloseCategory);

        }
        if (mostUsedButton != null)
        {
            mostUsedButton.onClick.AddListener(() => RequestAction(displayedLoadout != null ? displayedLoadout.MostUsedAbility : null));

        }
        if (lastUsedButton != null)
        {
            lastUsedButton.onClick.AddListener(() => RequestAction(displayedLoadout != null ? displayedLoadout.LastUsedAbility : null));

        }

        CloseCategory();
        ShowNoSelection();

    }

    private void Update()
    {
        TurnManager turns = TurnManager.Instance;

        UnitController selected = selectionManager != null ? selectionManager.selectedUnit : null;
        if (!CanCommand(selected) || selected.data == null)
        {
            selected = null;

        }

        if (selected != displayedUnit)
        {
            displayedUnit = selected;
            displayedLoadout = selected != null ? selected.GetComponent<UnitActionLoadout>() : null;
            lastLoadoutVersion = -1;
            nearbyInteractions.Clear();
            nextInteractionScanTime = 0f;
            CloseCategory();

        }

        bool hasUnit = displayedUnit != null;
        if (unitContent != null && hideUnitContentWhenNoSelection &&
            unitContent.activeSelf != hasUnit)
        {
            unitContent.SetActive(hasUnit);

        }

        if (!hasUnit)
        {
            ShowNoSelection();

            return;

        }

        // UnitData, NOT the obsolete UnitController.actionPoints/getStats.
        UnitData data = displayedUnit.data;
        float currentAP = Mathf.Max(0, data.currentActionPoints);
        float maxAP = Mathf.Max(0, data.maxActionPoints);

        if (unitNameText != null)
        {
            unitNameText.text = $"{data.raceName} / {data.characterName}";

        }
        if (apText != null)
        {
            apText.text = $"{currentAP:0.#} / {maxAP:0.#} AP";

        }
        if (apFillImage != null)
        {
            apFillImage.fillAmount = maxAP > 0f ? Mathf.Clamp01(currentAP / maxAP) : 0f;

        }

        SetInteractable(basicAttacksButton, true);
        SetInteractable(defenseButton, true);
        SetInteractable(abilitiesButton, true);
        SetInteractable(unitInfoButton, true);
        SetInteractable(inspectUnitButton,
            inspectUnitAction != null &&
            inspectUnitAction.CombatBehavior == CombatActionBehavior.InspectUnit &&
            currentAP >= Mathf.CeilToInt(inspectUnitAction.ApCost));

        // Refresh nearby interactions before updating the Interact button availability.
        if (Time.unscaledTime >= nextInteractionScanTime)
        {
            RefreshNearbyInteractions();
            nextInteractionScanTime = Time.unscaledTime + interactionScanInterval;

        }

        SetInteractable(interactButton, nearbyInteractions.Count > 0);

        int version = displayedLoadout != null ? displayedLoadout.Version : -1;
        if (version != lastLoadoutVersion)
        {
            lastLoadoutVersion = version;
            if (categoryIsOpen)
            {
                RebuildCategory();

            }

        }

        UpdateQuickButtons(currentAP);
        if (categoryIsOpen && !Mathf.Approximately(lastExpandedAP, currentAP))
        {
            RefreshExpandedButtonAffordability(currentAP);
            lastExpandedAP = currentAP;

        }

    }

    private bool CanCommand(UnitController unit)
    {
        if (unit == null || !unit.isActiveAndEnabled)
        {
            return false;

        }

        TurnManager turns = TurnManager.Instance;
        if (turns == null || !turns.IsPlayerTurn)
        {
            return false;

        }

        UnitFaction unitFaction = unit.GetComponent<UnitFaction>();

        return unitFaction != null && unitFaction.Faction != null &&
            turns.IsFactionTurn(unitFaction.Faction);

    }

    private void ShowNoSelection()
    {
        if (unitNameText != null)
        {
            unitNameText.text = "Select a Unit";

        }
        if (apText != null)
        {
            apText.text = "0 / 0 AP";

        }
        if (apFillImage != null)
        {
            apFillImage.fillAmount = 0f;

        }

        SetInteractable(unitInfoButton, false);
        SetInteractable(inspectUnitButton, false);
        SetInteractable(interactButton, false);
        SetInteractable(basicAttacksButton, false);
        SetInteractable(defenseButton, false);
        SetInteractable(abilitiesButton, false);
        SetInteractable(mostUsedButton, false);
        SetInteractable(lastUsedButton, false);
        if (mostUsedText != null)
        {
            mostUsedText.text = "MOST USED\n—";

        }
        if (lastUsedText != null)
        {
            lastUsedText.text = "LAST USED\n—";

        }

    }

    private static void SetInteractable(UIButton button, bool value)
    {
        if (button != null && button.interactable != value)
        {
            button.interactable = value;

        }

    }

    private void UpdateQuickButtons(float currentAP)
    {
        ActionDefinition most = displayedLoadout != null ? displayedLoadout.MostUsedAbility : null;
        ActionDefinition last = displayedLoadout != null ? displayedLoadout.LastUsedAbility : null;

        if (mostUsedText != null)
        {
            mostUsedText.text = most != null ? $"MOST USED\n{most.DisplayName}" : "MOST USED\n—";

        }
        if (lastUsedText != null)
        {
            lastUsedText.text = last != null ? $"LAST USED\n{last.DisplayName}" : "LAST USED\n—";

        }

        SetInteractable(mostUsedButton, most != null && currentAP >= most.ApCost);
        SetInteractable(lastUsedButton, last != null && currentAP >= last.ApCost);

    }

    private void OpenCategory(ActionMenuCategory category)
    {
        if (displayedUnit == null || displayedUnit.data == null || !CanCommand(displayedUnit))
        {
            return;

        }

        // Clicking the open category again closes it.
        if (categoryIsOpen && openCategory == category)
        {
            CloseCategory();

            return;

        }

        if (expandedPanel == null || expandedContent == null || actionButtonPrefab == null)
        {
            Debug.LogWarning("ActionMenuUI: Set up ExpandedPanel, ExpandedContent and ActionButtonPrefab to open categories.", this);

            return;

        }

        openCategory = category;
        categoryIsOpen = true;
        lastExpandedAP = float.NaN;
        expandedPanel.SetActive(true);
        RebuildCategory();

    }

    private void CloseCategory()
    {
        categoryIsOpen = false;
        lastExpandedAP = float.NaN;
        if (expandedPanel != null)
        {
            expandedPanel.SetActive(false);

        }

    }

    private void RebuildCategory()
    {
        if (!categoryIsOpen || expandedContent == null || actionButtonPrefab == null)
        {
            return;

        }

        for (int i = expandedContent.childCount - 1; i >= 0; --i)
        {
            // The scroll-content container should hold generated action buttons only.
            Destroy(expandedContent.GetChild(i).gameObject);

        }

        if (expandedTitle != null)
        {
            expandedTitle.text = openCategory == ActionMenuCategory.BasicAttacks ? "ATTACKS" :
                openCategory == ActionMenuCategory.Defense ? "DEFENCE" :
                openCategory == ActionMenuCategory.Interact ? "INTERACT" : "ABILITIES";

        }

        int count = 0;
        if (openCategory == ActionMenuCategory.Interact)
        {
            foreach (ObjectiveInteractable target in nearbyInteractions)
            {
                if (target == null || !target.CanInteract(displayedUnit) || target.InteractionAction == null)
                {
                    continue;

                }

                ActionDefinition action = target.InteractionAction;
                ActionButtonUI item = Instantiate(actionButtonPrefab, expandedContent);
                ObjectiveInteractable selectedTarget = target;
                item.Bind(action, displayedUnit.data.currentActionPoints,
                    _ => RequestInteraction(selectedTarget));
                item.SetContextLabel(target.InteractionTargetName);
                count++;

            }

        }
        else if (displayedLoadout != null)
        {
            foreach (ActionDefinition action in displayedLoadout.GetActions(openCategory))
            {
                ActionButtonUI item = Instantiate(actionButtonPrefab, expandedContent);
                item.Bind(action, displayedUnit.data.currentActionPoints, RequestAction);
                count++;

            }

        }

        if (emptyMessageText != null)
        {
            emptyMessageText.gameObject.SetActive(count == 0);
            if (count == 0)
            {
                emptyMessageText.text = openCategory == ActionMenuCategory.Interact
                    ? "No interactable targets within range."
                    : "No actions assigned to this category yet.";

            }

        }

    }

    private void RefreshExpandedButtonAffordability(float currentAP)
    {
        if (expandedContent == null)
        {
            return;

        }
        foreach (ActionButtonUI item in expandedContent.GetComponentsInChildren<ActionButtonUI>())
        {
            item.RefreshAvailability(currentAP);

        }

    }

    private void RequestAction(ActionDefinition action)
    {
        if (action == null || displayedUnit == null || displayedUnit.data == null ||
            !CanCommand(displayedUnit))
        {
            return;

        }

        if (displayedUnit.data.currentActionPoints < action.ApCost)
        {
            return;

        }

        if (action.IsPassive)
        {
            return;

        }

        if (combatExecutor == null)
        {
            combatExecutor = FindAnyObjectByType<CombatActionExecutor>();

        }

        if (combatExecutor == null)
        {
            Debug.LogError("No CombatActionExecutor in scene. Add it to the Action Menu object.", this);

            return;

        }

        if (combatExecutor.TryBeginAction(displayedUnit, action))
        {
            CloseCategory();

        }

    }

    private void RequestUnitInfo()
    {
        if (displayedUnit == null || displayedUnit.data == null)
        {
            return;

        }

        if (unitInspectionUI == null)
        {
            unitInspectionUI = FindAnyObjectByType<UnitInspectionUI>();

        }

        if (unitInspectionUI == null)
        {
            Debug.LogWarning("ActionMenuUI: Assign the UnitInspectionUI component in the Inspector.", this);

            return;

        }

        CloseCategory();
        unitInspectionUI.Open(displayedUnit);

    }

    private void RefreshNearbyInteractions()
    {
        List<ObjectiveInteractable> available = new();

        if (displayedUnit != null)
        {
            foreach (ObjectiveInteractable target in FindObjectsByType<ObjectiveInteractable>())
            {
                if (target != null && target.InteractionAction != null &&
                    target.InteractionAction.Category == ActionMenuCategory.Interact &&
                    !target.InteractionAction.IsPassive && target.CanInteract(displayedUnit))
                {
                    available.Add(target);

                }

            }

            available.Sort((left, right) =>
                (left.transform.position - displayedUnit.transform.position).sqrMagnitude.CompareTo(
                (right.transform.position - displayedUnit.transform.position).sqrMagnitude));

        }

        bool changed = available.Count != nearbyInteractions.Count;

        if (!changed)
        {
            for (int i = 0; i < available.Count; i++)
            {
                if (available[i] != nearbyInteractions[i])
                {
                    changed = true;

                    break;

                }

            }

        }

        if (!changed)
        {
            return;

        }

        nearbyInteractions.Clear();
        nearbyInteractions.AddRange(available);

        if (categoryIsOpen && openCategory == ActionMenuCategory.Interact)
        {
            RebuildCategory();

        }

    }

    private void RequestInteraction(ObjectiveInteractable target)
    {
        if (target == null || displayedUnit == null || displayedUnit.data == null ||
            !CanCommand(displayedUnit) || !target.CanInteract(displayedUnit))
        {
            return;

        }

        ActionDefinition action = target.InteractionAction;
        if (action == null || action.Category != ActionMenuCategory.Interact || action.IsPassive)
        {
            return;

        }

        int apCost = Mathf.CeilToInt(action.ApCost);
        if (displayedUnit.data.currentActionPoints < apCost)
        {
            Debug.Log($"Not enough AP for {action.DisplayName}.", this);

            return;

        }

        if (!target.TryInteract(displayedUnit))
        {
            return;

        }

        displayedUnit.data.ModifyActionPoints(-apCost);
        RefreshNearbyInteractions();

        if (categoryIsOpen && openCategory == ActionMenuCategory.Interact)
        {
            RebuildCategory();

        }

    }

}
