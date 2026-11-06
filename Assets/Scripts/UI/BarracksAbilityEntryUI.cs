using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A reusable visual-only ability entry. Never executes an ActionDefinition.
public sealed class BarracksAbilityEntryUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text actionNameText;
    [SerializeField] private TMP_Text apCostText;
    [SerializeField] private Image actionIcon;

    public void Bind(ActionDefinition action)
    {
        if (actionNameText != null)
        {
            actionNameText.text = action == null ? "Missing Action" : action.DisplayName;

        }

        if (apCostText != null)
        {
            apCostText.text = action == null ? "" :
                action.IsPassive ? "PASSIVE" : action.ApCost.ToString("0.#") + " AP";

        }

        if (actionIcon != null)
        {
            actionIcon.sprite = action == null ? null : action.Icon;
            actionIcon.enabled = actionIcon.sprite != null;
            actionIcon.raycastTarget = false;

        }

        // If the assigned prefab was based on a combat action button, turn off
        // its behavior as well as the Button's input.
        ActionButtonUI combatBinder = GetComponent<ActionButtonUI>();
        if (combatBinder != null)
        {
            combatBinder.enabled = false;

        }

        if (button == null)
        {
            button = GetComponent<Button>();

        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.transition = Selectable.Transition.None;
            button.interactable = false;

        }

    }
}
