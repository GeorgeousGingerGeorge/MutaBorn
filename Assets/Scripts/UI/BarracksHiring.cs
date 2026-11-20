using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Scene-authored Barracks menu. The UnitRoster remains the owner of hired units.
public sealed class BarracksHiring : MonoBehaviour
{
    [Serializable]
    private sealed class HireOption
    {
        public DynamicStats race;
        public Button button;
        [Min(1)] public int goldCost = 100;

    }

    [SerializeField] private Button openButton;
    [SerializeField] private GameObject hirePanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private HireOption[] options = new HireOption[0];

    private void Awake()
    {
        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);

        foreach (HireOption option in options)
        {
            HireOption selected = option;
            option.button.onClick.AddListener(() => Hire(selected));

        }

    }

    private void OnEnable()
    {
        Close();

    }

    private void OnDisable()
    {
        Close();

    }

    private void Open()
    {
        hirePanel.SetActive(true);
        UpdatePrices();
        int gold = ResourceStockpile.Instance != null ? ResourceStockpile.Instance.GetQuantity("gold") : 0;
        statusText.text = "Gold: " + gold + ". Choose a level 1 soldier.";

    }

    private void Close()
    {
        hirePanel.SetActive(false);

    }

    private void UpdatePrices()
    {
        ResourceStockpile stockpile = ResourceStockpile.Instance;
        int gold = stockpile != null ? stockpile.GetQuantity("gold") : 0;

        foreach (HireOption option in options)
        {
            int cost = PriceOf(option);
            TMP_Text label = option.button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = option.race.RaceName + " Soldier - " + cost + " Gold";

            }

            option.button.interactable = stockpile != null && UnitRoster.Instance != null && gold >= cost;

        }

    }

    private static int PriceOf(HireOption option)
    {
        // Existing Hub scenes did not serialize this new field. Default them to 100 Gold.
        return option.goldCost > 0 ? option.goldCost : 100;

    }

    private void Hire(HireOption option)
    {
        ResourceStockpile stockpile = ResourceStockpile.Instance;
        UnitRoster roster = UnitRoster.Instance;
        if (stockpile == null || roster == null)
        {
            statusText.text = "Hiring unavailable: missing roster or Gold stockpile.";

            return;

        }

        int cost = PriceOf(option);
        if (!stockpile.RemoveResource("gold", cost))
        {
            statusText.text = "Not enough Gold. Need " + cost + ".";
            UpdatePrices();

            return;

        }

        if (!roster.TryCreateNewUnit(option.race.RaceName + " Soldier", option.race, out UnitData unit))
        {
            stockpile.AddResource("gold", cost);
            statusText.text = "Could not hire the soldier. Gold refunded.";
            UpdatePrices();

            return;

        }

        statusText.text = unit.characterName + " hired for " + cost + " Gold. Remaining: " + stockpile.GetQuantity("gold") + ".";
        UpdatePrices();

    }

}
