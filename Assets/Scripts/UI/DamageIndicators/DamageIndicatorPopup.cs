using UnityEngine;

namespace MutaBorn.Gameplay
{
    // A tiny non-blocking animation: hold, accelerate away, fade and self-destruct.
    public sealed class DamageIndicatorPopup : MonoBehaviour
    {
        private RectTransform rect;
        private CanvasGroup group;
        private Vector2 direction;
        private Vector2 origin;
        private float lifetime;
        private float holdDuration;
        private float driftSpeed;
        private float acceleration;
        private float age;

        public void Initialize(Vector2 driftDirection, float duration, float hold, float speed, float accelerationRate)
        {
            rect = GetComponent<RectTransform>();
            group = GetComponent<CanvasGroup>();
            direction = driftDirection.normalized;
            origin = rect.anchoredPosition;
            lifetime = Mathf.Max(0.01f, duration);
            holdDuration = Mathf.Clamp(hold, 0f, lifetime);
            driftSpeed = Mathf.Max(0f, speed);
            acceleration = Mathf.Max(0f, accelerationRate);
            age = 0f;

        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);

                return;

            }

            if (age <= holdDuration)
            {
                rect.anchoredPosition = origin;
                group.alpha = 1f;
                float pop = Mathf.Clamp01(age / Mathf.Max(0.001f, holdDuration));
                rect.localScale = Vector3.one * Mathf.Lerp(0.86f, 1.08f, pop);

                return;

            }

            float travelTime = age - holdDuration;
            float distance = driftSpeed * travelTime + 0.5f * acceleration * travelTime * travelTime;
            rect.anchoredPosition = origin + direction * distance;
            float fadeProgress = travelTime / Mathf.Max(0.001f, lifetime - holdDuration);
            group.alpha = 1f - Mathf.SmoothStep(0f, 1f, fadeProgress);
            rect.localScale = Vector3.one * Mathf.Lerp(1.08f, 0.94f, fadeProgress);

        }

    }

}
