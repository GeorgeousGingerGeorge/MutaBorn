using UnityEngine;

public sealed class ProjectileSpinVisual : MonoBehaviour
{
    [SerializeField] private Vector3 localAxis = new Vector3(0f, 0f, 1f);
    [SerializeField] private float degreesPerSecond = 720f;

    private void Update()
    {
        transform.Rotate(localAxis, degreesPerSecond * Time.deltaTime, Space.Self);

    }
}
