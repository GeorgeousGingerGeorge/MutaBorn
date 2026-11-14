using MutaBorn.Gameplay;
using UnityEngine;

// Works on any projectile prefab that has these optional visual references.
public sealed class ProjectileElementVfx : MonoBehaviour
{
    [SerializeField] private Renderer orbRenderer;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private ParticleSystem flightParticles;
    [SerializeField] private ParticleSystem impactPrefab;
    private Color color = Color.white;
    private DamageType damageType = DamageType.Unspecified;
    private float impactSize = 0.7f;
    private bool areaImpact;

    public void Configure(DamageType type)
    {
        damageType = type;
        color = ElementVfxColors.For(type);
        if (orbRenderer != null)
        {
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            orbRenderer.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
            orbRenderer.SetPropertyBlock(properties);

        }

        if (trail != null)
        {
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);

        }

        if (flightParticles != null)
        {
            ParticleSystem.MainModule main = flightParticles.main;
            main.startColor = color;

        }

    }


    public void SetImpactSize(float size)
    {
        impactSize = Mathf.Max(0.1f, size);

    }

    public void SetAreaImpact(bool enabled)
    {
        areaImpact = enabled;

    }

    public void PlayImpact(Vector3 position)
    {
        if (!areaImpact)
        {
            WorldTrapVfx.Spawn(impactPrefab, position, color);

        }

    }
}
