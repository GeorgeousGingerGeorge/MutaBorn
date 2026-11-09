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

    public static bool IsOpen => instance != null && instance.window != null && instance.window.activeSelf;

    private void Awake()
    {
        instance = this;
        closeButton.onClick.AddListener(Close);
        window.SetActive(false);

    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;

        }

    }

    public static void Show(string title, IReadOnlyList<WorldLootStack> rewards, string emptyMessage)
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

        instance.Open(title, rewards, emptyMessage);

    }

    private void Open(string title, IReadOnlyList<WorldLootStack> rewards, string emptyMessage)
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
        window.SetActive(true);
        scrollRect.verticalNormalizedPosition = 1f;

    }

    public void Close()
    {
        window.SetActive(false);

    }

}
