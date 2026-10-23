using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the current round/faction and a temporary announcement when a turn starts.
/// Reads from TurnManager events; faction names and colors come from FactionData.
/// </summary>
public class TurnIndicatorUI : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private TurnManager turnManager;

    [Header("Persistent HUD")]
    [SerializeField] private TMP_Text roundLabel;
    [SerializeField] private TMP_Text currentFactionLabel;
    [SerializeField] private Image currentFactionColor;

    [Header("Turn Announcement Popup")]
    [SerializeField] private CanvasGroup popupGroup;
    [SerializeField] private TMP_Text popupRoundLabel;
    [SerializeField] private TMP_Text popupFactionLabel;
    [SerializeField] private Image popupFactionColor;
    [SerializeField, Min(0.1f)] private float popupDurationSeconds = 1.1f;

    private TurnManager subscribedManager;
    private Coroutine popupCoroutine;

    private void OnEnable()
    {
        if (popupGroup != null)
            popupGroup.alpha = 0f;
        Subscribe();
        RefreshHUD();
    }

    private void Start()
    {
        // Also bind here in case this UI enabled before TurnManager.Awake().
        Subscribe();
        RefreshHUD();
        if (subscribedManager != null && subscribedManager.ActiveFaction != null && popupCoroutine == null)
            ShowPopup(subscribedManager.ActiveFaction);
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }
        if (popupGroup != null)
            popupGroup.alpha = 0f;
    }

    private void Subscribe()
    {
        if (turnManager == null)
            turnManager = TurnManager.Instance != null
                ? TurnManager.Instance
                : FindFirstObjectByType<TurnManager>();

        if (subscribedManager == turnManager)
            return;

        Unsubscribe();
        if (turnManager == null)
            return;

        subscribedManager = turnManager;
        subscribedManager.TurnStarted += OnTurnStarted;
        subscribedManager.RoundStarted += OnRoundStarted;
        subscribedManager.TurnEnded += OnTurnEnded;
    }

    private void Unsubscribe()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.TurnStarted -= OnTurnStarted;
        subscribedManager.RoundStarted -= OnRoundStarted;
        subscribedManager.TurnEnded -= OnTurnEnded;
        subscribedManager = null;
    }

    private void OnRoundStarted(int round) => RefreshHUD();

    private void OnTurnStarted(FactionData faction)
    {
        RefreshHUD();
        ShowPopup(faction);
    }

    private void OnTurnEnded(FactionData faction) => RefreshHUD();

    private void RefreshHUD()
    {
        if (subscribedManager == null)
            return;

        if (roundLabel != null)
            roundLabel.text = subscribedManager.Round > 0
                ? $"Round {subscribedManager.Round}"
                : "Round —";

        FactionData active = subscribedManager.ActiveFaction;
        if (currentFactionLabel != null)
            currentFactionLabel.text = active != null ? active.DisplayName : "Waiting for turn";
        if (currentFactionColor != null)
            currentFactionColor.color = active != null ? active.Color : Color.gray;
    }

    private void ShowPopup(FactionData faction)
    {
        if (popupGroup == null || faction == null)
            return;

        if (popupCoroutine != null)
            StopCoroutine(popupCoroutine);

        if (popupRoundLabel != null)
            popupRoundLabel.text = $"ROUND {subscribedManager.Round}";
        if (popupFactionLabel != null)
            popupFactionLabel.text = $"{faction.DisplayName}'s Turn";
        if (popupFactionColor != null)
            popupFactionColor.color = faction.Color;

        popupGroup.alpha = 1f;
        popupGroup.interactable = false;
        popupGroup.blocksRaycasts = false;
        popupCoroutine = StartCoroutine(HidePopupAfterDelay());
    }

    private IEnumerator HidePopupAfterDelay()
    {
        yield return new WaitForSecondsRealtime(popupDurationSeconds);
        if (popupGroup != null)
            popupGroup.alpha = 0f;
        popupCoroutine = null;
    }
}
