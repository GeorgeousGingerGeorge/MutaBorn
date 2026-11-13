using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttackPreviewUI : MonoBehaviour
{
    [Header("Groups")]
    [SerializeField] private GameObject detailsGroup;
    [SerializeField] private GameObject messageGroup;
    [SerializeField] private TMP_Text messageText;

    [Header("Target")]
    [SerializeField] private TMP_Text targetNameText;
    [SerializeField] private TMP_Text targetStatsText;

    [Header("Attack")]
    [SerializeField] private TMP_Text actionNameText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private GameObject multiHitRow;
    [SerializeField] private TMP_Text multiHitText;

    [Header("Hit Chances")]
    [SerializeField] private TMP_Text hitChanceText;
    [SerializeField] private TMP_Text dodgeChanceText;
    [SerializeField] private TMP_Text landChanceText;

    [Header("Optional Rows")]
    [SerializeField] private GameObject criticalRow;
    [SerializeField] private TMP_Text criticalChanceText;
    [SerializeField] private TMP_Text criticalDamageText;
    [SerializeField] private GameObject onHitRow;
    [SerializeField] private TMP_Text onHitText;
    [SerializeField] private GameObject guardRow;
    [SerializeField] private TMP_Text guardText;

    public struct PreviewData
    {
        public string TargetName;
        public string TargetStats;
        public string ActionName;
        public int Damage;
        public int HitCount;
        public float HitChance;
        public float DodgeChance;
        public float LandChance;
        public float CriticalChance;
        public int CriticalDamage;
        public string OnHit;
        public string Guard;

    }

    private void Awake()
    {
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;

        }

    }

    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);

        }

    }

    public void ShowMessage(string message)
    {
        gameObject.SetActive(true);
        SetVisible(detailsGroup, false);
        SetVisible(messageGroup, true);
        SetText(messageText, message);

    }

    public void ShowDetails(PreviewData data)
    {
        gameObject.SetActive(true);
        SetVisible(messageGroup, false);
        SetVisible(detailsGroup, true);

        SetText(targetNameText, data.TargetName);
        SetText(targetStatsText, data.TargetStats);
        SetText(actionNameText, data.ActionName);
        SetText(damageText, $"{data.Damage}");
        SetText(hitChanceText, $"{data.HitChance:0.#}%");
        SetText(dodgeChanceText, $"{data.DodgeChance:0.#}%");
        SetText(landChanceText, $"{data.LandChance:0.#}%");

        SetVisible(multiHitRow, data.HitCount > 1);
        SetText(multiHitText, $"x{data.HitCount} hits");

        SetVisible(criticalRow, data.CriticalChance > 0f);
        SetText(criticalChanceText, $"{data.CriticalChance:0.#}%");
        SetText(criticalDamageText, $"{data.CriticalDamage}");

        SetVisible(onHitRow, !string.IsNullOrEmpty(data.OnHit));
        SetText(onHitText, data.OnHit);

        SetVisible(guardRow, !string.IsNullOrEmpty(data.Guard));
        SetText(guardText, data.Guard);

    }

    private static void SetVisible(GameObject element, bool visible)
    {
        if (element != null && element.activeSelf != visible)
        {
            element.SetActive(visible);

        }

    }

    private static void SetText(TMP_Text field, string value)
    {
        if (field != null && field.text != value)
        {
            field.text = value;

        }

    }

}
