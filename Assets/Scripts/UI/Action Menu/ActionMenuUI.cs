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

    [Header("Optional Expanded Menu (Not Collapsible Groups Yet)")]
    [SerializeField] private GameObject expandedPanel;
    [SerializeField] private TMP_Text expandedTitle;
    [SerializeField] private Transform expandedContent;
    [SerializeField] private ActionButtonUI actionButtonPrefab;
    [SerializeField] private UIButton closeExpandedButton;
    [SerializeField] private TMP_Text emptyMessageText;

    [Header("Combat Execution")]
    [SerializeField] private CombatActionExecutor combatExecutor;

    [Header("Prototype Interaction")]
    [SerializeField, Min(0.1f)] private float interactionRange = 2.5f;

    private UnitController displayedUnit;
    private UnitActionLoadout displayedLoadout;
    private SimpleInteractable nearbyInteractable;
    private ActionMenuCategory openCategory;
    private bool categoryIsOpen;
    private int lastLoadoutVersion = -1;
    private float nextInteractionScanTime;
    private float lastExpandedAP = float.NaN;

    private void Awake()
    {
        if (selectionManager == null)
            selectionManager = FindAnyObjectByType<SelectionManager>();

        // Connect in code. Leave these buttons' Inspector On Click lists EMPTY.
        if (basicAttacksButton != null)
            basicAttacksButton.onClick.AddListener(() => OpenCategory(ActionMenuCategory.BasicAttacks));
        if (defenseButton != null)
            defenseButton.onClick.AddListener(() => OpenCategory(ActionMenuCategory.Defense));
        if (abilitiesButton != null)
            abilitiesButton.onClick.AddListener(() => OpenCategory(ActionMenuCategory.Abilities));
        if (interactButton != null)
            interactButton.onClick.AddListener(Interact);
        if (unitInfoButton != null)
            unitInfoButton.onClick.AddListener(() => Debug.Log("Unit Stats screen is not implemented yet.", this));
        if (closeExpandedButton != null)
            closeExpandedButton.onClick.AddListener(CloseCategory);
        if (mostUsedButton != null)
            mostUsedButton.onClick.AddListener(() => RequestAction(displayedLoadout != null ? displayedLoadout.MostUsedAbility : null));
        if (lastUsedButton != null)
            lastUsedButton.onClick.AddListener(() => RequestAction(displayedLoadout != null ? displayedLoadout.LastUsedAbility : null));

        CloseCategory();
        ShowNoSelection();

    }

    private void Update()
    {
        TurnManager turns = TurnManager.Instance;

        UnitController selected = selectionManager != null ? selectionManager.selectedUnit : null;
        if (!CanCommand(selected) || selected.data == null)
            selected = null;

        if (selected != displayedUnit)
        {
            displayedUnit = selected;
            displayedLoadout = selected != null ? selected.GetComponent<UnitActionLoadout>() : null;
            lastLoadoutVersion = -1;
            nearbyInteractable = null;
            nextInteractionScanTime = 0f;
            CloseCategory();
        }

        bool hasUnit = displayedUnit != null;
        if (unitContent != null && hideUnitContentWhenNoSelection &&
            unitContent.activeSelf != hasUnit)
            unitContent.SetActive(hasUnit);

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
            unitNameText.text = $"{data.raceName} / {data.characterName}";
        if (apText != null)
            apText.text = $"{currentAP:0.#} / {maxAP:0.#} AP";
        if (apFillImage != null)
            apFillImage.fillAmount = maxAP > 0f ? Mathf.Clamp01(currentAP / maxAP) : 0f;

        SetInteractable(basicAttacksButton, true);
        SetInteractable(defenseButton, true);
        SetInteractable(abilitiesButton, true);
        SetInteractable(unitInfoButton, true);

        // Prototype interaction checks for a nearby SimpleInteractable component.
        if (Time.unscaledTime >= nextInteractionScanTime)
        {
            nearbyInteractable = FindNearbyInteractable(displayedUnit);
            nextInteractionScanTime = Time.unscaledTime + 0.2f;
        }
        SetInteractable(interactButton, nearbyInteractable != null);

        int version = displayedLoadout != null ? displayedLoadout.Version : -1;
        if (version != lastLoadoutVersion)
        {
            lastLoadoutVersion = version;
            if (categoryIsOpen)
                RebuildCategory();
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
            return false;

        TurnManager turns = TurnManager.Instance;
        if (turns == null || !turns.IsPlayerTurn)
            return false;

        UnitFaction unitFaction = unit.GetComponent<UnitFaction>();
        return unitFaction != null && unitFaction.Faction != null &&
            turns.IsFactionTurn(unitFaction.Faction);
    }

    private void ShowNoSelection()
    {
        if (unitNameText != null) unitNameText.text = "Select a Unit";
        if (apText != null) apText.text = "0 / 0 AP";
        if (apFillImage != null) apFillImage.fillAmount = 0f;

        SetInteractable(unitInfoButton, false);
        SetInteractable(interactButton, false);
        SetInteractable(basicAttacksButton, false);
        SetInteractable(defenseButton, false);
        SetInteractable(abilitiesButton, false);
        SetInteractable(mostUsedButton, false);
        SetInteractable(lastUsedButton, false);
        if (mostUsedText != null) mostUsedText.text = "MOST USED\n—";
        if (lastUsedText != null) lastUsedText.text = "LAST USED\n—";
    }

    private static void SetInteractable(UIButton button, bool value)
    {
        if (button != null && button.interactable != value)
            button.interactable = value;
    }

    private void UpdateQuickButtons(float currentAP)
    {
        ActionDefinition most = displayedLoadout != null ? displayedLoadout.MostUsedAbility : null;
        ActionDefinition last = displayedLoadout != null ? displayedLoadout.LastUsedAbility : null;

        if (mostUsedText != null)
            mostUsedText.text = most != null ? $"MOST USED\n{most.DisplayName}" : "MOST USED\n—";
        if (lastUsedText != null)
            lastUsedText.text = last != null ? $"LAST USED\n{last.DisplayName}" : "LAST USED\n—";

        SetInteractable(mostUsedButton, most != null && currentAP >= most.ApCost);
        SetInteractable(lastUsedButton, last != null && currentAP >= last.ApCost);
    }

    private void OpenCategory(ActionMenuCategory category)
    {
        if (displayedUnit == null || displayedUnit.data == null || !CanCommand(displayedUnit))
            return;

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
            expandedPanel.SetActive(false);
    }

    private void RebuildCategory()
    {
        if (!categoryIsOpen || expandedContent == null || actionButtonPrefab == null)
            return;

        for (int i = expandedContent.childCount - 1; i >= 0; --i)
        {
            // The scroll-content container should hold generated action buttons only.
            Destroy(expandedContent.GetChild(i).gameObject);
        }

        if (expandedTitle != null)
            expandedTitle.text = openCategory == ActionMenuCategory.BasicAttacks ? "ATTACKS" :
                openCategory == ActionMenuCategory.Defense ? "DEFENCE" : "ABILITIES";

        int count = 0;
        if (displayedLoadout != null)
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
                emptyMessageText.text = "No actions assigned to this category yet.";
        }
    }

    private void RefreshExpandedButtonAffordability(float currentAP)
    {
        if (expandedContent == null)
            return;
        foreach (ActionButtonUI item in expandedContent.GetComponentsInChildren<ActionButtonUI>())
            item.RefreshAvailability(currentAP);
    }

    private void RequestAction(ActionDefinition action)
    {
        if (action == null || displayedUnit == null || displayedUnit.data == null ||
            !CanCommand(displayedUnit))
            return;

        if (displayedUnit.data.currentActionPoints < action.ApCost)
            return;

        if (action.IsPassive)
            return;

        if (combatExecutor == null)
            combatExecutor = FindAnyObjectByType<CombatActionExecutor>();

        if (combatExecutor == null)
        {
            Debug.LogError("No CombatActionExecutor in scene. Add it to the Action Menu object.", this);
            return;
        }

        if (combatExecutor.TryBeginAction(displayedUnit, action))
            CloseCategory();
    }

    private SimpleInteractable FindNearbyInteractable(UnitController unit)
    {
        if (unit == null)
            return null;

        Collider[] hits = Physics.OverlapSphere(unit.transform.position,
            interactionRange, ~0, QueryTriggerInteraction.Collide);
        SimpleInteractable nearest = null;
        float bestDistance = float.PositiveInfinity;
        foreach (Collider hit in hits)
        {
            SimpleInteractable target = hit.GetComponentInParent<SimpleInteractable>();
            if (target == null || !target.CanInteract(unit))
                continue;

            float distance = (hit.ClosestPoint(unit.transform.position) - unit.transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = target;
            }
        }
        return nearest;
    }

    private void Interact()
    {
        if (displayedUnit == null || !CanCommand(displayedUnit))
            return;
        SimpleInteractable target = FindNearbyInteractable(displayedUnit);
        if (target != null)
            target.Interact(displayedUnit);
    }

}
