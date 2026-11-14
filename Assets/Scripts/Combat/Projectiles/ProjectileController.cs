using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    private ProjectileDefinition definition;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float travelDuration;
    private float progress;

    private bool isMoving;
    private System.Action<Collider, Vector3> onImpact;
    private Transform ignoredRoot;
    private bool arcOverUnits;
    private Transform intendedTarget;

    public void Launch(ProjectileDefinition projectile, Vector3 destination, System.Action<Collider, Vector3> impactCallback = null, Transform ignoreRoot = null, bool arcOverUnits = false, Transform intendedTarget = null)
    {
        if (projectile == null)
        {
            Debug.LogError("Projectile definition is missing.");
            return;
        }

        definition = projectile;
        onImpact = impactCallback;
        ignoredRoot = ignoreRoot;
        this.arcOverUnits = arcOverUnits;
        this.intendedTarget = intendedTarget;

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

        if (arcOverUnits || definition.TrajectoryType == ProjectileTrajectoryType.Arc)
        {
            float horizontalDistance = Vector2.Distance(
                new Vector2(startPosition.x, startPosition.z),
                new Vector2(targetPosition.x, targetPosition.z));
            float height = arcOverUnits ? Mathf.Max(1.8f, definition.ArcHeight, horizontalDistance * 0.4f) : definition.ArcHeight;
            nextPosition.y += Mathf.Sin(progress * Mathf.PI) * height;
        }

        Vector3 direction = nextPosition - transform.position;

        if (direction.sqrMagnitude > 0.0001f)
        {
            float distance = direction.magnitude;

            // Check for collisions along the projectile's movement path.
            RaycastHit[] hits = Physics.RaycastAll(transform.position, direction.normalized, distance, definition.CollisionLayers, QueryTriggerInteraction.Ignore);

            Collider closestCollider = null;
            Vector3 closestPoint = Vector3.zero;
            float closestDistance = float.PositiveInfinity;

            foreach (RaycastHit hit in hits)
            {
                // Ignore the attacking unit and its children.
                if (ignoredRoot != null && (hit.collider.transform == ignoredRoot || hit.collider.transform.IsChildOf(ignoredRoot)))
                {
                    continue;
                }

                // Player attacks fly over non-target units; trap projectiles can still hit them.
                if (arcOverUnits)
                {
                    UnitController hitUnit = hit.collider.GetComponentInParent<UnitController>();
                    if (hitUnit != null && (intendedTarget == null || hitUnit.transform != intendedTarget))
                    {
                        continue;

                    }

                }

                // Find the closest valid collision.
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestCollider = hit.collider;
                    closestPoint = hit.point;
                }
            }

            if (closestCollider != null)
            {
                transform.position = closestPoint;

                Debug.Log($"Projectile hit {closestCollider.name}!");

                isMoving = false;

                GetComponent<ProjectileElementVfx>()?.PlayImpact(closestPoint);
                onImpact?.Invoke(closestCollider, closestPoint);

                Destroy(gameObject);

                return;
            }

            transform.rotation = Quaternion.LookRotation(direction);
        }

        transform.position = nextPosition;

        if (progress >= 1f)
        {
            isMoving = false;

            GetComponent<ProjectileElementVfx>()?.PlayImpact(targetPosition);
            onImpact?.Invoke(null, targetPosition);

            Destroy(gameObject);
        }
    }
}