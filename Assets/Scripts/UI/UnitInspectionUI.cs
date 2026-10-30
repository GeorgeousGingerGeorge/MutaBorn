using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UIButton = UnityEngine.UI.Button;


public sealed class UnitInspectionUI : MonoBehaviour
{
    [Header("Inspection Panel")]
    [Tooltip("Assign the panel GameObject. Keep this component on an always-active Canvas or Action Menu object.")]
    [SerializeField] private GameObject panelRoot;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private UIButton closeButton;

    [Header("Inspection Scrolling")]
    [Tooltip("Optional: UnitScroll in the inspection panel. Found automatically if unassigned.")]
    [SerializeField] private ScrollRect inspectionScroll;
    [SerializeField, Min(0f)] private float textPadding = 8f;

    [Header("Live Data")]
    [Tooltip("How often to refresh visible stats while inspecting a unit.")]
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.2f;

    private UnitController inspectedUnit;
    private float nextRefreshTime;
    private readonly StringBuilder details = new StringBuilder(1024);

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);

        }

        Close();

    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);

        }

    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;

        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();

            return;

        }

        if (inspectedUnit == null || !inspectedUnit.isActiveAndEnabled || inspectedUnit.data == null)
        {
            Close();

            return;

        }

        if (Time.unscaledTime >= nextRefreshTime)
        {
            Refresh();
            nextRefreshTime = Time.unscaledTime + refreshInterval;

        }

    }

    public void Open(UnitController unit)
    {
        if (unit == null || unit.data == null)
        {
            return;

        }

        if (panelRoot == null || detailsText == null)
        {
            Debug.LogWarning("UnitInspectionUI: Assign Panel Root and Details Text in the Inspector.", this);

            return;

        }

        inspectedUnit = unit;
        panelRoot.SetActive(true);
        ConfigureScroll();
        Refresh();

        if (inspectionScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            inspectionScroll.StopMovement();
            inspectionScroll.verticalNormalizedPosition = 1f;

        }
        nextRefreshTime = Time.unscaledTime + refreshInterval;

    }

    public void Close()
    {
        inspectedUnit = null;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);

        }

    }

    public void Refresh()
    {
        if (inspectedUnit == null || inspectedUnit.data == null || detailsText == null)
        {
            return;

        }

        UnitData data = inspectedUnit.data;
        UnitFaction unitFaction = inspectedUnit.GetComponent<UnitFaction>();
        string factionName = unitFaction != null && unitFaction.Faction != null
            ? unitFaction.Faction.name
            : "Unassigned";

        if (titleText != null)
        {
            titleText.text = $"{data.characterName} — {data.raceName}";

        }

        details.Clear();
        details.AppendLine("<b>IDENTITY</b>");
        details.AppendLine($"Name: {data.characterName}");
        details.AppendLine($"Race: {data.raceName}");
        details.AppendLine($"Faction: {factionName}");
        details.AppendLine();

        details.AppendLine("<b>Pools</b>");
        details.AppendLine($"Health: {data.currentHealth} / {data.maxHealth}");
        details.AppendLine($"Mana: {data.currentMana} / {data.maxMana}");
        details.AppendLine($"Action Points: {data.currentActionPoints} / {data.maxActionPoints}");
        details.AppendLine();

        details.AppendLine("<b>ATTRIBUTES</b>");
        details.AppendLine($"Strength: {data.totalStrength}     Dexterity: {data.totalDexterity}");
        details.AppendLine($"Vitality: {data.totalVitality}     Constitution: {data.totalConstitution}");
        details.AppendLine($"Perception: {data.totalPerception}     Intelligence: {data.totalIntelligence}");
        details.AppendLine();

        details.AppendLine("<b>COMBAT STATS</b>");
        details.AppendLine($"Melee Damage: {data.meleeDamage}");
        details.AppendLine($"Ranged Damage: {data.rangeDamage}");
        details.AppendLine($"Magic Damage: {data.magicDamage}");
        details.AppendLine($"Armor: {data.armor}     Will: {data.will}");
        details.AppendLine();

        details.AppendLine("<b>EQUIPMENT</b>");
        int equipmentCount = 0;

        if (data.equippedItems != null)
        {
            foreach (KeyValuePair<EquipmentSlots, EquipmentInstance> equipped in data.equippedItems)
            {
                if (equipped.Value.Equals(default(EquipmentInstance)))
                {
                    continue;

                }

                details.AppendLine($"{equipped.Key}: {equipped.Value.itemName}");
                equipmentCount++;


            }

        }

        if (equipmentCount == 0)
        {
            details.AppendLine("None");

        }

        details.AppendLine();
        details.AppendLine("<b>MUTATIONS</b>");
        int mutationCount = 0;

        if (data.activeMutations != null)
        {
            foreach (KeyValuePair<BodySlots, BaseMutation> equipped in data.activeMutations)
            {
                if (equipped.Value == null)
                {
                    continue;

                }

                details.AppendLine($"{equipped.Value.name} ({equipped.Key}, Stage {equipped.Value.currentStage})");
                mutationCount++;

            }

        }

        if (mutationCount == 0)
        {
            details.AppendLine("None");

        }

        string result = details.ToString();
        if (detailsText.text != result)
        {
            detailsText.text = result;
            RefreshScrollHeight();

        }

    }

    private void ConfigureScroll()
    {
        if (inspectionScroll == null && panelRoot != null)
        {
            inspectionScroll = panelRoot.GetComponentInChildren<ScrollRect>(true);

        }

        if (inspectionScroll == null || inspectionScroll.content == null || detailsText == null ||
            !detailsText.transform.IsChildOf(inspectionScroll.content))
        {
            return;

        }

        inspectionScroll.horizontal = false;
        inspectionScroll.vertical = true;
        inspectionScroll.movementType = ScrollRect.MovementType.Clamped;
        inspectionScroll.scrollSensitivity = Mathf.Max(25f, inspectionScroll.scrollSensitivity);

        // Repair the scene's collapsed 0 x 0 UnitScroll viewport at runtime.
        RectTransform viewport = inspectionScroll.viewport;
        if (viewport != null && viewport.anchorMin == viewport.anchorMax &&
            viewport.sizeDelta.sqrMagnitude < 1f)
        {
            float scrollbarWidth = inspectionScroll.verticalScrollbar != null
                ? Mathf.Max(16f, inspectionScroll.verticalScrollbar.GetComponent<RectTransform>().rect.width)
                : 0f;

            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = new Vector2(-scrollbarWidth, 0f);

        }

        RectTransform content = inspectionScroll.content;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, content.sizeDelta.y);

        RectTransform textRect = detailsText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(textPadding, -textPadding);
        textRect.sizeDelta = new Vector2(-2f * textPadding, textRect.sizeDelta.y);

        Canvas.ForceUpdateCanvases();
        RefreshScrollHeight();

    }

    private void RefreshScrollHeight()
    {
        if (inspectionScroll == null || inspectionScroll.content == null ||
            inspectionScroll.viewport == null || detailsText == null)
        {
            return;

        }

        float availableWidth = Mathf.Max(80f, inspectionScroll.viewport.rect.width - 2f * textPadding);
        float preferredTextHeight = Mathf.Ceil(detailsText.GetPreferredValues(detailsText.text, availableWidth, 0f).y);
        float contentHeight = Mathf.Max(inspectionScroll.viewport.rect.height, preferredTextHeight + 2f * textPadding);

        detailsText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredTextHeight);
        inspectionScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

    }

}
