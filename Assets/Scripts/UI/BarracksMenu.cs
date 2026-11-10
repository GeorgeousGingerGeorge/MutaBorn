using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using MutaBorn.Gameplay;

// Inspector-wired Barracks / Unit Details UI. All panels, buttons and reusable
// row prefabs are authored in Unity; this script creates ONLY repeated row instances.
// Permanent units and exact item instances remain owned by UnitRoster and ItemStorage.
public sealed class BarracksMenu : MonoBehaviour
{
    [Serializable]
    private sealed class RaceArt
    {
        public DynamicStats raceDefinition;
        public string raceNameFallback;
        public Sprite rosterIcon;
        public Sprite unitPortrait;

    }

    [Serializable]
    private sealed class SlotUI
    {
        public UnitSlotId slot;
        public Button button;
        [Tooltip("Drag the child item icon Image, not the button's background Image.")]
        public Image itemIcon;
        public TMP_Text slotLabel;

    }

    [Header("Existing Unity Screen Objects")]
    [SerializeField] private GameObject barracksRoot;
    [SerializeField] private GameObject unitDetailsRoot;
    [SerializeField] private Button closeDetailsButton;

    [Header("Roster Scroll View")]
    [SerializeField] private Transform rosterContent;
    [SerializeField] private BarracksUnitRowUI unitRowPrefab;
    [SerializeField] private List<RaceArt> raceArtwork = new List<RaceArt>();
    [SerializeField] private Sprite defaultUnitPortrait;

    [Header("Selected Unit - Separate Fields")]
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text raceNameText;
    [SerializeField] private Image unitPortraitImage;
    [SerializeField] private TMP_Text unitStatsText;
    [SerializeField] private TMP_Text unitMutationsText;
    [SerializeField] private Button renameButton;
    [SerializeField] private TMP_InputField renameInput;

    [Header("Mutations Panel - Existing Unity Objects")]
    [SerializeField] private Button mutationsButton;
    [SerializeField] private GameObject mutationsPanel;
    [SerializeField] private Button closeMutationsButton;

    [Header("Optional Leveling Controls - Author in Unity")]
    [SerializeField] private TMP_Dropdown attributeChoiceDropdown;
    [SerializeField] private Button spendAttributePointButton;

    [Header("Optional Mutation Controls - Author in Unity")]
    [SerializeField] private TMP_Dropdown mutationChoiceDropdown;
    [SerializeField] private Button applyMutationButton;
    private readonly List<(MutationID current, MutationID next)> mutationChoices =
        new List<(MutationID current, MutationID next)>();

    [Header("Fourteen Existing Equipment Slot Buttons")]
    [SerializeField] private List<SlotUI> slotButtons = new List<SlotUI>();
    [SerializeField] private Button lockSlotButton;
    [Tooltip("Shown for empty slots locked by unit progression/research.")]
    [SerializeField] private Sprite lockedSlotSprite;
    [Tooltip("Shown on the free hand blocked by a two-handed weapon.")]
    [SerializeField] private Sprite twoHandedLockSprite;

    [Header("Item Inspector - Existing UI")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemTypeText;
    [SerializeField] private Image itemIcon;
    [Tooltip("Exactly five Images, ordered first star through fifth star.")]
    [SerializeField] private Image[] itemStars = new Image[5];
    [SerializeField] private Color inactiveStarColor = Color.black;
    [SerializeField] private Color activeStarColor = new Color(1f, 0.78f, 0.10f, 1f);
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private TMP_Text itemStatsText;
    [SerializeField] private Transform grantedActionsContent;
    [SerializeField] private BarracksAbilityEntryUI grantedActionPrefab;
    [SerializeField] private Button changeButton;
    [SerializeField] private Button unequipButton;
    [Tooltip("Optional shortcut to Workshop Upgrades; add this button using the Workshop Editor tool.")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Optional Storage Picker - For Change Button")]
    [SerializeField] private GameObject storagePickerPanel;
    [SerializeField] private Transform storageContent;
    [SerializeField] private BarracksStoredItemRowUI storageItemRowPrefab;
    [SerializeField] private Button equipSelectedButton;
    [SerializeField] private Button closePickerButton;
    [Header("Storage Picker Tile Layout")]
    [SerializeField] private bool useStorageTileGrid = true;
    [SerializeField] private Vector2 storageTileSize = new Vector2(106f, 106f);
    [SerializeField] private int storageTileColumns = 4;
    [SerializeField] private bool filterPickerBySelectedSlot = true;

    private readonly List<GameObject> generatedRosterRows = new List<GameObject>();
    private readonly List<GameObject> generatedAbilityRows = new List<GameObject>();
    private readonly List<GameObject> generatedStorageRows = new List<GameObject>();
    private string selectedUnitId;
    private string selectedStoredItemId;
    private UnitData externalUnit;
    private UnitSlotId selectedSlot = UnitSlotId.RightHand;
    private bool allowEditing = true;
    private bool transferInProgress;
    private UnitData listenedUnit;
    private UnitRoster subscribedRoster;
    private ItemStorage subscribedStorage;
    private bool renaming;
    private string renameDraft;
    private TMP_Text renameButtonLabel;
    private string originalRenameButtonLabel;
    private Keyboard renameKeyboard;

    private void Awake()
    {
        ConfigureStorageTileLayout();
        ConfigureUnitStatsViewport();
        if (renameButton != null)
        {
            renameButtonLabel = renameButton.GetComponentInChildren<TMP_Text>();
            originalRenameButtonLabel = renameButtonLabel == null ? "Rename" : renameButtonLabel.text;

        }

        if (closeDetailsButton != null)
        {
            closeDetailsButton.onClick.AddListener(CloseDetails);

        }

        if (changeButton != null)
        {
            changeButton.onClick.AddListener(OpenStoragePicker);

        }

        if (unequipButton != null)
        {
            unequipButton.onClick.AddListener(ReturnSelectedItem);

        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OpenSelectedUpgrade);

        }

        if (lockSlotButton != null)
        {
            lockSlotButton.onClick.AddListener(ToggleSlotLock);

        }

        if (equipSelectedButton != null)
        {
            equipSelectedButton.onClick.AddListener(EquipSelectedItem);

        }

        if (closePickerButton != null)
        {
            closePickerButton.onClick.AddListener(CloseStoragePicker);

        }

        if (renameButton != null)
        {
            renameButton.onClick.AddListener(ApplyRename);

        }

        if (mutationsButton != null)
        {
            mutationsButton.onClick.AddListener(OpenMutationsPanel);

        }

        if (closeMutationsButton != null)
        {
            closeMutationsButton.onClick.AddListener(CloseMutationsPanel);

        }

        if (attributeChoiceDropdown != null)
        {
            attributeChoiceDropdown.ClearOptions();
            attributeChoiceDropdown.AddOptions(new List<string>
            {
                "Strength", "Dexterity", "Vitality", "Constitution", "Perception", "Intelligence"
            });

        }

        if (spendAttributePointButton != null)
        {
            spendAttributePointButton.onClick.AddListener(SpendSelectedAttributePoint);

        }

        if (applyMutationButton != null)
        {
            applyMutationButton.onClick.AddListener(ApplySelectedMutation);

        }


        foreach (SlotUI entry in slotButtons)
        {
            if (entry == null || entry.button == null || !UnitLoadoutSlots.IsValid(entry.slot))
            {
                continue;

            }

            UnitSlotId capturedSlot = entry.slot;
            entry.button.onClick.AddListener(() => SelectSlot(capturedSlot));

        }

    }

    // Lays out the EXISTING Unity Content object; creates no panels, graphics or buttons.
    // You can attach and edit GridLayoutGroup/ContentSizeFitter yourself in Unity later.
    private void ConfigureStorageTileLayout()
    {
        if (!useStorageTileGrid || storageContent == null)
        {
            return;

        }

        GridLayoutGroup grid = storageContent.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = storageContent.gameObject.AddComponent<GridLayoutGroup>();

        }

        grid.cellSize = storageTileSize;
        grid.spacing = new Vector2(8f, 8f);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, storageTileColumns);

        ContentSizeFitter fitter = storageContent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = storageContent.gameObject.AddComponent<ContentSizeFitter>();

        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    }

    // Keep the existing Unity-authored Scroll View, but use RectMask2D for TMP text.
    // This swaps the mask COMPONENT only; no UI panels or text objects are generated.
    private void ConfigureUnitStatsViewport()
    {
        if (unitStatsText == null)
        {
            return;

        }

        ScrollRect scroll = unitStatsText.GetComponentInParent<ScrollRect>(true);
        if (scroll == null || scroll.viewport == null)
        {
            return;

        }

        RectTransform viewport = scroll.viewport;
        Mask oldMask = viewport.GetComponent<Mask>();
        if (oldMask != null)
        {
            oldMask.enabled = false;

        }

        Image maskImage = viewport.GetComponent<Image>();
        if (maskImage != null)
        {
            maskImage.enabled = false;

        }

        if (viewport.GetComponent<RectMask2D>() == null)
        {
            viewport.gameObject.AddComponent<RectMask2D>();

        }

    }

    private void OnEnable()
    {
        Subscribe();
        RefreshAll();

    }

    private void Start()
    {
        Subscribe();
        RefreshAll();

    }

    private void OnDisable()
    {
        CancelRename();
        Unsubscribe();

    }

    private void OnDestroy()
    {
        CancelRename();
        Unsubscribe();

    }

    // Assign this to the Hub's existing Barracks tab through Button -> On Click().
    public void Open()
    {
        externalUnit = null;
        allowEditing = true;
        if (barracksRoot != null)
        {
            barracksRoot.SetActive(true);

        }

        Subscribe();
        RefreshAll();

    }

    public void Close()
    {
        CancelRename();
        CloseMutationsPanel();
        CloseStoragePicker();
        if (barracksRoot != null)
        {
            barracksRoot.SetActive(false);

        }

    }

    // The future combat inspector can call this with editable=false without
    // giving combat UI access to transfers or slot locks.
    public void ShowUnit(UnitData unit, bool editable)
    {
        CancelRename();
        externalUnit = unit;
        selectedUnitId = unit == null ? null : unit.UnitId;
        allowEditing = editable;
        selectedStoredItemId = null;
        if (unitDetailsRoot != null)
        {
            unitDetailsRoot.SetActive(true);

        }

        ListenToUnit(unit);
        RefreshDetails();

    }

    public void OpenMutationsPanel()
    {
        if (mutationsPanel != null && SelectedUnit() != null)
        {
            mutationsPanel.SetActive(true);

        }

    }

    public void CloseMutationsPanel()
    {
        if (mutationsPanel != null)
        {
            mutationsPanel.SetActive(false);

        }

    }

    private void CloseDetails()
    {
        CancelRename();
        CloseMutationsPanel();
        CloseStoragePicker();
        if (unitDetailsRoot != null)
        {
            unitDetailsRoot.SetActive(false);

        }

    }

    private void Subscribe()
    {
        if (subscribedRoster != UnitRoster.Instance)
        {
            if (subscribedRoster != null)
            {
                subscribedRoster.RosterChanged -= OnModelsChanged;

            }

            subscribedRoster = UnitRoster.Instance;
            if (subscribedRoster != null)
            {
                subscribedRoster.RosterChanged += OnModelsChanged;

            }

        }

        if (subscribedStorage != ItemStorage.Instance)
        {
            if (subscribedStorage != null)
            {
                subscribedStorage.StorageChanged -= OnModelsChanged;

            }

            subscribedStorage = ItemStorage.Instance;
            if (subscribedStorage != null)
            {
                subscribedStorage.StorageChanged += OnModelsChanged;

            }

        }

    }

    private void Unsubscribe()
    {
        if (subscribedRoster != null)
        {
            subscribedRoster.RosterChanged -= OnModelsChanged;
            subscribedRoster = null;

        }

        if (subscribedStorage != null)
        {
            subscribedStorage.StorageChanged -= OnModelsChanged;
            subscribedStorage = null;

        }

        ListenToUnit(null);

    }

    private void ListenToUnit(UnitData unit)
    {
        if (ReferenceEquals(listenedUnit, unit))
        {
            return;

        }

        if (listenedUnit != null)
        {
            listenedUnit.LoadoutChanged -= OnModelsChanged;

        }

        listenedUnit = unit;
        if (listenedUnit != null)
        {
            listenedUnit.LoadoutChanged += OnModelsChanged;

        }

    }

    private void OnModelsChanged()
    {
        if (!transferInProgress && isActiveAndEnabled)
        {
            RefreshAll();

        }

    }

    private UnitData SelectedUnit()
    {
        if (externalUnit != null)
        {
            return externalUnit;

        }

        UnitRoster roster = UnitRoster.Instance;
        if (roster != null && roster.TryGetUnit(selectedUnitId, out UnitData unit))
        {
            return unit;

        }

        return null;

    }

    private bool CanEdit(UnitData unit)
    {
        UnitRoster roster = UnitRoster.Instance;
        return allowEditing && externalUnit == null && unit != null && roster != null &&
            ItemStorage.Instance != null && roster.TryGetUnit(unit.UnitId, out UnitData owned) &&
            ReferenceEquals(unit, owned) && !roster.IsUnitDeployed(unit.UnitId);

    }

    private void RefreshAll()
    {
        UnitRoster roster = UnitRoster.Instance;
        if (externalUnit == null && roster != null &&
            (string.IsNullOrEmpty(selectedUnitId) ||
             !roster.TryGetUnit(selectedUnitId, out UnitData ignored)))
        {
            selectedUnitId = roster.Units.Count > 0 && roster.Units[0] != null
                ? roster.Units[0].UnitId : null;

        }

        RefreshRoster();
        RefreshDetails();
        if (storagePickerPanel != null && storagePickerPanel.activeSelf)
        {
            RefreshStoragePicker();

        }

    }

    private void RefreshRoster()
    {
        ClearGenerated(generatedRosterRows);
        if (rosterContent == null || unitRowPrefab == null)
        {
            return;

        }

        UnitRoster roster = UnitRoster.Instance;
        if (roster == null)
        {
            return;

        }

        foreach (UnitData unit in roster.Units)
        {
            if (unit == null)
            {
                continue;

            }

            BarracksUnitRowUI row = Instantiate(unitRowPrefab, rosterContent);
            RaceArt art = FindRaceArt(unit);
            row.Bind(unit, art == null ? null : art.rosterIcon,
                unit.UnitId == selectedUnitId, roster.IsUnitDeployed(unit.UnitId), SelectRosterUnit);
            generatedRosterRows.Add(row.gameObject);

        }

    }

    private void SelectRosterUnit(string unitId)
    {
        CancelRename();
        externalUnit = null;
        allowEditing = true;
        selectedUnitId = unitId;
        selectedStoredItemId = null;
        CloseStoragePicker();
        if (unitDetailsRoot != null)
        {
            unitDetailsRoot.SetActive(true);

        }

        RefreshAll();

    }

    private RaceArt FindRaceArt(UnitData unit)
    {
        if (unit == null)
        {
            return null;

        }

        foreach (RaceArt entry in raceArtwork)
        {
            if (entry != null && entry.raceDefinition != null &&
                entry.raceDefinition == unit.RaceDefinition)
            {
                return entry;

            }

        }

        foreach (RaceArt entry in raceArtwork)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.raceNameFallback) &&
                string.Equals(entry.raceNameFallback, unit.raceName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return entry;

            }

        }

        return null;

    }

    private void SpendSelectedAttributePoint()
    {
        UnitData unit = SelectedUnit();
        if (!CanEdit(unit) || attributeChoiceDropdown == null ||
            !unit.TrySpendAttributePoint((StatIdentifier)(attributeChoiceDropdown.value + 1)))
        {
            SetStatus("Cannot spend a level-up point.");

            return;

        }

        SetStatus("Attribute upgraded.");
        RefreshAll();

    }

    private void RefreshProgressionOptions(UnitData unit)
    {
        if (spendAttributePointButton != null)
        {
            spendAttributePointButton.interactable = CanEdit(unit) && unit.UnspentAttributePoints > 0;

        }

        if (mutationChoiceDropdown == null)
        {
            if (applyMutationButton != null)
            {
                applyMutationButton.interactable = false;

            }

            return;

        }

        mutationChoices.Clear();
        List<string> labels = new List<string>();
        MutationDataBase database = GameDataBaseLoader.Instance != null
            ? GameDataBaseLoader.Instance.mutationData : null;
        bool researchUnlocked = ResearchManager.Instance == null ||
            ResearchManager.Instance.HasBenefit(ResearchBenefitType.MutationResearch);
        if (unit != null && database != null && researchUnlocked)
        {
            foreach (MutationNodeDefinition node in database.MutationNodes)
            {
                if (node != null && !node.HiddenFromAcquisition && node.Level == 1 && unit.CanEquipMutation(node.Id))
                {
                    mutationChoices.Add((MutationID.none, node.Id));
                    labels.Add($"Acquire {node.DisplayName} (50 points)");

                }

            }

            foreach (ActiveMutationRecord owned in unit.activeMutationNodes)
            {
                MutationNodeDefinition current = database.GetNode(owned.MutationId);
                if (current == null)
                {
                    continue;

                }

                foreach (MutationID nextId in current.NextEvolutions)
                {
                    MutationNodeDefinition next = database.GetNode(nextId);
                    if (next != null)
                    {
                        mutationChoices.Add((current.Id, nextId));
                        labels.Add($"Evolve to {next.DisplayName} ({next.Level * 50} points)");

                    }

                }

            }

        }

        int previous = mutationChoiceDropdown.value;
        mutationChoiceDropdown.ClearOptions();
        mutationChoiceDropdown.AddOptions(labels.Count == 0 ?
            new List<string> { researchUnlocked ? "No mutations available" : "Unlock Mutation Research in the Lab" } : labels);
        mutationChoiceDropdown.SetValueWithoutNotify(Mathf.Clamp(previous, 0, Mathf.Max(0, labels.Count - 1)));
        mutationChoiceDropdown.RefreshShownValue();
        if (applyMutationButton != null)
        {
            applyMutationButton.interactable = CanEdit(unit) && mutationChoices.Count > 0;

        }

    }

    private void ApplySelectedMutation()
    {
        if (ResearchManager.Instance != null &&
            !ResearchManager.Instance.HasBenefit(ResearchBenefitType.MutationResearch))
        {
            SetStatus("Unlock Mutation Research in the Lab first.");

            return;

        }

        UnitData unit = SelectedUnit();
        if (!CanEdit(unit) || mutationChoiceDropdown == null ||
            mutationChoiceDropdown.value < 0 || mutationChoiceDropdown.value >= mutationChoices.Count)
        {
            return;

        }

        (MutationID current, MutationID next) choice = mutationChoices[mutationChoiceDropdown.value];
        MutationDataBase database = GameDataBaseLoader.Instance != null
            ? GameDataBaseLoader.Instance.mutationData : null;
        bool succeeded = choice.current == MutationID.none
            ? unit.TryPurchaseMutation(choice.next, database)
            : unit.TryEvolveMutation(choice.current, choice.next);
        SetStatus(succeeded ? "Mutation updated." : "Not enough mutation points or slots are occupied.");
        RefreshAll();

    }

    private void RefreshDetails()
    {
        UnitData unit = SelectedUnit();
        ListenToUnit(unit);
        if (unitNameText != null)
        {
            unitNameText.text = renaming ? renameDraft + " |" :
                unit == null ? "Select a unit" : unit.characterName;

        }

        if (raceNameText != null)
        {
            raceNameText.text = unit == null ? "" : unit.raceName;

        }

        if (unitPortraitImage != null)
        {
            RaceArt art = FindRaceArt(unit);
            Sprite portrait = art != null && art.unitPortrait != null
                ? art.unitPortrait : defaultUnitPortrait;
            unitPortraitImage.sprite = portrait;
            unitPortraitImage.enabled = portrait != null;
            unitPortraitImage.raycastTarget = false;

        }

        if (unitStatsText != null)
        {
            unitStatsText.text = unit == null ? "" :
                $"Level {unit.Level}   XP {unit.Experience}/{unit.ExperienceToNextLevel}   Points {unit.UnspentAttributePoints}\n" +
                "HP  " + unit.currentHealth + "/" + unit.maxHealth + "\n" +
                "Mana  " + unit.currentMana + "/" + unit.maxMana + "\n" +
                "AP  " + unit.currentActionPoints + "/" + unit.maxActionPoints + "\n" +
                "Armor  " + unit.armor + "     Ward  " + unit.ward + "\n" +
                "STR " + unit.totalStrength + "   DEX " + unit.totalDexterity + "\n" +
                "VIT " + unit.totalVitality + "   CON " + unit.totalConstitution + "\n" +
                "PER " + unit.totalPerception + "   INT " + unit.totalIntelligence + "\n" +
                "Melee " + unit.meleeDamage + "   Ranged " + unit.rangeDamage +
                "   Magic " + unit.magicDamage;
            // Rebuild the glyph mesh after switching the Scroll View's mask.
            unitStatsText.ForceMeshUpdate();

        }

        if (unitMutationsText != null)
        {
            StringBuilder names = new StringBuilder();
            if (unit != null && unit.activeMutationNodes != null)
            {
                foreach (ActiveMutationRecord record in unit.activeMutationNodes)
                {
                    if (record == null)
                    {
                        continue;

                    }

                    if (names.Length > 0)
                    {
                        names.Append("\n");

                    }

                    names.Append(record.MutationId);

                }

            }

            unitMutationsText.text = unit == null ? "Select a unit" :
                $"Mutation Points: {unit.MutationPoints}\n" +
                (names.Length == 0 ? "None" : names.ToString());

        }

        RefreshProgressionOptions(unit);
        bool editable = CanEdit(unit);
        if (renameButton != null)
        {
            renameButton.interactable = editable;

        }

        if (renameInput != null)
        {
            renameInput.text = unit == null ? "" : unit.characterName;
            renameInput.interactable = editable;

        }

        foreach (SlotUI entry in slotButtons)
        {
            if (entry == null || !UnitLoadoutSlots.IsValid(entry.slot))
            {
                continue;

            }

            if (entry.button != null)
            {
                entry.button.interactable = unit != null;

            }

            Sprite sprite = null;
            if (unit != null && unit.TryGetEquipmentAtSlot(entry.slot, out EquipmentInstance gear))
            {
                sprite = gear.template == null ? null : gear.template.sprite;

            }
            else if (unit != null &&
                unit.TryGetConsumableAtSlot(entry.slot, out ConsumableInstance consumable))
            {
                sprite = consumable.definition == null ? null : consumable.definition.sprite;

            }

            // Draw a lock only when there is no physical item in this slot.
            // Two-handed occupancy has priority over the regular lock icon.
            UnitSlotBlockReason blockReason = unit == null ? UnitSlotBlockReason.None :
                unit.GetSlotBlockReason(entry.slot);
            if (sprite == null && blockReason == UnitSlotBlockReason.TwoHandedWeapon)
            {
                sprite = twoHandedLockSprite != null ? twoHandedLockSprite : lockedSlotSprite;

            }
            else if (sprite == null &&
                (blockReason == UnitSlotBlockReason.ProgressionLocked ||
                 blockReason == UnitSlotBlockReason.PlayerLocked ||
                 blockReason == UnitSlotBlockReason.MutationOccupied))
            {
                sprite = lockedSlotSprite;

            }

            if (entry.itemIcon != null)
            {
                entry.itemIcon.sprite = sprite;
                entry.itemIcon.enabled = sprite != null;
                entry.itemIcon.raycastTarget = false;

            }

            if (entry.slotLabel != null && unit != null)
            {
                UnitSlotBlockReason blocked = unit.GetSlotBlockReason(entry.slot);
                entry.slotLabel.text = blocked != UnitSlotBlockReason.None &&
                    blocked != UnitSlotBlockReason.ItemOccupied
                    ? blocked.ToString() : "";

            }

        }

        ShowSelectedSlotItem();

    }

    private void SelectSlot(UnitSlotId slot)
    {
        selectedSlot = slot;
        selectedStoredItemId = null;
        ShowSelectedSlotItem();

    }

    private void ShowSelectedSlotItem()
    {
        UnitData unit = SelectedUnit();
        if (unit == null)
        {
            ShowEmptyItem("Select a unit to inspect equipment.");
            SetEditButtons(false, false);

            return;

        }

        if (unit.TryGetEquipmentAtSlot(selectedSlot, out EquipmentInstance gear))
        {
            ShowEquipment(gear);

        }
        else if (unit.TryGetConsumableAtSlot(selectedSlot, out ConsumableInstance consumable))
        {
            ShowConsumable(consumable);

        }
        else
        {
            ShowEmptyItem(selectedSlot + " is empty. " + unit.GetSlotBlockReason(selectedSlot));

        }

        UnitLoadoutSlotState state = unit.GetSlot(selectedSlot);
        bool editable = CanEdit(unit);
        bool hasItem = state != null && !string.IsNullOrEmpty(state.ItemInstanceId);
        bool modifiable = state != null && state.ProgressionUnlocked &&
            !state.PlayerLocked && state.OccupyingMutation == MutationID.none;
        SetEditButtons(editable && modifiable, editable && modifiable && hasItem);
        if (lockSlotButton != null)
        {
            lockSlotButton.interactable = editable && state != null;

        }

    }

    public void OpenSelectedUpgrade()
    {
        if (ResearchManager.Instance != null &&
            !ResearchManager.Instance.HasBenefit(ResearchBenefitType.EquipmentUpgrades))
        {
            SetStatus("Unlock Equipment Upgrades through research first.");

            return;

        }

        UnitData unit = SelectedUnit();
        if (!CanEdit(unit) || !unit.TryGetEquipmentAtSlot(selectedSlot, out EquipmentInstance gear))
        {
            SetStatus("Select an owned piece of equipment in Barracks first.");
            return;
        }

        WorkshopUpgradeMenu workshop = FindAnyObjectByType<WorkshopUpgradeMenu>(FindObjectsInactive.Include);
        if (workshop == null)
        {
            SetStatus("Workshop Upgrades not installed. Run the Workshop Upgrade UI builder.");
            return;
        }

        Close();
        workshop.OpenForEquipped(unit.UnitId, gear.instanceId);

    }

    private void SetEditButtons(bool canChange, bool canRemove)
    {
        if (changeButton != null)
        {
            changeButton.gameObject.SetActive(allowEditing);
            changeButton.interactable = canChange;

        }

        if (unequipButton != null)
        {
            unequipButton.gameObject.SetActive(allowEditing);
            unequipButton.interactable = canRemove;

        }

        if (upgradeButton != null)
        {
            bool upgradeUnlocked = ResearchManager.Instance == null ||
                ResearchManager.Instance.HasBenefit(ResearchBenefitType.EquipmentUpgrades);
            upgradeButton.gameObject.SetActive(allowEditing && upgradeUnlocked);
            UnitData unit = SelectedUnit();
            upgradeButton.interactable = allowEditing && unit != null && CanEdit(unit) &&
                unit.TryGetEquipmentAtSlot(selectedSlot, out EquipmentInstance gear) &&
                gear.rarity < EquipmentGrade.FiveStar;

        }

        if (lockSlotButton != null)
        {
            lockSlotButton.gameObject.SetActive(allowEditing);

        }

    }

    private void ShowEquipment(EquipmentInstance gear)
    {
        SetItemHeader(!string.IsNullOrWhiteSpace(gear.itemName) ? gear.itemName :
            ItemName(gear.template), gear.template == null ? null : gear.template.sprite,
            gear.template == null ? "Equipment" :
            (string.IsNullOrWhiteSpace(gear.template.DisplayTypeLabel)
                ? gear.template.EquipmentFit.ToString()
                : gear.template.DisplayTypeLabel));
        SetStars((int)gear.rarity);
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = gear.template == null ? "" : gear.template.description;

        }

        if (itemStatsText != null)
        {
            itemStatsText.text = EquipmentStats(gear, SelectedUnit());

        }

        List<ActionDefinition> actions = new List<ActionDefinition>();
        if (gear.grantedActionSet != null && gear.grantedActionSet.Actions != null)
        {
            actions.AddRange(gear.grantedActionSet.Actions);

        }

        if (gear.grantedActions != null)
        {
            actions.AddRange(gear.grantedActions);

        }

        ShowActions(actions);

    }

    private void ShowConsumable(ConsumableInstance item)
    {
        ConsumableData definition = item.definition;
        SetItemHeader(ItemName(definition), definition == null ? null : definition.sprite,
            "Consumable");
        SetStars(0);
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = definition == null ? "" : definition.description;

        }

        if (itemStatsText != null)
        {
            itemStatsText.text = "Remaining uses: " + item.remainingUses +
                "/" + (definition == null ? 0 : definition.MaxUses) + "\n" +
                "Magnitude: " + item.magnitude + "\nDamage: " + item.damage;

        }

        List<ActionDefinition> actions = new List<ActionDefinition>();
        if (definition != null && definition.UseAction != null)
        {
            actions.Add(definition.UseAction);

        }

        ShowActions(actions);

    }

    private void ShowEmptyItem(string description)
    {
        SetItemHeader("No item", null, "");
        SetStars(0);
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = description;

        }

        if (itemStatsText != null)
        {
            itemStatsText.text = "";

        }

        ShowActions(null);

    }

    private void SetItemHeader(string name, Sprite sprite, string type)
    {
        if (itemNameText != null)
        {
            itemNameText.text = name;

        }

        if (itemTypeText != null)
        {
            itemTypeText.text = type;

        }

        if (itemIcon != null)
        {
            itemIcon.sprite = sprite;
            itemIcon.enabled = sprite != null;
            itemIcon.raycastTarget = false;

        }

    }

    private void SetStars(int count)
    {
        int earned = Mathf.Clamp(count, 0, 5);
        if (itemStars == null)
        {
            return;

        }

        for (int i = 0; i < itemStars.Length; i++)
        {
            if (itemStars[i] != null)
            {
                itemStars[i].color = i < earned ? activeStarColor : inactiveStarColor;

            }

        }

    }

    private void ShowActions(List<ActionDefinition> actions)
    {
        ClearGenerated(generatedAbilityRows);
        if (grantedActionsContent == null || grantedActionPrefab == null || actions == null)
        {
            return;

        }

        HashSet<string> shown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ActionDefinition action in actions)
        {
            if (action == null)
            {
                continue;

            }

            string key = string.IsNullOrWhiteSpace(action.Id) ? action.name : action.Id;
            if (!shown.Add(key))
            {
                continue;

            }

            BarracksAbilityEntryUI entry = Instantiate(grantedActionPrefab, grantedActionsContent);
            entry.Bind(action);
            generatedAbilityRows.Add(entry.gameObject);

        }

    }

    private static string EquipmentStats(EquipmentInstance gear, UnitData owner)
    {
        StringBuilder info = new StringBuilder();
        info.AppendLine("Generation: " + gear.type);
        EquipmentAttributeScaling grades = gear.attributeScaling;
        info.AppendLine("Attribute scaling: " + (owner != null ?
            "+" + grades.TotalPercent(owner) + "% for this unit" : "no wielder"));
        for (int i = 0; i < 6; i++)
            info.Append(EquipmentAttributeScaling.Name(i)).Append(' ').Append(grades.Get(i))
                .Append(i == 2 ? "\n" : "   ");
        info.AppendLine();

        if (gear.primaryDamageType != DamageType.Unspecified)
        {
            info.AppendLine("Primary damage type: " + gear.primaryDamageType);

        }

        EquipmentStatValues effective = EquipmentGenerator.GetEffectiveStats(gear, owner);
        info.AppendLine("--- Permanent / Unscaled Bonus / Equipped ---");
        AppendStatBreakdown(info, "Armor", gear.baseStats.armor, gear.rolledStats.armor, effective.armor);
        AppendStatBreakdown(info, "Ward", gear.baseStats.ward, gear.rolledStats.ward, effective.ward);
        AppendStatBreakdown(info, "Melee Damage", gear.baseStats.meleeDamage, gear.rolledStats.meleeDamage, effective.meleeDamage);
        AppendStatBreakdown(info, "Ranged Damage", gear.baseStats.rangeDamage, gear.rolledStats.rangeDamage, effective.rangeDamage);
        AppendStatBreakdown(info, "Magic Damage", gear.baseStats.magicDamage, gear.rolledStats.magicDamage, effective.magicDamage);
        AppendStatBreakdown(info, "Accuracy", gear.baseStats.accuracy, gear.rolledStats.accuracy, effective.accuracy);
        AppendStatBreakdown(info, "Movement Penalty", gear.baseStats.movementPenalty, gear.rolledStats.movementPenalty, effective.movementPenalty);

        if (gear.resistances != null)
        {
            foreach (DamageResistance resistance in gear.resistances)
            {
                if (Math.Abs(resistance.Percent) > 0.001f)
                {
                    info.AppendLine(resistance.Type + " Resistance: " +
                        resistance.Percent.ToString("+0.##;-0.##;0") + "%");

                }

            }

        }

        if (gear.bonusDamage != null)
        {
            foreach (DamageComponent extra in gear.bonusDamage)
            {
                if (extra.Amount != 0)
                {
                    info.AppendLine("Extra " + extra.Type + " Damage: " + extra.Amount);

                }

            }

        }

        if (gear.grantedEffects != null)
        {
            foreach (EffectGrant effect in gear.grantedEffects)
            {
                if (effect != null && effect.Definition != null)
                {
                    info.AppendLine("Effect: " + effect.Definition.name);

                }

            }

        }

        if (gear.grantedEffectCollection != null &&
            gear.grantedEffectCollection.Effects != null)
        {
            foreach (EffectGrant effect in gear.grantedEffectCollection.Effects)
            {
                if (effect != null && effect.Definition != null)
                {
                    info.AppendLine("Effect: " + effect.Definition.name);

                }

            }

        }

        return info.ToString().TrimEnd();

    }

    private static void AppendStatBreakdown(StringBuilder text, string name,
        int baseValue, int rolledValue, int finalValue)
    {
        // The final column comes from the wielder's live attribute scaling and unscaled bonuses.
        text.Append(name).Append(": ").Append(finalValue);
        text.Append("   (base ").Append(baseValue);
        if (rolledValue != 0)
        {
            text.Append(", roll ").Append(rolledValue > 0 ? "+" : "").Append(rolledValue);

        }

        text.AppendLine(")");

    }

    private static string ItemName(ItemData item)
    {
        return item == null ? "Missing Item" :
            string.IsNullOrWhiteSpace(item.itemName) ? item.name : item.itemName;

    }

    private void OpenStoragePicker()
    {
        UnitData unit = SelectedUnit();
        if (!CanEdit(unit))
        {
            SetStatus("Cannot change equipment on a deployed or non-roster unit.");

            return;

        }

        if (storagePickerPanel == null || storageContent == null || storageItemRowPrefab == null)
        {
            SetStatus("Connect a Storage Picker panel, Content, and row prefab in the Inspector.");

            return;

        }

        selectedStoredItemId = null;
        bool anyCompatible = false;
        if (ItemStorage.Instance != null)
        {
            foreach (StoredItem item in ItemStorage.Instance.Items)
            {
                if (item != null && UnitItemTransfers.CanSwapWithStorage(
                    unit.UnitId, item.InstanceId, selectedSlot, out string reason))
                {
                    anyCompatible = true;
                    break;

                }

            }

        }

        storagePickerPanel.SetActive(true);
        if (!anyCompatible)
        {
            SetStatus("No compatible stored items for " + selectedSlot + ".");

        }
        RefreshStoragePicker();

    }

    private void CloseStoragePicker()
    {
        selectedStoredItemId = null;
        if (storagePickerPanel != null)
        {
            storagePickerPanel.SetActive(false);

        }

        ShowSelectedSlotItem();

    }

    private void RefreshStoragePicker()
    {
        ClearGenerated(generatedStorageRows);
        UnitData unit = SelectedUnit();
        ItemStorage storage = ItemStorage.Instance;
        if (storageContent == null || storageItemRowPrefab == null || storage == null)
        {
            return;

        }

        foreach (StoredItem item in storage.Items)
        {
            if (item == null || (filterPickerBySelectedSlot &&
                (unit == null || !UnitItemTransfers.CanSwapWithStorage(
                    unit.UnitId, item.InstanceId, selectedSlot, out string reason))))
            {
                continue;

            }

            BarracksStoredItemRowUI row = Instantiate(storageItemRowPrefab, storageContent);
            row.Bind(item, SelectStoredItem);
            generatedStorageRows.Add(row.gameObject);

        }

        if (equipSelectedButton != null)
        {
            equipSelectedButton.interactable = CanEdit(unit) &&
                !string.IsNullOrWhiteSpace(selectedStoredItemId) &&
                UnitItemTransfers.CanSwapWithStorage(unit.UnitId,
                    selectedStoredItemId, selectedSlot, out string unusedReason);

        }

    }

    private void SelectStoredItem(string instanceId)
    {
        selectedStoredItemId = instanceId;
        if (ItemStorage.Instance != null &&
            ItemStorage.Instance.TryGetItem(instanceId, out StoredItem stored) && stored != null)
        {
            if (stored.Kind == StoredItemKind.Equipment)
            {
                ShowEquipment(stored.Equipment);

            }
            else
            {
                ShowConsumable(stored.Consumable);

            }

        }

        if (equipSelectedButton != null)
        {
            UnitData unit = SelectedUnit();
            equipSelectedButton.interactable = CanEdit(unit) &&
                UnitItemTransfers.CanSwapWithStorage(unit.UnitId,
                    selectedStoredItemId, selectedSlot, out string unusedReason);

        }

    }

    private void EquipSelectedItem()
    {
        UnitData unit = SelectedUnit();
        if (!CanEdit(unit) || string.IsNullOrEmpty(selectedStoredItemId))
        {
            SetStatus("Select an editable unit and an item in storage.");

            return;

        }

        transferInProgress = true;
        bool success;
        string error;
        try
        {
            success = UnitItemTransfers.TrySwapWithStorage(unit.UnitId,
                selectedStoredItemId, selectedSlot, out error);

        }
        finally
        {
            transferInProgress = false;

        }

        SetStatus(success ? "Equipment changed; exact old instance returned to storage." : "Change rejected: " + error);
        if (success)
        {
            selectedStoredItemId = null;
            if (storagePickerPanel != null)
            {
                storagePickerPanel.SetActive(false);

            }

        }

        RefreshAll();

    }

    private void ReturnSelectedItem()
    {
        UnitData unit = SelectedUnit();
        if (!CanEdit(unit))
        {
            SetStatus("Cannot unequip this unit during combat.");

            return;

        }

        transferInProgress = true;
        bool success;
        string error;
        try
        {
            success = UnitItemTransfers.TryReturnToStorage(unit.UnitId, selectedSlot, out error);

        }
        finally
        {
            transferInProgress = false;

        }

        SetStatus(success ? "Exact item returned to storage." : "Unequip rejected: " + error);
        RefreshAll();

    }

    private void ToggleSlotLock()
    {
        UnitData unit = SelectedUnit();
        if (!CanEdit(unit))
        {
            return;

        }

        UnitLoadoutSlotState state = unit.GetSlot(selectedSlot);
        bool changed = state != null &&
            unit.TrySetPlayerSlotLock(selectedSlot, !state.PlayerLocked);
        SetStatus(changed ? "Slot lock updated." : "Slot lock not changed.");
        RefreshAll();

    }

    private void Update()
    {
        if (!renaming || renameInput != null)
        {
            return;

        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;

        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            CancelRename();
            RefreshDetails();

            return;

        }

        if (keyboard.enterKey.wasPressedThisFrame ||
            keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            CommitRename(renameDraft);

            return;

        }

        if (keyboard.backspaceKey.wasPressedThisFrame && renameDraft.Length > 0)
        {
            renameDraft = renameDraft.Substring(0, renameDraft.Length - 1);
            RefreshRenameDisplay();

        }

    }

    private void OnRenameTyped(char character)
    {
        if (!renaming || renameInput != null || char.IsControl(character) ||
            renameDraft.Length >= 40)
        {
            return;

        }

        renameDraft += character;
        RefreshRenameDisplay();

    }

    private void RefreshRenameDisplay()
    {
        if (unitNameText != null && renaming)
        {
            unitNameText.text = renameDraft + " |";

        }

    }

    private void ApplyRename()
    {
        UnitData unit = SelectedUnit();
        if (!CanEdit(unit))
        {
            return;

        }

        if (!renaming)
        {
            renaming = true;
            renameDraft = unit.characterName;
            if (renameInput != null)
            {
                renameInput.gameObject.SetActive(true);
                renameInput.text = renameDraft;
                renameInput.Select();
                renameInput.ActivateInputField();

            }
            else
            {
                renameKeyboard = Keyboard.current;
                if (renameKeyboard != null)
                {
                    renameKeyboard.onTextInput += OnRenameTyped;

                }

                SetStatus("Type the new unit name. Enter/Save confirms; Escape cancels.");
                RefreshRenameDisplay();

            }

            if (renameButtonLabel != null)
            {
                renameButtonLabel.text = "Save";

            }

            return;

        }

        CommitRename(renameInput != null ? renameInput.text : renameDraft);

    }

    private void CommitRename(string proposed)
    {
        UnitData unit = SelectedUnit();
        if (!CanEdit(unit) || !unit.TryRename(proposed))
        {
            SetStatus("Name must be between 1 and 40 characters.");

            return;

        }

        CancelRename();
        SetStatus("Unit renamed to " + unit.characterName + ".");
        RefreshAll();

    }

    private void CancelRename()
    {
        if (renameKeyboard != null)
        {
            renameKeyboard.onTextInput -= OnRenameTyped;
            renameKeyboard = null;

        }

        if (renameInput != null)
        {
            renameInput.gameObject.SetActive(false);

        }

        renaming = false;
        if (renameButtonLabel != null)
        {
            renameButtonLabel.text = originalRenameButtonLabel;

        }

    }

    private void SetStatus(string message)
    {
        if (statusText != null && statusText != unitStatsText)
        {
            statusText.text = message;

        }
        else
        {
            Debug.Log("[Barracks] " + message, this);

        }

    }

    private static void ClearGenerated(List<GameObject> objects)
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
