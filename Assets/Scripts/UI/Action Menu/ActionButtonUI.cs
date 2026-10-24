using System;
using TMPro;
using UnityEngine;

using UIButton = UnityEngine.UI.Button;

[RequireComponent(typeof(UIButton))]
public class ActionButtonUI : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private UIButton button;

    [Header("Text References")]
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private TMP_Text apCostText;

    private ActionDefinition boundAction;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<UIButton>();
    }

    public void Bind(
        ActionDefinition action,
        float availableAP,
        Action<ActionDefinition> onRequested)
    {
        if (button == null)
            button = GetComponent<UIButton>();

        button.onClick.RemoveAllListeners();

        boundAction = action;

        // Handle missing definitions.
        if (action == null)
        {
            button.interactable = false;

            if (buttonText != null)
                buttonText.text = "Missing Action";

            if (apCostText != null)
                apCostText.text = "";

            return;

        }

        // Display action name.
        if (buttonText != null)
            buttonText.text = action.DisplayName;

        // Display AP cost or Passive label.
        if (apCostText != null)
        {
            apCostText.text = action.IsPassive
                ? "PASSIVE"
                : $"{action.ApCost:0.#} AP";
        }

        // Determine whether the button is available.
        RefreshAvailability(availableAP);

        // Passive abilities display information.
        if (action.IsPassive)
        {
            button.onClick.AddListener(() =>
            {
                Debug.Log(
                    $"Passive: {action.DisplayName}\n" +
                    $"{action.Description}",
                    this
                );
            });
        }
        else
        {
            // Active abilities request execution.
            button.onClick.AddListener(() =>
            {
                onRequested?.Invoke(action);
            });
        }
    }

    public void RefreshAvailability(float availableAP)
    {
        if (button == null)
            return;

        if (boundAction == null)
        {
            button.interactable = false;
            return;
        }

        // Passives remain inspectable regardless of AP.
        if (boundAction.IsPassive)
        {
            button.interactable = true;
            return;
        }

        // Active actions require sufficient AP.
        button.interactable =
            availableAP >= boundAction.ApCost;

    }

}