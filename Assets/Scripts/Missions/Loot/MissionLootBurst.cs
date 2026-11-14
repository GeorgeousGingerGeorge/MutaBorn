using UnityEngine;
using UnityEngine.Rendering;

// A short burst of tumbling coins and gems when a loot container is opened.
public static class MissionLootBurst
{
    private static Mesh coinMesh;
    private static Mesh gemMesh;
    private static Material particleMaterial;

    public static void Play(Transform container)
    {
        Renderer[] renderers = container.GetComponentsInChildren<Renderer>();
        Vector3 origin = container.position + Vector3.up * 0.8f;
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);

            }

            origin = new Vector3(bounds.center.x, bounds.max.y + 0.05f, bounds.center.z);

        }

        if (coinMesh == null)
        {
            coinMesh = CreateCoinMesh();
            gemMesh = CreateGemMesh();

        }

        if (particleMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null || !shader.isSupported)
            {
                shader = Shader.Find("Particles/Standard Unlit");

            }

            if (shader == null || !shader.isSupported)
            {
                shader = Shader.Find("Unlit/Color");

            }

            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[MutaBorn Loot V2] Particle shader unavailable; treasure burst cannot render.");

                return;

            }

            particleMaterial = new Material(shader);
            particleMaterial.name = "MutaBorn Loot Particles";
            particleMaterial.hideFlags = HideFlags.DontSave;
            if (particleMaterial.HasProperty("_BaseColor"))
            {
                particleMaterial.SetColor("_BaseColor", Color.white);

            }
            if (particleMaterial.HasProperty("_Color"))
            {
                particleMaterial.SetColor("_Color", Color.white);

            }

        }

        Spawn(origin, "Gold Coins", coinMesh, new Color(1f, 0.75f, 0.15f), 10, 14);
        Spawn(origin, "Silver Coins", coinMesh, new Color(0.78f, 0.85f, 0.95f), 8, 12);
        Spawn(origin, "Gems", gemMesh, new Color(0.32f, 0.92f, 0.78f), 6, 9);

    }

    private static void Spawn(Vector3 position, string effectName, Mesh mesh, Color color, short minimum, short maximum)
    {
        GameObject effect = new GameObject(effectName, typeof(ParticleSystem));
        effect.transform.SetPositionAndRotation(position, Quaternion.FromToRotation(Vector3.forward, Vector3.up));
        ParticleSystem particles = effect.GetComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 5.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.25f);
        main.startColor = color;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.maxParticles = 20;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, minimum, maximum) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 34f;
        shape.radius = 0.18f;

        ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.separateAxes = true;
        rotation.x = new ParticleSystem.MinMaxCurve(-5f, 5f);
        rotation.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
        rotation.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

        ParticleSystemRenderer particleRenderer = effect.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleRenderer.mesh = mesh;
        particleRenderer.sharedMaterial = particleMaterial;
        particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        particles.Play();

    }

    private static Mesh CreateCoinMesh()
    {
        const int sides = 12;
        Vector3[] vertices = new Vector3[sides * 2 + 2];
        int[] triangles = new int[sides * 12];
        vertices[sides * 2] = new Vector3(0f, 0.09f, 0f);
        vertices[sides * 2 + 1] = new Vector3(0f, -0.09f, 0f);

        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            float x = Mathf.Cos(angle) * 0.5f;
            float z = Mathf.Sin(angle) * 0.5f;
            vertices[i * 2] = new Vector3(x, 0.09f, z);
            vertices[i * 2 + 1] = new Vector3(x, -0.09f, z);

            int next = (i + 1) % sides * 2;
            int start = i * 12;
            int top = i * 2;
            int bottom = top + 1;

            triangles[start] = sides * 2;
            triangles[start + 1] = next;
            triangles[start + 2] = top;
            triangles[start + 3] = sides * 2 + 1;
            triangles[start + 4] = bottom;
            triangles[start + 5] = next + 1;
            triangles[start + 6] = top;
            triangles[start + 7] = next;
            triangles[start + 8] = bottom;
            triangles[start + 9] = next;
            triangles[start + 10] = next + 1;
            triangles[start + 11] = bottom;

        }

        Mesh mesh = new Mesh { name = "Loot Coin", hideFlags = HideFlags.DontSave };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;

    }

    private static Mesh CreateGemMesh()
    {
        Mesh mesh = new Mesh { name = "Loot Gem", hideFlags = HideFlags.DontSave };
        mesh.vertices = new[]
        {
            new Vector3(0f, 0.65f, 0f),
            new Vector3(0.5f, 0f, 0f),
            new Vector3(0f, 0f, 0.5f),
            new Vector3(-0.5f, 0f, 0f),
            new Vector3(0f, 0f, -0.5f),
            new Vector3(0f, -0.65f, 0f)
        };
        mesh.triangles = new[]
        {
            0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4,
            5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;

    }

}
