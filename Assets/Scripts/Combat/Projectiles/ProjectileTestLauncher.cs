using UnityEngine;

public class ProjectileTestLauncher : MonoBehaviour
{
    [SerializeField] private ProjectileDefinition projectileDefinition;
    [SerializeField] private float testDistance = 20f;

    [ContextMenu("Test / Launch Projectile")]
    private void TestLaunchProjectile()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Projectile testing requires Play Mode.");
            return;
        }

        if (projectileDefinition == null || projectileDefinition.ProjectilePrefab == null)
        {
            Debug.LogError("Projectile Definition or Prefab is missing.");
            return;
        }

        GameObject projectile = Instantiate(
            projectileDefinition.ProjectilePrefab,
            transform.position,
            transform.rotation
        );

        ProjectileController controller = projectile.GetComponent<ProjectileController>();

        if (controller == null)
        {
            Debug.LogError("ProjectileController is missing from the prefab.");
            Destroy(projectile);
            return;
        }

        Vector3 destination = transform.position + transform.forward * testDistance;

        controller.Launch(projectileDefinition, destination);

        Debug.Log("Projectile launched!");
    }
}