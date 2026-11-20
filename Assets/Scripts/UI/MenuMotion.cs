using UnityEngine;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public sealed class MenuMotion : MonoBehaviour
{
    public enum TransitionStyle
    {
        Slide,
        Grow

    }

    [SerializeField] private TransitionStyle transition = TransitionStyle.Slide;
    [SerializeField] private Vector2 slideOffset = new Vector2(0f, -65f);
    [SerializeField, Range(0.5f, 1f)] private float startScale = 0.86f;
    [SerializeField, Min(0.01f)] private float duration = 0.22f;

    private RectTransform rect;
    private CanvasGroup group;
    private Vector2 restingPosition;
    private Vector3 restingScale;
    private float restingAlpha;
    private bool restingInteractable;
    private bool restingRaycasts;
    private float elapsed;
    private float visibility;
    private float fromVisibility;
    private float targetVisibility;
    private bool animating;

    public bool IsClosing => animating && targetVisibility == 0f;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
        restingPosition = rect.anchoredPosition;
        restingScale = rect.localScale;
        restingAlpha = group.alpha;
        restingInteractable = group.interactable;
        restingRaycasts = group.blocksRaycasts;

    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;

        }

        visibility = 0f;
        ApplyVisibility();
        AnimateTo(1f);

    }

    private void Update()
    {
        if (!animating)
        {
            return;

        }

        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        visibility = Mathf.Lerp(fromVisibility, targetVisibility, eased);
        ApplyVisibility();

        if (t < 1f)
        {
            return;

        }

        animating = false;
        if (targetVisibility == 0f)
        {
            gameObject.SetActive(false);

            return;

        }

        group.interactable = restingInteractable;
        group.blocksRaycasts = restingRaycasts;

    }

    private void AnimateTo(float target)
    {
        fromVisibility = visibility;
        targetVisibility = target;
        elapsed = 0f;
        animating = true;
        group.interactable = false;
        group.blocksRaycasts = false;

    }

    private void ApplyVisibility()
    {
        rect.anchoredPosition = transition == TransitionStyle.Slide
            ? restingPosition + slideOffset * (1f - visibility)
            : restingPosition;
        rect.localScale = transition == TransitionStyle.Grow
            ? restingScale * Mathf.Lerp(startScale, 1f, visibility)
            : restingScale;
        group.alpha = restingAlpha * visibility;

    }

    private void OnDisable()
    {
        if (rect == null || group == null)
        {
            return;

        }

        animating = false;
        visibility = 1f;
        rect.anchoredPosition = restingPosition;
        rect.localScale = restingScale;
        group.alpha = restingAlpha;
        group.interactable = restingInteractable;
        group.blocksRaycasts = restingRaycasts;

    }

    public void Open()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);

            return;

        }

        if (IsClosing)
        {
            AnimateTo(1f);

        }

    }

    public void Close()
    {
        if (!isActiveAndEnabled)
        {
            gameObject.SetActive(false);

            return;

        }

        AnimateTo(0f);

    }

    public static void Show(GameObject panel)
    {
        if (panel == null)
        {
            return;

        }

        MenuMotion motion = panel.GetComponent<MenuMotion>();
        if (motion != null)
        {
            motion.Open();

            return;

        }

        panel.SetActive(true);

    }

    public static void Hide(GameObject panel)
    {
        if (panel == null)
        {
            return;

        }

        MenuMotion motion = panel.GetComponent<MenuMotion>();
        if (motion != null && panel.activeInHierarchy)
        {
            motion.Close();

            return;

        }

        panel.SetActive(false);

    }
}
