using MutaBorn.Gameplay;
using UnityEngine;

// Prefabs are authored in Unity; this component only triggers/tints them.
public sealed class WorldTrapVfx : MonoBehaviour
{
    [SerializeField] private ParticleSystem firePrefab;
    [SerializeField] private ParticleSystem hitPrefab;
    [SerializeField] private Vector3 fireOffset = new Vector3(0f, 0.65f, 0f);

    public void PlayFire(DamageType type)
    {
        Spawn(firePrefab, transform.position + fireOffset, ElementVfxColors.For(type));

    }

    public void PlayHit(Vector3 position, DamageType type)
    {
        Spawn(hitPrefab, position, ElementVfxColors.For(type));

    }

    public void PlayDisarm()
    {
        Spawn(firePrefab, transform.position + fireOffset, Color.gray);

    }

    public static void Spawn(ParticleSystem prefab, Vector3 position, Color color)
    {
        if (prefab == null)
        {
            return;

        }

        ParticleSystem particles = Instantiate(prefab, position, Quaternion.identity);
        ParticleSystem.MainModule main = particles.main;
        main.startColor = color;
        main.stopAction = ParticleSystemStopAction.Destroy;
        particles.Play(true);

    }
}
