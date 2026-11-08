using System.Collections;
using UnityEngine;

public class WorldTrapVisual : MonoBehaviour
{
    public enum Motion
    {
        SwingHorizontal,
        SwingVertical,
        Fall,
        Burst

    }

    [SerializeField] private Transform movingVisual;
    [SerializeField] private Motion motion;
    [SerializeField, Min(0.05f)] private float duration = 0.5f;
    [SerializeField] private float angle = 90f;
    [SerializeField] private float fallHeight = 2f;
    [SerializeField, Min(0.1f)] private float burstSize = 6f;

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Vector3 initialScale;
    private Renderer[] movingRenderers;
    private Coroutine animationRoutine;

    private void Awake()
    {
        if (movingVisual != null)
        {
            initialRotation = movingVisual.localRotation;
            initialPosition = movingVisual.localPosition;
            initialScale = movingVisual.localScale;
            movingRenderers = movingVisual.GetComponentsInChildren<Renderer>(true);
            if (motion == Motion.Fall)
            {
                movingVisual.localPosition = initialPosition + Vector3.up * fallHeight;

            }
            else if (motion == Motion.Burst)
            {
                SetVisible(false);

            }

        }

    }

    public void Play()
    {
        if (movingVisual == null)
        {

            return;

        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);

        }

        if (motion == Motion.Fall)
        {
            movingVisual.localPosition = initialPosition + Vector3.up * fallHeight;

        }
        else if (motion == Motion.Burst)
        {
            SetVisible(true);

        }

        animationRoutine = StartCoroutine(Animate());

    }

    private void SetVisible(bool visible)
    {
        if (movingRenderers == null)
        {

            return;

        }

        foreach (Renderer renderer in movingRenderers)
        {
            renderer.enabled = visible;

        }

    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float phase = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
            if (motion == Motion.Fall)
            {
                float drop = Mathf.Clamp01(elapsed / duration);
                movingVisual.localPosition = Vector3.Lerp(initialPosition + Vector3.up * fallHeight,
                    initialPosition, drop * drop);

            }
            else if (motion == Motion.Burst)
            {
                movingVisual.localScale = initialScale * (Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI) * burstSize);

            }
            else
            {
                Vector3 axis = motion == Motion.SwingHorizontal ? Vector3.up : Vector3.right;
                movingVisual.localRotation = initialRotation * Quaternion.AngleAxis(angle * phase, axis);

            }

            yield return null;

        }

        movingVisual.localPosition = initialPosition;
        movingVisual.localRotation = initialRotation;
        movingVisual.localScale = initialScale;
        if (motion == Motion.Burst)
        {
            SetVisible(false);

        }

        animationRoutine = null;

    }

}
