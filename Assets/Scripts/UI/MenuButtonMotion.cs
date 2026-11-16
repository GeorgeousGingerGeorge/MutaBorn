using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public sealed class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField, Range(1f, 1.25f)] private float hoverScale = 1.07f;
    [SerializeField, Range(0.8f, 1f)] private float pressedScale = 0.96f;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.10f;

    [SerializeField] private Color hoverTextColor = new Color(0.98f, 0.84f, 0.61f, 1f);

    private Button button;
    private TMP_Text label;
    private Color originalTextColor;
    private Vector3 originalScale;
    private bool hovered;
    private bool pressed;

    private void Awake()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            originalTextColor = label.color;

        }
        originalScale = transform.localScale;

    }

    private void Update()
    {
        float multiplier = !button.interactable ? 1f : pressed ? pressedScale : hovered ? hoverScale : 1f;
        float t = 1f - Mathf.Exp(-Time.unscaledDeltaTime / smoothTime);
        transform.localScale = Vector3.Lerp(transform.localScale, originalScale * multiplier, t);
        if (label != null)
        {
            label.color = Color.Lerp(label.color,
                hovered && button.interactable ? hoverTextColor : originalTextColor, t);

        }

    }

    private void OnDisable()
    {
        hovered = false;
        pressed = false;
        if (button != null)
        {
            transform.localScale = originalScale;
            if (label != null)
            {
                label.color = originalTextColor;

            }

        }

    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData eventData) { pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; }
    public void OnSelect(BaseEventData eventData) { hovered = true; }
    public void OnDeselect(BaseEventData eventData) { hovered = false; pressed = false; }

}
