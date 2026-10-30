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

    [Header("Selected Unit / AP / MP")]
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text apText;
    [SerializeField] private UIImage apFillImage;
    [SerializeField] private TMP_Text mpText;
    [SerializeField] private UIImage mpFillImage;

    [Header("Main Buttons")]
    [SerializeField] private UIButton attacksButton;
    [SerializeField] private UIButton defenseButton;
    [SerializeField] private UIButton interactButton;
    [SerializeField] private UIButton abilitiesButton;
    [SerializeField] private UIButton itemsButton;

    //[SerializeField] private UIButton mostUsedButton;
    //[SerializeField] private TMP_Text mostUsedText;
    //[SerializeField] private UIButton lastUsedButton;
    //[SerializeField] private TMP_Text lastUsedText;
    //[SerializeField] private UIButton unitInfoButton;
    //[SerializeField] private UIButton inspectUnitButton;
    //[SerializeField] private ActionDefinition inspectUnitAction;

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

    [Header("Action Description (Optional)")]
    [Tooltip("Existing Action Info Boarder; located automatically if left unassigned.")]
    [SerializeField] private GameObject actionInfoPanel;
    [SerializeField] private TMP_Text actionDescriptionText;

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
    private ActionDefinition previewAction;
    private ActionDefinition targetedAction;

    private void Awake()
    {
        ConfigureActionInfoPanel();

        if (selectionManager == null)
        {
            selectionManager = FindAnyObjectByType<SelectionManager>();

        }

        // Connect in code. Leave these buttons' Inspector On Click lists EMPTY.
        if (attacksButton != null)
        {
            attacksButton.onClick.AddListener(() => OpenCategory(ActionMenuCategory.Attacks));

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

        if (itemsButton != null)
        {
            itemsButton.onClick.AddListener(() => OpenCategory(ActionMenuCategory.Items));

        }

        //if (unitInfoButton != null)
        //{
        //    unitInfoButton.onClick.AddListener(RequestUnitInfo);
        //
        //}

        //if (inspectUnitButton != null)
        //{
        //    inspectUnitButton.onClick.AddListener(() => RequestAction(inspectUnitAction));
        //
        //}

        if (closeExpandedButton != null)
        {
            closeExpandedButton.onClick.AddListener(CloseCategory);

        }

        //if (mostUsedButton != null)
        //{
        //    mostUsedButton.onClick.AddListener(() => RequestAction(displayedLoadout != null ? displayedLoadout.MostUsedAbility : null));
        //
        //}

        //if (lastUsedButton != null)
        //{
        //    lastUsedButton.onClick.AddListener(() => RequestAction(displayedLoadout != null ? displayedLoadout.LastUsedAbility : null));
        //
        //}

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
            targetedAction = null;
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
            RefreshActionInfoPanel();

            return;

        }

        // UnitData, NOT the obsolete UnitController.actionPoints/getStats.
        UnitData data = displayedUnit.data;
        float currentAP = Mathf.Max(0, data.currentActionPoints);
        float maxAP = Mathf.Max(0, data.maxActionPoints);
        float currentMP = Mathf.Max(0, data.currentMana);
        float maxMP = Mathf.Max(0, data.maxMana);

        if (unitNameText != null)
        {
            unitNameText.text = $"{data.raceName} / {data.characterName}";

        }

        if (apText != null)
        {
            apText.text = $"{currentAP:0.#} / {maxAP:0.#}";

        }

        if (apFillImage != null)
        {
            apFillImage.fillAmount = maxAP > 0f ? Mathf.Clamp01(currentAP / maxAP) : 0f;

        }

        if (mpText != null)
        {
            mpText.text = $"{currentMP:0.#} / {maxMP:0.#}";

        }

        if (mpFillImage != null)
        {
            mpFillImage.fillAmount = maxMP > 0f ? Mathf.Clamp01(currentMP / maxMP) : 0f;

        }

        SetInteractable(attacksButton, true);
        SetInteractable(defenseButton, true);
        SetInteractable(abilitiesButton, true);
        SetInteractable(itemsButton, true);

        //SetInteractable(unitInfoButton, true);
        //SetInteractable(inspectUnitButton,
        //    inspectUnitAction != null &&
        //    inspectUnitAction.CombatBehavior == CombatActionBehavior.InspectUnit &&
        //    currentAP >= Mathf.CeilToInt(inspectUnitAction.ApCost));

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

        //UpdateQuickButtons(currentAP);
        RefreshActionInfoPanel();
        if (categoryIsOpen && !Mathf.Approximately(lastExpandedAP, currentAP))
        {
            RefreshExpandedButtonAffordability(currentAP);
            lastExpandedAP = currentAP;

        }

    }

    private bool CanCommand(UnitController unit)
    {
        return selectionManager != null && selectionManager.CanCommandUnit(unit);

    }

    private void ShowNoSelection()
    {
        if (unitNameText != null)
        {
            unitNameText.text = "Select a Unit";

        }
        if (apText != null)
        {
            apText.text = "0 / 0";

        }
        if (apFillImage != null)
        {
            apFillImage.fillAmount = 0f;

        }

        if (mpText != null)
        {
            mpText.text = "0 / 0";

        }

        if (mpFillImage != null)
        {
            mpFillImage.fillAmount = 0f;

        }

        SetInteractable(interactButton, false);
        SetInteractable(attacksButton, false);
        SetInteractable(defenseButton, false);
        SetInteractable(abilitiesButton, false);
        SetInteractable(itemsButton, false);

    }

    private static void SetInteractable(UIButton button, bool value)
    {
        if (button != null && button.interactable != value)
        {
            button.interactable = value;

        }

    }

    //private void UpdateQuickButtons(float currentAP)
    //{
    //    ActionDefinition most = displayedLoadout != null ? displayedLoadout.MostUsedAbility : null;
    //    ActionDefinition last = displayedLoadout != null ? displayedLoadout.LastUsedAbility : null;
    //
    //    if (mostUsedText != null)
    //    {
    //        mostUsedText.text = most != null ? $"MOST USED\n{most.DisplayName}" : "MOST USED\n—";
    //
    //    }
    //    if (lastUsedText != null)
    //    {
    //        lastUsedText.text = last != null ? $"LAST USED\n{last.DisplayName}" : "LAST USED\n—";
    //
    //    }
    //
    //    SetInteractable(mostUsedButton, most != null && currentAP >= most.ApCost);
    //    SetInteractable(lastUsedButton, last != null && currentAP >= last.ApCost);
    //
    //}

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
        previewAction = null;
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
            expandedTitle.text = openCategory == ActionMenuCategory.Attacks ? "ATTACKS" :
                openCategory == ActionMenuCategory.Defense ? "DEFENCE" :
                openCategory == ActionMenuCategory.Interact ? "INTERACT" : 
                openCategory == ActionMenuCategory.Items ? "ITEMS" : 
                "ABILITIES";

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
                    _ => RequestInteraction(selectedTarget), PreviewAction, EndActionPreview);
                item.SetContextLabel(target.InteractionTargetName);
                count++;

            }

        }
        else if (displayedLoadout != null)
        {
            foreach (ActionDefinition action in displayedLoadout.GetActions(openCategory))
            {
                ActionButtonUI item = Instantiate(actionButtonPrefab, expandedContent);
                item.Bind(action, displayedUnit.data.currentActionPoints, RequestAction,
                    PreviewAction, EndActionPreview);
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

        if (action.IsPassive)
        {
            CloseCategory();
            RefreshActionInfoPanel();

            return;

        }

        if (displayedUnit.data.currentActionPoints < action.ApCost)
        {
            return;

        }

        // A valid action click dismisses the expanded menu, even if execution
        // cannot begin (for example, an action without an implemented behavior).
        CloseCategory();

        if (combatExecutor == null)
        {
            combatExecutor = FindAnyObjectByType<CombatActionExecutor>();

        }

        if (combatExecutor == null)
        {
            Debug.LogError("No CombatActionExecutor in scene. Add it to the Action Menu object.", this);
            RefreshActionInfoPanel();

            return;

        }

        if (combatExecutor.TryBeginAction(displayedUnit, action))
        {
            targetedAction = TargetingModeController.IsTargeting ? action : null;

        }

        RefreshActionInfoPanel();

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

    private void ConfigureActionInfoPanel()
    {
        // Reuse the existing Action Info Boarder from Tristan Sand Box.
        if (actionInfoPanel == null)
        {
            Transform found = transform.Find("Action Info Boarder");
            if (found != null)
            {
                actionInfoPanel = found.gameObject;

            }

        }

        if (actionDescriptionText == null && actionInfoPanel != null)
        {
            actionDescriptionText = actionInfoPanel.GetComponentInChildren<TMP_Text>(true);

        }

        if (actionDescriptionText != null)
        {
            actionDescriptionText.enableAutoSizing = true;
            actionDescriptionText.fontSizeMin = 12f;
            actionDescriptionText.fontSizeMax = 17f;
            actionDescriptionText.alignment = TextAlignmentOptions.Center;

        }

        if (actionInfoPanel != null)
        {
            // The tooltip must never steal pointer hover or clicks from action buttons.
            foreach (UnityEngine.UI.Graphic graphic in actionInfoPanel.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                graphic.raycastTarget = false;

            }

            actionInfoPanel.SetActive(false);

        }

    }

    private void PreviewAction(ActionDefinition action)
    {
        previewAction = action;
        RefreshActionInfoPanel();

    }

    private void EndActionPreview(ActionDefinition action)
    {
        if (previewAction == action)
        {
            previewAction = null;
            RefreshActionInfoPanel();

        }

    }

    private void RefreshActionInfoPanel()
    {
        if (actionInfoPanel == null)
        {
            return;

        }

        if (targetedAction != null && !TargetingModeController.IsTargeting)
        {
            targetedAction = null;

        }

        ActionDefinition shown = displayedUnit != null ? targetedAction ?? previewAction : null;
        bool shouldShow = shown != null;
        if (actionInfoPanel.activeSelf != shouldShow)
        {
            actionInfoPanel.SetActive(shouldShow);

        }

        if (!shouldShow || actionDescriptionText == null)
        {
            return;

        }

        string cost = shown.IsPassive ? "PASSIVE" : $"{shown.ApCost:0.#} AP";
        string range = shown.CombatBehavior == CombatActionBehavior.DamageUnit
            ? $"  |  Range: {shown.CombatRange:0.#}"
            : "";
        string description = string.IsNullOrWhiteSpace(shown.Description)
            ? "No description available."
            : shown.Description.Trim();
        string text = $"<b>{shown.DisplayName}</b>  |  {cost}{range}\n{description}";

        if (actionDescriptionText.text != text)
        {
            actionDescriptionText.text = text;

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
        CloseCategory();
        RefreshNearbyInteractions();
        RefreshActionInfoPanel();

    }

}
