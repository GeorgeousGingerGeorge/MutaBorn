using TMPro;
using UnityEngine;
using UnityEngine.UI;


[DisallowMultipleComponent]
public class UnitHealthBarUI : MonoBehaviour
{
    [Header("Placement")]
    [Tooltip("World-space height above the unit's origin.")]
    [SerializeField, Min(0f)] private float heightAboveUnit = 1.75f;

    [Tooltip("World-space size of the health bar UI.")]
    [SerializeField, Min(0.001f)] private float canvasScale = 0.005f;

    [Header("Display")]
    [SerializeField] private bool showHealthNumbers = true;
    [SerializeField] private bool hideAtFullHealth = false;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.9f);
    [SerializeField] private Color healthyColor = new Color(0.18f, 0.8f, 0.33f, 1f);
    [SerializeField] private Color woundedColor = new Color(0.98f, 0.76f, 0.18f, 1f);
    [SerializeField] private Color criticalColor = new Color(0.93f, 0.25f, 0.22f, 1f);

    private UnitController unit;
    private RectTransform barRoot;
    private Image healthFill;
    private RectTransform healthFillRect;
    private TMP_Text healthText;
    private Camera targetCamera;

    private int lastHealth = -1;
    private int lastMaximumHealth = -1;


    private void Awake()
    {
        unit = GetComponent<UnitController>();

        if (unit == null)
        {
            unit = GetComponentInParent<UnitController>();

        }

        if (unit == null)
        {
            Debug.LogWarning("UnitHealthBarUI needs a UnitController on this object or one of its parents.", this);
            enabled = false;

            return;

        }

        CreateBar();

    }

    private void Update()
    {
        if (barRoot == null)
        {
            return;

        }

        UnitData data = unit != null ? unit.data : null;

        if (data == null || data.maxHealth <= 0 || data.currentHealth <= 0)
        {
            SetBarVisible(false);

            return;

        }

        bool fullHealth = data.currentHealth >= data.maxHealth;
        SetBarVisible(!hideAtFullHealth || !fullHealth);

        if (data.currentHealth != lastHealth || data.maxHealth != lastMaximumHealth)
        {
            RefreshHealth(data.currentHealth, data.maxHealth);

        }

    }

    private void LateUpdate()
    {
        if (barRoot == null || !barRoot.gameObject.activeSelf)
        {
            return;

        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;

        }

        barRoot.position = unit.transform.position + Vector3.up * heightAboveUnit;
        barRoot.localScale = Vector3.one * canvasScale;

        if (targetCamera != null)
        {
            barRoot.rotation = targetCamera.transform.rotation;

        }

    }

    private void CreateBar()
    {
        GameObject canvasObject = new GameObject("Floating Health Bar", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(unit.transform, false);

        barRoot = canvasObject.GetComponent<RectTransform>();
        barRoot.sizeDelta = new Vector2(204f, 36f);
        barRoot.localScale = Vector3.one * canvasScale;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(barRoot, false);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(200f, 30f);

        Image background = backgroundObject.GetComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;

        GameObject fillObject = new GameObject("Health Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(backgroundRect, false);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(2f, 0f);
        fillRect.sizeDelta = new Vector2(196f, 26f);
        healthFillRect = fillRect;

        healthFill = fillObject.GetComponent<Image>();
        healthFill.color = healthyColor;
        healthFill.raycastTarget = false;

        if (showHealthNumbers)
        {
            GameObject textObject = new GameObject("Health Numbers", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(backgroundRect, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.raycastTarget = false;

            healthText = label;

        }

        SetBarVisible(false);

    }

    private void RefreshHealth(int current, int maximum)
    {
        float percent = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0f;

        healthFillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 196f * percent);
        healthFill.color = percent > 0.5f ? healthyColor :
            percent > 0.25f ? woundedColor : criticalColor;

        if (healthText != null)
        {
            healthText.text = $"{current} / {maximum}";

        }

        lastHealth = current;
        lastMaximumHealth = maximum;

    }

    private void SetBarVisible(bool visible)
    {
        if (barRoot.gameObject.activeSelf != visible)
        {
            barRoot.gameObject.SetActive(visible);

        }

    }

}
