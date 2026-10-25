using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UIButton = UnityEngine.UI.Button;


public sealed class UnitInspectionUI : MonoBehaviour
{
    [Header("Inspection Panel")]
    [Tooltip("Assign the panel GameObject. Keep this component on an always-active Canvas or Action Menu object.")]
    [SerializeField] private GameObject panelRoot;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private UIButton closeButton;

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
        Refresh();
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
            foreach (KeyValuePair<EquipmentSlots, EquipmentData> equipped in data.equippedItems)
            {
                if (equipped.Value == null)
                {
                    continue;

                }

                details.AppendLine($"{equipped.Key}: {equipped.Value.name}");
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

        detailsText.text = details.ToString();

    }

}
