using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class BrigSlotView
{
    public Button button;
    public TMP_Text label;

}

// Scene-authored Hub menu. Reuses BarracksMenu for prisoner details and item inspection.
public sealed class BrigMenu : MonoBehaviour
{
    [Header("Existing Hub UI")]
    [SerializeField] private GameObject brigRoot;
    [SerializeField] private BarracksMenu barracks;
    [SerializeField] private bool usePrisonerRoster;
    [SerializeField] private TMP_Text cellCountText;
    [SerializeField] private BrigSlotView[] slots = new BrigSlotView[BrigManager.MaximumSlots];
    [SerializeField] private Button ransomButton;
    [SerializeField] private Button recruitButton;
    [SerializeField] private Button researchButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text mutationResearchText;

    private int selectedSlot = -1;
    private BrigManager subscribed;

    private void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].button != null)
            {
                int index = i;
                slots[i].button.onClick.AddListener(() => Select(index));

            }

        }

        if (ransomButton != null)
        {
            ransomButton.onClick.AddListener(RansomSelected);

        }
        if (recruitButton != null)
        {
            recruitButton.onClick.AddListener(RecruitSelected);

        }
        if (researchButton != null)
        {
            researchButton.onClick.AddListener(ResearchSelected);

        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);

        }

    }

    private void OnEnable()
    {
        if (usePrisonerRoster && barracks != null)
        {
            barracks.PrisonerSelectionChanged += OnPrisonerCellSelected;

        }
        Subscribe();
        Refresh();

    }

    private void OnDisable()
    {
        if (usePrisonerRoster && barracks != null)
        {
            barracks.PrisonerSelectionChanged -= OnPrisonerCellSelected;

        }
        if (subscribed != null)
        {
            subscribed.Changed -= Refresh;
            subscribed = null;

        }

    }

    private void Subscribe()
    {
        if (subscribed == BrigManager.Instance)
        {
            return;

        }

        if (subscribed != null)
        {
            subscribed.Changed -= Refresh;

        }
        subscribed = BrigManager.Instance;
        if (subscribed != null)
        {
            subscribed.Changed += Refresh;

        }

    }

    public void Open()
    {
        if (brigRoot != null)
        {
            brigRoot.SetActive(true);
            brigRoot.transform.SetAsLastSibling();

        }
        if (barracks != null)
        {
            barracks.Open();

        }
        selectedSlot = -1;
        Subscribe();
        Refresh();

    }

    public void Close()
    {
        if (usePrisonerRoster && brigRoot != null)
        {
            brigRoot.SetActive(false);

            return;

        }
        if (barracks != null)
        {
            barracks.Close();

        }
        if (brigRoot != null)
        {
            brigRoot.SetActive(false);

        }

    }

    public void Select(int slotIndex)
    {
        BrigManager brig = BrigManager.Instance;
        if (brig == null || slotIndex < 0 || slotIndex >= brig.UnlockedSlots ||
            brig.GetPrisoner(slotIndex) == null)
        {
            return;

        }

        if (usePrisonerRoster && barracks != null)
        {
            barracks.ShowPrisoner(brig.GetPrisoner(slotIndex));

        }
        selectedSlot = slotIndex;
        Refresh();

    }

    private UnitData Selected()
    {
        return usePrisonerRoster && barracks != null ? barracks.SelectedPrisoner :
            BrigManager.Instance == null ? null : BrigManager.Instance.GetPrisoner(selectedSlot);

    }

    private void OnPrisonerCellSelected()
    {
        if (statusText != null && barracks != null)
        {
            statusText.text = barracks.PrisonerCellStatus;

        }
        Refresh();

    }

    public void Refresh()
    {
        BrigManager brig = BrigManager.Instance;
        if (usePrisonerRoster && barracks != null)
        {
            barracks.RefreshPrisoners();

        }
        if (cellCountText != null)
        {
            cellCountText.text = brig == null ? "BRIG UNAVAILABLE" :
                $"CELLS {brig.Prisoners.Count}/{brig.UnlockedSlots}  |  " +
                $"{BrigManager.MaximumSlots - brig.UnlockedSlots} LOCKED";

        }
        for (int i = 0; i < slots.Length; i++)
        {
            BrigSlotView view = slots[i];
            if (view == null)
            {
                continue;

            }
            UnitData unit = brig != null ? brig.GetPrisoner(i) : null;
            bool unlocked = brig != null && i < brig.UnlockedSlots;
            if (view.label != null)
            {
                view.label.text = !unlocked ? "LOCKED" : unit == null
                    ? "EMPTY" : $"{unit.characterName} (Lv {unit.Level})";

            }

            if (view.button != null)
            {
                view.button.interactable = unlocked && unit != null;

            }

        }

        UnitData selected = Selected();
        if (!usePrisonerRoster && barracks != null && brigRoot != null && brigRoot.activeSelf)
        {
            barracks.ShowPrisoner(selected);

        }
        if (ransomButton != null)
        {
            ransomButton.interactable = selected != null;

        }
        if (recruitButton != null)
        {
            recruitButton.interactable = selected != null;

        }
        if (researchButton != null)
        {
            researchButton.interactable = selected != null && brig.MutationValue(selected) > 0;

        }
        if (goldText != null)
        {
            goldText.text = selected == null ? "" :
                $"Ransom: {brig.RansomGold(selected)} gold | Recruit offer: {brig.RecruitGold(selected)} gold";

        }
        if (mutationResearchText != null)
        {
            mutationResearchText.text = brig == null ? "" :
                $"Mutation research: {brig.MutationResearchPoints}" +
                (selected == null ? "" : $" (+{brig.MutationValue(selected)} from captive)");

        }

    }

    public void RansomSelected()
    {
        UnitData unit = Selected();
        if (unit == null)
        {
            return;

        }
        bool success = BrigManager.Instance.TryRansom(unit);
        if (success)
        {
            selectedSlot = -1;

        }
        Status(success ? $"Ransomed {unit.characterName}." : "Ransom could not be paid out.");
        Refresh();

    }

    public void RecruitSelected()
    {
        UnitData unit = Selected();
        if (unit == null)
        {
            return;

        }
        bool attempted = BrigManager.Instance.TryRecruit(unit, out bool accepted);
        if (accepted)
        {
            selectedSlot = -1;

        }
        Status(!attempted ? "Not enough gold or roster unavailable." :
            accepted ? $"{unit.characterName} joined the roster!" : "Offer rejected. Gold spent.");
        Refresh();

    }

    public void ResearchSelected()
    {
        UnitData unit = Selected();
        if (unit == null)
        {
            return;

        }
        bool success = BrigManager.Instance.TryConvertToMutationResearch(unit);
        if (success)
        {
            selectedSlot = -1;

        }
        Status(success ? "Captive converted into Mutation Research points." : "Conversion failed.");
        Refresh();

    }

    private void Status(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;

        }
        Debug.Log("[Brig] " + message, this);

    }
}
