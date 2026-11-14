using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The prefab is created once through Tools > MutaBorn > Missions > Create Loot Popup Prefab.
public sealed class MissionLootPopup : MonoBehaviour
{
    private static MissionLootPopup instance;

    [SerializeField] private GameObject window;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform resultsPanel;

    private Vector3 resultsScale;
    private Coroutine revealRoutine;

    public static bool IsOpen => instance != null && instance.window != null && instance.window.activeSelf;

    private void Awake()
    {
        instance = this;
        closeButton.onClick.AddListener(Close);
        if (resultsPanel == null)
        {
            resultsPanel = window.transform.Find("Loot Results") as RectTransform;

        }

        resultsScale = resultsPanel != null ? resultsPanel.localScale : Vector3.one;
        if (resultsPanel == null)
        {
            Debug.LogWarning("[MutaBorn Loot V2] Cannot find Loot Results panel; pop animation is disabled.", this);

        }

        window.SetActive(false);

    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;

        }

    }

    public static void Show(string title, IReadOnlyList<WorldLootStack> rewards, string emptyMessage, float delaySeconds = 0f)
    {
        if (instance == null)
        {
            MissionLootPopup prefab = Resources.Load<MissionLootPopup>("UI/Mission Loot Popup");
            if (prefab == null)
            {
                Debug.LogWarning("Mission Loot Popup prefab is missing. Use Tools > MutaBorn > Missions > Create Loot Popup Prefab.");

                return;

            }

            Instantiate(prefab);

        }

        instance.Open(title, rewards, emptyMessage, delaySeconds);

    }

    private void Open(string title, IReadOnlyList<WorldLootStack> rewards, string emptyMessage, float delaySeconds)
    {
        titleText.text = title;
        StringBuilder description = new StringBuilder();
        if (rewards != null)
        {
            foreach (WorldLootStack stack in rewards)
            {
                if (stack == null || stack.quantity <= 0)
                {
                    continue;

                }

                string displayName = stack.DisplayName;
                if (stack.kind == WorldLootKind.Resource && ResourceStockpile.Instance != null)
                {
                    ResourceData definition = ResourceStockpile.Instance.GetResourceData(stack.resourceId);
                    if (definition != null)
                    {
                        displayName = definition.ResourceName;

                    }

                }

                description.AppendLine($"+{stack.quantity} {displayName}");

            }

        }

        bodyText.text = description.Length > 0 ? description.ToString().TrimEnd() : emptyMessage;
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);

        }

        window.SetActive(false);
        Debug.Log($"[MutaBorn Loot V2] Opening '{title}' (panel: {(resultsPanel != null ? resultsPanel.name : "MISSING")})", this);
        revealRoutine = StartCoroutine(Reveal(delaySeconds));

    }

    private IEnumerator Reveal(float delaySeconds)
    {
        if (delaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);

        }

        if (resultsPanel != null)
        {
            resultsPanel.localScale = resultsScale * 0.1f;

        }

        window.SetActive(true);
        scrollRect.verticalNormalizedPosition = 1f;
        if (resultsPanel != null)
        {
            yield return AnimateScale(0.1f, 1.1f, 0.28f);
            yield return AnimateScale(1.1f, 1f, 0.2f);

        }

        revealRoutine = null;

    }

    private IEnumerator AnimateScale(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float fraction = Mathf.Clamp01(elapsed / duration);
            fraction = fraction * fraction * (3f - 2f * fraction);
            resultsPanel.localScale = resultsScale * Mathf.Lerp(from, to, fraction);
            yield return null;

        }

        resultsPanel.localScale = resultsScale * to;

    }

    public void Close()
    {
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;

        }

        window.SetActive(false);
        if (resultsPanel != null)
        {
            resultsPanel.localScale = resultsScale;

        }

    }

}
