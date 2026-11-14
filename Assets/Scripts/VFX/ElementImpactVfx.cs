using MutaBorn.Gameplay;
using UnityEngine;

// Shared area-impact visual. The size argument is the blast radius in world units.
public static class ElementImpactVfx
{
    private static ParticleSystem prefab;

    public static bool Play(Vector3 position, DamageType type, float size = 1f)
    {
        if (prefab == null)
        {
            prefab = Resources.Load<ParticleSystem>("VFX/VFX_ElementImpact");

        }

        if (prefab == null)
        {
            return false;

        }

        ParticleSystem effect = Object.Instantiate(prefab, position, Quaternion.identity);
        effect.transform.localScale = Vector3.one * Mathf.Max(0.1f, size);
        Color color = ElementVfxColors.For(type);
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            main.startColor = color;

        }

        float lifetime = 1f;
        foreach (ElementImpactSphereFade sphere in effect.GetComponentsInChildren<ElementImpactSphereFade>(true))
        {
            sphere.SetColor(color);
            lifetime = Mathf.Max(lifetime, sphere.Duration + 0.15f);

        }

        ParticleSystem.MainModule rootParticles = effect.main;
        rootParticles.stopAction = ParticleSystemStopAction.None;
        effect.Play(true);
        Object.Destroy(effect.gameObject, lifetime);
        return true;

    }
}
