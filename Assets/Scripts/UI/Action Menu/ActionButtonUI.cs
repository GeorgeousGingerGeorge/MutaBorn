using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



[RequireComponent(typeof(Button))]
public class ActionButtonUI : MonoBehaviour
{
    [Header("Button Parts")]

    [SerializeField] private Button button;
    [SerializeField] private TMP_Text abilityName;
    [SerializeField] private TMP_Text apCost;
    [SerializeField] private Image iconImage;

    private ActionDefinition boundAction;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();

    }

    public void Bind(ActionDefinition action, float availableAP,
        Action<ActionDefinition> onRequested)
    {
        if (button == null) button = GetComponent<Button>();

        button.onClick.RemoveAllListeners();

        boundAction = action;
        if (action == null)
        {
            button.interactable = false;

            return;

        }

        // Display the action's name.
        if (abilityName != null)
        {
            abilityName.text = action.DisplayName;

        }

        // Display the action's AP cost separately.
        if (apCost != null)
        {
            apCost.text = $"{action.ApCost:0.#} AP";

        }

        if (iconImage != null)
        {
            iconImage.enabled = action.Icon != null;
            iconImage.sprite = action.Icon;

        }

        button.interactable = availableAP >= action.ApCost;

        button.onClick.AddListener(() => onRequested?.Invoke(action));

    }

    public void RefreshAvailability(float availableAP)
    {
        if (button != null)
        {
            button.interactable = boundAction != null && availableAP >= boundAction.ApCost;

        }

    }

}
