using System.Collections.Generic;
using System.Text;
using MutaBorn.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

    [Header("Optional Expanded Menu (Not Collapsible Groups Yet)")]
    [SerializeField] private GameObject expandedPanel;
    [SerializeField] private TMP_Text expandedTitle;
    [SerializeField] private Transform expandedContent;
    [SerializeField] private ActionButtonUI actionButtonPrefab;
    [SerializeField] private UIButton closeExpandedButton;
    [SerializeField] private TMP_Text emptyMessageText;

    [Header("Combat Execution")]
    [SerializeField] private CombatActionExecutor combatExecutor;

    [Header("Action Description (Optional)")]
    [Tooltip("Existing Action Info Boarder; located automatically if left unassigned.")]
    [SerializeField] private GameObject actionInfoPanel;
    [SerializeField] private TMP_Text actionDescriptionText;

    [Header("Attack Preview (scene/prefab component)")]
    [SerializeField] private AttackPreviewUI attackPreviewUI;

    [Header("Contextual Interactions")]
    [Tooltip("How often to refresh actions from nearby targets (seconds). Uses grid-range checks from WorldInteractable.")]
    [SerializeField, Min(0.05f)] private float interactionScanInterval = 0.2f;

    private UnitController displayedUnit;
    private UnitActionLoadout displayedLoadout;
    private readonly List<WorldInteractable> nearbyInteractions = new();
    private ActionMenuCategory openCategory;
    private bool categoryIsOpen;
    private int lastLoadoutVersion = -1;
    private float nextInteractionScanTime;
    private float lastExpandedAP = float.NaN;
    private ActionDefinition previewAction;
    private ConsumableData previewConsumable;
    private ActionDefinition targetedAction;
    private bool wasConsumableTargeting;
    private ConsumableData targetedConsumable;
    private AccuracySystem previewAccuracy;
    private TargetingSystem previewTargeting;
    private float nextPreviewRefresh;

    private void Awake()
    {
        ConfigureActionInfoPanel();
        if (attackPreviewUI == null)
        {
            attackPreviewUI = GetComponentInChildren<AttackPreviewUI>(true);

        }

        if (attackPreviewUI != null)
        {
            attackPreviewUI.Hide();

        }

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
            targetedConsumable = null;
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
            RefreshAttackPreviewPanel();

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

        bool isConsumableTargeting = CombatActionExecutor.IsTargeting;
        if (categoryIsOpen && openCategory == ActionMenuCategory.Items &&
            wasConsumableTargeting && !isConsumableTargeting)
        {
            RebuildCategory();

        }

        wasConsumableTargeting = isConsumableTargeting;
        if (categoryIsOpen && openCategory == ActionMenuCategory.Items)
        {
            RefreshExpandedButtonAffordability(currentAP);

        }

        //UpdateQuickButtons(currentAP);
        RefreshActionInfoPanel();
        RefreshAttackPreviewPanel();
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
        previewConsumable = null;
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
            foreach (WorldInteractable target in nearbyInteractions)
            {
                if (target == null || !target.CanInteract(displayedUnit) || target.InteractionAction == null)
                {
                    continue;

                }

                ActionDefinition action = target.InteractionAction;
                ActionButtonUI item = Instantiate(actionButtonPrefab, expandedContent);
                WorldInteractable selectedTarget = target;
                item.Bind(action, displayedUnit.data.currentActionPoints,
                    _ => RequestInteraction(selectedTarget), PreviewAction, EndActionPreview);
                item.SetContextLabel(target.InteractionMenuLabel);
                count++;

            }

        }
        else if (displayedLoadout != null)
        {
            foreach (ActionDefinition action in displayedLoadout.GetActions(openCategory))
            {
                // Capture now belongs exclusively to carried consumables, not gear-granted actions.
                if (action.CombatBehavior == CombatActionBehavior.CaptureUnit)
                {
                    continue;

                }

                ActionButtonUI item = Instantiate(actionButtonPrefab, expandedContent);
                item.Bind(action, displayedUnit.data.currentActionPoints, RequestAction,
                    PreviewAction, EndActionPreview);
                count++;

            }

        }

        if (openCategory == ActionMenuCategory.Items && displayedUnit != null && displayedUnit.data != null)
        {
            for (int i = 0; i < displayedUnit.data.carriedConsumables.Count; i++)
            {
                ConsumableInstance consumableInstance = displayedUnit.data.carriedConsumables[i];
                ConsumableData consumable = consumableInstance.definition;
                if (consumable == null)
                {
                    continue;

                }

                int selectedIndex = i;
                ActionButtonUI item = Instantiate(actionButtonPrefab, expandedContent);
                item.BindConsumable(consumable, consumable.UseAction, consumableInstance.remainingUses,
                    displayedUnit.data.currentActionPoints,
                    () => RequestConsumable(selectedIndex), PreviewConsumable, EndConsumablePreview);
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
            CameraController.Instance?.FocusOnUnit(displayedUnit, true);

        }

        RefreshActionInfoPanel();

    }

    private void RefreshNearbyInteractions()
    {
        List<WorldInteractable> available = new();

        if (displayedUnit != null)
        {
            foreach (WorldInteractable target in FindObjectsByType<WorldInteractable>())
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

        if (!TargetingModeController.IsTargeting)
        {
            targetedAction = null;
            targetedConsumable = null;

        }

        ActionDefinition shown = displayedUnit != null ? targetedAction ?? previewAction : null;
        ConsumableData shownConsumable = displayedUnit != null && shown == null
            ? targetedConsumable ?? previewConsumable : null;
        bool shouldShow = shown != null || shownConsumable != null;
        if (actionInfoPanel.activeSelf != shouldShow)
        {
            actionInfoPanel.SetActive(shouldShow);

        }

        if (!shouldShow || actionDescriptionText == null)
        {
            return;

        }

        if (shownConsumable != null)
        {
            string consumableDescription = string.IsNullOrWhiteSpace(shownConsumable.description)
                ? "No description available."
                : shownConsumable.description.Trim();
            ActionDefinition useAction = shownConsumable.UseAction;
            if (useAction != null)
            {
                consumableDescription = string.IsNullOrWhiteSpace(useAction.Description)
                    ? consumableDescription
                    : useAction.Description.Trim();

            }

            string label = useAction != null ? useAction.DisplayName : shownConsumable.itemName;
            actionDescriptionText.text = $"<b>{label}</b>  |  Consumable\n" +
                consumableDescription;

            return;

        }

        string cost = shown.IsPassive ? "PASSIVE" : $"{shown.ApCost:0.#} AP";
        string range = (shown.CombatBehavior == CombatActionBehavior.DamageUnit ||
            shown.CombatBehavior == CombatActionBehavior.CaptureUnit)
            ? $"  |  Range: {shown.CombatRange:0.#}"
            : (shown.CombatBehavior == CombatActionBehavior.DamageArea ||
                shown.CombatBehavior == CombatActionBehavior.HealArea)
                ? $"  |  Throw: {shown.AreaThrowRangeInTiles} tiles | Blast: {shown.AreaRadiusInTiles} tiles"
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

    private void PreviewConsumable(ConsumableData consumable)
    {
        previewConsumable = consumable;
        RefreshActionInfoPanel();

    }

    private void EndConsumablePreview(ConsumableData consumable)
    {
        if (previewConsumable == consumable)
        {
            previewConsumable = null;
            RefreshActionInfoPanel();

        }

    }

    private void RefreshAttackPreviewPanel()
    {
        if (attackPreviewUI == null)
        {
            return;

        }

        bool targeting = TargetingModeController.IsTargeting;
        ActionDefinition action = displayedUnit == null ? null :
            (targeting ? targetedAction ?? targetedConsumable?.UseAction :
             previewAction ?? previewConsumable?.UseAction);
        bool active = action != null && action.CombatBehavior == CombatActionBehavior.DamageUnit;
        if (!active)
        {
            attackPreviewUI.Hide();

            return;

        }

        if (Time.unscaledTime < nextPreviewRefresh)
        {
            return;

        }

        nextPreviewRefresh = Time.unscaledTime + 0.1f;
        string message = "HOVER OVER UNIT OR DESTRUCTIBLE SURFACE";
        if (Mouse.current == null || Camera.main == null ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            ShowPreviewMessage(message);

            return;

        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        UnitController target = null;
        ObstacleController obstacle = null;
        foreach (RaycastHit raycastHit in hits)
        {
            if (raycastHit.collider == null)
            {
                continue;

            }

            target = raycastHit.collider.GetComponentInParent<UnitController>();
            if (target != null)
            {
                break;

            }

            obstacle = raycastHit.collider.GetComponentInParent<ObstacleController>();
            if (obstacle != null && obstacle.isDestructible)
            {
                break;

            }

        }

        if (target == null)
        {
            ShowPreviewMessage(obstacle != null && obstacle.isDestructible
                ? $"{obstacle.obstacleName}  |  {obstacle.currentHealth}/{obstacle.maxHealth} HP\n" +
                  "DESTRUCTIBLE — THIS ACTION TARGETS UNITS ONLY"
                : message);

            return;

        }

        if (target == displayedUnit || target.data == null ||
            target.data.currentHealth <= 0 || !target.isActiveAndEnabled)
        {
            ShowPreviewMessage("HOVER OVER A VALID TARGET");

            return;

        }

        if (Vector3.Distance(displayedUnit.transform.position,
            target.transform.position) > action.CombatRange)
        {
            ShowPreviewMessage("NOT IN RANGE");

            return;

        }

        if (previewTargeting == null)
        {
            previewTargeting = FindAnyObjectByType<TargetingSystem>();

        }

        if (previewTargeting == null ||
            !previewTargeting.CanTargetUnit(displayedUnit, target, action.CombatRange))
        {
            ShowPreviewMessage("TARGET BLOCKED / NOT DETECTED");

            return;

        }

        if (previewAccuracy == null)
        {
            previewAccuracy = FindAnyObjectByType<AccuracySystem>();

        }

        if (previewAccuracy == null)
        {
            ShowPreviewMessage("ACCURACY SYSTEM UNAVAILABLE");

            return;

        }

        int equipmentIndex = FindPreviewEquipmentIndex(displayedUnit, action);
        if (!DamageResolver.TryPreviewActionDamage(displayedUnit, target,
            action, equipmentIndex, out int damage, out int criticalDamage))
        {
            ShowPreviewMessage("DAMAGE PREVIEW UNAVAILABLE");

            return;

        }

        float hitChance = previewAccuracy.CalculateHitChance(displayedUnit, target, action);
        float dodge = DamageResolver.GetDodgeChance(target);
        float chanceToLand = hitChance * (1f - dodge / 100f);
        StringBuilder onHit = new StringBuilder();
        AppendHitProcs(onHit, action, displayedUnit, equipmentIndex);
        string guard = null;
        if (CombatStatusHandler.Instance != null &&
            CombatStatusHandler.Instance.TryPreviewGuard(target, 0, out _, out float negate))
        {
            guard = negate > 0f
                ? $"Guard active — {negate:0.#}% chance to negate damage"
                : "Damage includes active Guard";

        }

        attackPreviewUI.ShowDetails(new AttackPreviewUI.PreviewData
        {
            TargetName = target.data.characterName,
            TargetStats = $"HP {target.data.currentHealth}/{target.data.maxHealth}  " +
                $"Armor {target.data.armor}  Ward {target.data.ward}",
            ActionName = action.DisplayName,
            Damage = damage,
            HitCount = action.HitCount,
            HitChance = hitChance,
            DodgeChance = dodge,
            LandChance = chanceToLand,
            CriticalChance = action.CriticalChancePercent,
            CriticalDamage = criticalDamage,
            OnHit = onHit.ToString(),
            Guard = guard

        });

    }

    private static int FindPreviewEquipmentIndex(UnitController unit, ActionDefinition action)
    {
        if (unit == null || unit.data == null || unit.data.equippedItems == null)
        {
            return -1;

        }

        int sourceIndex = -1;
        for (int i = 0; i < unit.data.equippedItems.Count; i++)
        {
            EquipmentInstance item = unit.data.equippedItems[i];
            bool grants = item.grantedActionSet != null &&
                item.grantedActionSet.ContainsAction(action.Id);
            if (!grants && item.grantedActions != null)
            {
                foreach (ActionDefinition granted in item.grantedActions)
                {
                    if (granted != null && string.Equals(granted.Id, action.Id,
                        System.StringComparison.OrdinalIgnoreCase))
                    {
                        grants = true;

                        break;

                    }

                }

            }

            if (grants)
            {
                if (sourceIndex >= 0)
                {
                    return -1;

                }

                sourceIndex = i;

            }

        }

        return sourceIndex;

    }

    private static void AppendHitProcs(StringBuilder output, ActionDefinition action,
        UnitController unit, int equipmentIndex)
    {
        List<EffectGrant> grants = new List<EffectGrant>();
        if (action.OnDamageEffects != null && action.OnDamageEffects.Effects != null)
        {
            grants.AddRange(action.OnDamageEffects.Effects);

        }

        if (equipmentIndex >= 0 && unit.data.equippedItems != null &&
            equipmentIndex < unit.data.equippedItems.Count)
        {
            EquipmentInstance equipment = unit.data.equippedItems[equipmentIndex];
            if (equipment.grantedEffectCollection != null)
            {
                grants.AddRange(equipment.grantedEffectCollection.Effects);

            }

            if (equipment.grantedEffects != null)
            {
                grants.AddRange(equipment.grantedEffects);

            }

        }

        int count = 0;
        foreach (EffectGrant grant in grants)
        {
            if (grant == null || grant.Definition == null || grant.Activation == null ||
                grant.Activation.Trigger != EffectTrigger.OnDamageDealt ||
                grant.ProcChancePercent <= 0f ||
                grant.Definition.Operation != EffectOperation.ApplyStatus ||
                grant.OperationParameters == null ||
                grant.OperationParameters.StatusDefinition == null ||
                grant.Targeting == null ||
                grant.Targeting.Selection != EffectTargetSelection.Single ||
                (grant.Targeting.Reference != EffectTargetReference.EventTarget &&
                 grant.Targeting.Reference != EffectTargetReference.ActionTarget))
            {
                continue;

            }

            if (count > 0)
            {
                output.Append("  |  ");

            }

            output.Append($"{grant.OperationParameters.StatusDefinition.DisplayName} " +
                $"{grant.ProcChancePercent:0.#}%");
            count++;
            if (count >= 3)
            {
                break;

            }

        }

    }

    private void ShowPreviewMessage(string message)
    {
        attackPreviewUI.ShowMessage(message);

    }

    private void RequestConsumable(int index)
    {
        if (displayedUnit == null || displayedUnit.data == null || !CanCommand(displayedUnit))
        {
            return;

        }

        if (displayedUnit.TryUseConsumable(index))
        {
            targetedConsumable = TargetingModeController.IsTargeting &&
                index >= 0 && index < displayedUnit.data.carriedConsumables.Count
                ? displayedUnit.data.carriedConsumables[index].definition : null;
            CameraController.Instance?.FocusOnUnit(displayedUnit, true);
            // Preserve the item button and refresh its use count/availability.
            RebuildCategory();
            lastExpandedAP = float.NaN;
            RefreshActionInfoPanel();

        }

    }

    private void RequestInteraction(WorldInteractable target)
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
