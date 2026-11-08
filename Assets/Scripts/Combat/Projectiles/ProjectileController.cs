using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    private ProjectileDefinition definition;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float travelDuration;
    private float progress;

    private bool isMoving;

    public void Launch(ProjectileDefinition projectile, Vector3 destination)
    {
        if (projectile == null)
        {
            Debug.LogError("Projectile definition is missing.");
            return;
        }

        definition = projectile;

        startPosition = transform.position;
        targetPosition = destination;

        float distance = Vector3.Distance(startPosition, targetPosition);

        travelDuration = Mathf.Max(0.001f, distance / definition.MovementSpeed);

        progress = 0f;
        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving)
        {
            return;
        }

        progress += Time.deltaTime / travelDuration;
        progress = Mathf.Clamp01(progress);

        Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, progress);

        if (definition.TrajectoryType == ProjectileTrajectoryType.Arc)
        {
            nextPosition.y += Mathf.Sin(progress * Mathf.PI) * definition.ArcHeight;
        }

        Vector3 direction = nextPosition - transform.position;

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        transform.position = nextPosition;

        if (progress >= 1f)
        {
            isMoving = false;
            Destroy(gameObject);
        }
    }
}