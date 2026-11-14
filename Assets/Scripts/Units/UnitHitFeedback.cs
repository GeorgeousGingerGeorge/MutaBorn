using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class UnitHitFeedback : MonoBehaviour
{
    private sealed class Surface
    {
        public Renderer Renderer;
        public int Index;
        public MaterialPropertyBlock Original;
        public MaterialPropertyBlock Flash;
        public bool BaseColor;
        public bool Color;
        public bool PrimaryColor;
        public bool SecondaryColor;
        public bool AccentColor;

    }

    private static readonly Color[] HitColors =
    {
        new Color(1f, 0.92f, 0.12f),
        Color.white,
        new Color(1f, 0.45f, 0.65f),
        new Color(0.45f, 0.025f, 0.055f),
        new Color(1f, 0.45f, 0.65f),
        Color.white
    };

    private readonly List<Surface> surfaces = new List<Surface>();
    private Coroutine flashRoutine;
    private static Material bloodMaterial;

    public static void Show(UnitController unit, UnitController attacker)
    {
        if (unit == null || !unit.gameObject.activeInHierarchy || unit.fogstate != FogState.CLEAR)
        {
            return;

        }

        if (!unit.TryGetComponent(out UnitHitFeedback feedback))
        {
            feedback = unit.gameObject.AddComponent<UnitHitFeedback>();

        }

        feedback.PlayHit(attacker);

    }

    private void PlayHit(UnitController attacker)
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreColors();

        }

        CaptureSurfaces();
        if (surfaces.Count == 0)
        {
            return;

        }

        SpawnBlood(attacker);
        flashRoutine = StartCoroutine(FlashColors());

    }

    private void CaptureSurfaces()
    {
        surfaces.Clear();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
            {
                continue;

            }

            Material[] materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];
                if (material == null)
                {
                    continue;

                }

                Surface surface = new Surface
                {
                    Renderer = renderer,
                    Index = index,
                    BaseColor = material.HasProperty("_BaseColor"),
                    Color = material.HasProperty("_Color"),
                    PrimaryColor = material.HasProperty("_PrimaryColor"),
                    SecondaryColor = material.HasProperty("_SecondaryColor"),
                    AccentColor = material.HasProperty("_AccentColor"),
                    Original = new MaterialPropertyBlock(),
                    Flash = new MaterialPropertyBlock()
                };
                if (!surface.BaseColor && !surface.Color && !surface.PrimaryColor &&
                    !surface.SecondaryColor && !surface.AccentColor)
                {
                    continue;

                }

                renderer.GetPropertyBlock(surface.Original, index);
                renderer.GetPropertyBlock(surface.Flash, index);
                surfaces.Add(surface);

            }

        }

    }

    private IEnumerator FlashColors()
    {
        foreach (Color color in HitColors)
        {
            foreach (Surface surface in surfaces)
            {
                if (surface.Renderer == null)
                {
                    continue;

                }

                if (surface.BaseColor)
                {
                    surface.Flash.SetColor("_BaseColor", color);

                }
                if (surface.Color)
                {
                    surface.Flash.SetColor("_Color", color);

                }
                if (surface.PrimaryColor)
                {
                    surface.Flash.SetColor("_PrimaryColor", color);

                }
                if (surface.SecondaryColor)
                {
                    surface.Flash.SetColor("_SecondaryColor", color);

                }
                if (surface.AccentColor)
                {
                    surface.Flash.SetColor("_AccentColor", color);

                }
                surface.Renderer.SetPropertyBlock(surface.Flash, surface.Index);

            }

            yield return new WaitForSeconds(0.045f);

        }

        RestoreColors();
        flashRoutine = null;

    }

    private void RestoreColors()
    {
        foreach (Surface surface in surfaces)
        {
            if (surface.Renderer != null)
            {
                surface.Renderer.SetPropertyBlock(surface.Original, surface.Index);

            }

        }

        surfaces.Clear();

    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;

        }

        RestoreColors();

    }

    private void SpawnBlood(UnitController attacker)
    {
        Vector3 direction = attacker != null
            ? transform.position - attacker.transform.position : transform.forward;
        direction.y = 0.25f;
        direction.Normalize();

        Bounds bounds = surfaces[0].Renderer.bounds;
        foreach (Surface surface in surfaces)
        {
            if (surface.Renderer != null)
            {
                bounds.Encapsulate(surface.Renderer.bounds);

            }

        }

        GameObject effect = new GameObject("Blood Hit", typeof(ParticleSystem));
        effect.transform.SetPositionAndRotation(bounds.center, Quaternion.LookRotation(direction));
        ParticleSystem particles = effect.GetComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.12f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.72f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.19f);
        main.startColor = new Color(0.78f, 0.012f, 0.025f);
        main.gravityModifier = 0.7f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 32, 46) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 36f;
        shape.radius = 0.13f;

        ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.65f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        // Use a pipeline-compatible material instead of Unity's default particle shader.
        if (bloodMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null || !shader.isSupported)
            {
                shader = Shader.Find("Particles/Standard Unlit");

            }

            if (shader != null && shader.isSupported)
            {
                bloodMaterial = new Material(shader);
                bloodMaterial.name = "MutaBorn Blood Particles";
                bloodMaterial.hideFlags = HideFlags.DontSave;
                bloodMaterial.SetColor("_BaseColor", Color.white);
                bloodMaterial.SetColor("_Color", Color.white);
                bloodMaterial.SetFloat("_Surface", 1f);
                bloodMaterial.SetFloat("_Blend", 0f);
                bloodMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                bloodMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                bloodMaterial.SetInt("_ZWrite", 0);
                bloodMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                bloodMaterial.SetOverrideTag("RenderType", "Transparent");
                bloodMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            }

        }

        if (bloodMaterial != null)
        {
            effect.GetComponent<ParticleSystemRenderer>().sharedMaterial = bloodMaterial;

        }

        particles.Play();

    }

}
