using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

using UIButton = UnityEngine.UI.Button;

[RequireComponent(typeof(UIButton))]
public class ActionButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Button References")]
    [SerializeField] private UIButton button;

    [Header("Text References")]
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private TMP_Text apCostText;

    private ActionDefinition boundAction;
    private Action<ActionDefinition> previewStarted;
    private Action<ActionDefinition> previewEnded;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<UIButton>();
    }

    public void Bind(
        ActionDefinition action,
        float availableAP,
        Action<ActionDefinition> onRequested,
        Action<ActionDefinition> onPreviewStarted = null,
        Action<ActionDefinition> onPreviewEnded = null)
    {
        if (button == null)
            button = GetComponent<UIButton>();

        button.onClick.RemoveAllListeners();

        boundAction = action;
        previewStarted = onPreviewStarted;
        previewEnded = onPreviewEnded;

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
                onRequested?.Invoke(action);
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (boundAction != null)
        {
            previewStarted?.Invoke(boundAction);

        }

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        EndPreview();

    }

    public void OnSelect(BaseEventData eventData)
    {
        if (boundAction != null)
        {
            previewStarted?.Invoke(boundAction);

        }

    }

    public void OnDeselect(BaseEventData eventData)
    {
        EndPreview();

    }

    private void OnDisable()
    {
        EndPreview();

    }

    private void EndPreview()
    {
        if (boundAction != null)
        {
            previewEnded?.Invoke(boundAction);

        }

    }

    // Optional per-target context for action buttons generated from nearby objects.
    public void SetContextLabel(string targetName)
    {
        if (boundAction != null && buttonText != null && !string.IsNullOrWhiteSpace(targetName))
        {
            buttonText.text = $"{boundAction.DisplayName} - {targetName}";

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