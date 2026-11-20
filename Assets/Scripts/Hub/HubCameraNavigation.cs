
using System.Collections;
using UnityEngine;

public class HubCameraNavigation : MonoBehaviour
{
    [Header("Camera Transition")]
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private float swoopHeight = 0.5f;

    private Coroutine activeTransition;

    public void MoveTo(Transform destination)
    {
        if (destination == null)
        {
            return;
        }

        if (activeTransition != null)
        {
            StopCoroutine(activeTransition);
        }

        activeTransition = StartCoroutine(TransitionTo(destination));
    }

    private IEnumerator TransitionTo(Transform destination)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 targetPosition = destination.position;
        Quaternion targetRotation = destination.rotation;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, transitionDuration));
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);
            position.y += Mathf.Sin(t * Mathf.PI) * swoopHeight;

            transform.position = position;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        transform.SetPositionAndRotation(targetPosition, targetRotation);
        activeTransition = null;
    }
}
