#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class MutaBornVfxInstaller
{
    private const string VfxFolder = "Assets/Prefabs/VFX";
    private const string ProjectileFolder = "Assets/Prefabs/Projectiles";
    private const string DefinitionsFolder = "Assets/Scriptable Objects/Projectiles";
    private const string MaterialFolder = "Assets/Materials/VFX";
    private const string TextureFolder = "Assets/Textures/VFX";
    private const string ImpactFolder = "Assets/Resources/VFX";

    [MenuItem("Tools/MutaBorn/VFX/Install Editable Trap, Interaction and Magic VFX")]
    public static void Install()
    {
        EnsureFolder(VfxFolder);
        EnsureFolder(ProjectileFolder);
        EnsureFolder(DefinitionsFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(TextureFolder);
        EnsureFolder(ImpactFolder);

        Material particleMaterial = GetParticleMaterial();
        Material orbMaterial = GetOrbMaterial();
        ParticleSystem trapBurst = GetBurst(VfxFolder + "/VFX_TrapBurst.prefab", particleMaterial, 18, 0.55f, 2.8f);
        ParticleSystem trapImpact = GetBurst(VfxFolder + "/VFX_TrapImpact.prefab", particleMaterial, 12, 0.32f, 1.6f);
        ParticleSystem interaction = GetBurst(VfxFolder + "/VFX_InteractableSpark.prefab", particleMaterial, 15, 0.75f, 1.3f);
        ParticleSystem magicImpact = GetBurst(VfxFolder + "/VFX_MagicImpact.prefab", particleMaterial, 25, 0.5f, 2.5f);
        GetElementImpact(particleMaterial);
        CreateImpactCircles();
        ParticleSystem plateIdle = GetPressurePlateIdle(particleMaterial);

        GameObject magicOrb = GetMagicOrb(particleMaterial, orbMaterial, magicImpact);
        ProjectileDefinition magicDefinition = GetDefinition(DefinitionsFolder + "/Projectile_MagicOrb.asset", magicOrb, 15f);
        GameObject arrow = EnhanceArrowPrefab(particleMaterial, magicImpact);
        ProjectileDefinition dartDefinition = arrow != null
            ? GetDefinition(DefinitionsFolder + "/Projectile_TrapDart.asset", arrow, 20f)
            : null;

        GameObject bluntBall = GetBluntBall(particleMaterial, orbMaterial, magicImpact);
        GameObject slashDisc = GetSlashDisc(particleMaterial, orbMaterial, magicImpact);
        GameObject elementArrow = GetElementArrow(arrow, particleMaterial, orbMaterial, magicImpact);

        ProjectileDefinition bluntDefinition = GetDefinition(DefinitionsFolder + "/Projectile_BluntBall.asset", bluntBall, 15f);
        ProjectileDefinition slashDefinition = GetDefinition(DefinitionsFolder + "/Projectile_SlashDisc.asset", slashDisc, 18f);
        ProjectileDefinition elementArrowDefinition = elementArrow != null
            ? GetDefinition(DefinitionsFolder + "/Projectile_ElementArrow.asset", elementArrow, 20f)
            : null;

        GameObject groundTile = GetTrapElementTile();
        int groundHazards = ConfigureGroundHazardSurface(groundTile);

        int traps = AttachTrapVfx(trapBurst, trapImpact, dartDefinition);
        int interactables = AttachInteractionVfx(interaction);
        int pressurePlates = AttachPressurePlateVfx(plateIdle);
        int swingTraps = ConfigureSwingHinges();
        int actions = AttachMagicProjectile(magicDefinition);
        AssignExplosiveArrow(elementArrowDefinition);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MutaBorn VFX] Installed editable prefabs. Traps: {traps}, ground hazards: {groundHazards}, interactables: {interactables}, direct magic actions: {actions}. " +
            $"Pressure plates: {pressurePlates}, swing hinges: {swingTraps}. Projectiles: {(arrow != null ? "Arrow+Trail, " : string.Empty)}{bluntDefinition.name}, {slashDefinition.name}, {(elementArrowDefinition != null ? elementArrowDefinition.name : "(no element arrow - arrow prefab missing)")}. Assets: {VfxFolder} and {ProjectileFolder}.");

    }

    private static void EnsureFolder(string path)
    {
        string[] names = path.Split('/');
        string parent = names[0];
        for (int i = 1; i < names.Length; i++)
        {
            string current = parent + "/" + names[i];
            if (!AssetDatabase.IsValidFolder(current))
            {
                AssetDatabase.CreateFolder(parent, names[i]);

            }

            parent = current;

        }

    }

    private static Material GetParticleMaterial()
    {
        string path = MaterialFolder + "/MutaBorn_SoftParticle.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;

        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Unlit/Transparent");
        if (shader == null)
        {
            throw new InvalidOperationException("No compatible particle shader found.");

        }

        material = new Material(shader) { name = "MutaBorn Soft Particle" };
        Texture2D texture = GetSoftDot();
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);

        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);

        }

        AssetDatabase.CreateAsset(material, path);
        return material;

    }

    private static Material GetOrbMaterial()
    {
        string path = MaterialFolder + "/MutaBorn_MagicOrb.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;

        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material material = new Material(shader) { name = "MutaBorn Magic Orb" };
        AssetDatabase.CreateAsset(material, path);
        return material;

    }

    private static Texture2D GetSoftDot()
    {
        string path = TextureFolder + "/SoftDot.png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
        {
            return existing;

        }

        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float radius = new Vector2(x - 15.5f, y - 15.5f).magnitude / 15.5f;
                float opacity = Mathf.Pow(Mathf.Clamp01(1f - radius), 1.4f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, opacity));

            }

        }

        texture.Apply();
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);

    }

    private static ParticleSystem GetBurst(string path, Material material, short count, float lifetime, float speed)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing.GetComponent<ParticleSystem>();

        }

        GameObject root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.duration = 0.12f;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.maxParticles = 96;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        ParticleSystem.ColorOverLifetimeModule overLifetime = particles.colorOverLifetime;
        overLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        overLifetime.color = fade;

        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<ParticleSystem>();

    }

    private static GameObject GetMagicOrb(Material particleMaterial, Material orbMaterial, ParticleSystem impact)
    {
        string path = ProjectileFolder + "/Projectile_MagicOrb.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject root = existing != null
            ? PrefabUtility.LoadPrefabContents(path)
            : new GameObject("Projectile_MagicOrb");

        try
        {
            EnsureProjectileController(root);
            ProjectileElementVfx visuals = root.GetComponent<ProjectileElementVfx>();
            if (visuals == null)
            {
                visuals = root.AddComponent<ProjectileElementVfx>();

            }

            Transform body = root.transform.Find("Orb");
            if (body == null)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Orb";
                sphere.transform.SetParent(root.transform, false);
                UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
                sphere.GetComponent<Renderer>().sharedMaterial = orbMaterial;
                body = sphere.transform;

            }

            // 0.5-unit diameter so the orb remains readable at combat camera distance.
            body.localScale = Vector3.one * 0.5f;
            Renderer orb = body.GetComponent<Renderer>();

            TrailRenderer trail = root.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = root.AddComponent<TrailRenderer>();

            }

            ConfigureTrail(trail, particleMaterial, 0.35f, 0.25f, 0.02f);
            ParticleSystem flight = FindChildComponent<ParticleSystem>(root.transform, "Flight Particles");
            if (flight == null)
            {
                flight = CreateFlightParticles(root.transform, particleMaterial, "Flight Particles", 32f, 0.08f, 0.06f, 0.12f);

            }

            ConfigureElementVisuals(visuals, orb, trail, flight, impact);
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (existing != null)
            {
                PrefabUtility.UnloadPrefabContents(root);

            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);

            }

        }

    }

    private static GameObject GetBluntBall(Material particleMaterial, Material orbMaterial, ParticleSystem impact)
    {
        string path = ProjectileFolder + "/Projectile_BluntBall.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing;

        }

        GameObject root = new GameObject("Projectile_BluntBall");
        root.AddComponent<ProjectileController>();
        ProjectileElementVfx visuals = root.AddComponent<ProjectileElementVfx>();

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Ball";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
        Renderer renderer = body.GetComponent<Renderer>();
        renderer.sharedMaterial = orbMaterial;

        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        ConfigureTrail(trail, particleMaterial, 0.28f, 0.16f, 0.03f);
        ParticleSystem flight = CreateFlightParticles(root.transform, particleMaterial, "Dust Trail", 18f, 0.06f, 0.04f, 0.08f);

        ConfigureElementVisuals(visuals, renderer, trail, flight, impact);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;

    }

    private static GameObject GetSlashDisc(Material particleMaterial, Material orbMaterial, ParticleSystem impact)
    {
        string path = ProjectileFolder + "/Projectile_SlashDisc.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject root = existing != null
            ? PrefabUtility.LoadPrefabContents(path)
            : new GameObject("Projectile_SlashDisc");

        try
        {
            EnsureProjectileController(root);
            ProjectileElementVfx visuals = root.GetComponent<ProjectileElementVfx>();
            if (visuals == null)
            {
                visuals = root.AddComponent<ProjectileElementVfx>();

            }

            Transform oldBlade = root.transform.Find("Slash Disc");
            if (oldBlade != null)
            {
                UnityEngine.Object.DestroyImmediate(oldBlade.gameObject);

            }

            Transform spinner = root.transform.Find("Spinning Plus");
            if (spinner == null)
            {
                GameObject spinning = new GameObject("Spinning Plus");
                spinner = spinning.transform;
                spinner.SetParent(root.transform, false);
                spinner.gameObject.AddComponent<ProjectileSpinVisual>();

            }

            // Two perpendicular bars form a plus facing along the projectile's +Z axis.
            Renderer bar = GetPlusBar(spinner, "Horizontal Bar", new Vector3(0.64f, 0.14f, 0.09f), orbMaterial);
            GetPlusBar(spinner, "Vertical Bar", new Vector3(0.14f, 0.64f, 0.09f), orbMaterial);

            TrailRenderer trail = root.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = root.AddComponent<TrailRenderer>();

            }

            ConfigureTrail(trail, particleMaterial, 0.25f, 0.24f, 0.02f);
            ParticleSystem flight = FindChildComponent<ParticleSystem>(root.transform, "Blade Sparks");
            if (flight == null)
            {
                flight = CreateFlightParticles(root.transform, particleMaterial, "Blade Sparks", 22f, 0.05f, 0.03f, 0.06f);

            }

            ConfigureElementVisuals(visuals, bar, trail, flight, impact);
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (existing != null)
            {
                PrefabUtility.UnloadPrefabContents(root);

            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);

            }

        }

    }

    private static Renderer GetPlusBar(Transform parent, string name, Vector3 size, Material material)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.GetComponent<Renderer>().sharedMaterial = material;
            child = cube.transform;

        }

        child.localScale = size;
        return child.GetComponent<Renderer>();

    }

    private static GameObject GetElementArrow(GameObject arrowSource, Material particleMaterial, Material orbMaterial, ParticleSystem impact)
    {
        string path = ProjectileFolder + "/Projectile_ElementArrow.prefab";
        if (arrowSource == null)
        {
            Debug.LogWarning("[MutaBorn VFX] Cannot create Projectile_ElementArrow because Projectile_Arrow.prefab was not found.");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(arrowSource), path))
            {
                Debug.LogWarning("[MutaBorn VFX] Failed to copy Projectile_Arrow.prefab for elemental arrow.");
                return null;

            }

        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            EnsureProjectileController(root);
            TrailRenderer trail = root.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = root.AddComponent<TrailRenderer>();

            }

            ConfigureTrail(trail, particleMaterial, 0.32f, 0.12f, 0.015f);
            ParticleSystem flight = FindChildComponent<ParticleSystem>(root.transform, "Flight Particles");
            if (flight == null)
            {
                flight = CreateFlightParticles(root.transform, particleMaterial, "Flight Particles", 20f, 0.05f, 0.035f, 0.07f);

            }

            Renderer renderer = FindFirstRenderer(root.transform);
            if (renderer == null)
            {
                GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                glow.name = "Element Tip";
                glow.transform.SetParent(root.transform, false);
                glow.transform.localPosition = new Vector3(0f, 0f, 0.3f);
                glow.transform.localScale = Vector3.one * 0.08f;
                UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());
                renderer = glow.GetComponent<Renderer>();
                renderer.sharedMaterial = orbMaterial;

            }

            ProjectileElementVfx visuals = root.GetComponent<ProjectileElementVfx>();
            if (visuals == null)
            {
                visuals = root.AddComponent<ProjectileElementVfx>();

            }

            ConfigureElementVisuals(visuals, renderer, trail, flight, impact);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);

        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);

    }

    private static GameObject EnhanceArrowPrefab(Material particleMaterial, ParticleSystem impact)
    {
        string path = ProjectileFolder + "/Projectile_Arrow.prefab";
        GameObject arrow = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (arrow == null)
        {
            Debug.LogWarning("[MutaBorn VFX] Projectile_Arrow.prefab not found. Standard bow arrow trail was not added.");
            return null;

        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            EnsureProjectileController(root);
            TrailRenderer trail = root.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = root.AddComponent<TrailRenderer>();

            }

            ConfigureTrail(trail, particleMaterial, 0.24f, 0.08f, 0.01f);
            ParticleSystem flight = FindChildComponent<ParticleSystem>(root.transform, "Flight Particles");
            if (flight == null)
            {
                flight = CreateFlightParticles(root.transform, particleMaterial, "Flight Particles", 12f, 0.04f, 0.025f, 0.05f);

            }

            ProjectileElementVfx visuals = root.GetComponent<ProjectileElementVfx>();
            if (visuals == null)
            {
                visuals = root.AddComponent<ProjectileElementVfx>();

            }

            Renderer renderer = FindFirstRenderer(root.transform);
            ConfigureElementVisuals(visuals, renderer, trail, flight, impact);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);

        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);

    }

    private static ProjectileDefinition GetDefinition(string path, GameObject prefab, float speed)
    {
        ProjectileDefinition result = AssetDatabase.LoadAssetAtPath<ProjectileDefinition>(path);
        if (result != null)
        {
            return result;

        }

        result = ScriptableObject.CreateInstance<ProjectileDefinition>();
        SerializedObject serialized = new SerializedObject(result);
        serialized.FindProperty("projectilePrefab").objectReferenceValue = prefab;
        serialized.FindProperty("trajectoryType").enumValueIndex = (int)ProjectileTrajectoryType.Straight;
        serialized.FindProperty("movementSpeed").floatValue = speed;
        serialized.FindProperty("arcHeight").floatValue = 0f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(result, path);
        return result;

    }

    private static int AttachTrapVfx(ParticleSystem fire, ParticleSystem hit, ProjectileDefinition dart)
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Missions/Traps" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                WorldTrap trap = root.GetComponentInChildren<WorldTrap>(true);
                if (trap == null)
                {
                    continue;

                }

                WorldTrapVfx visuals = trap.GetComponent<WorldTrapVfx>();
                if (visuals == null)
                {
                    visuals = trap.gameObject.AddComponent<WorldTrapVfx>();

                }

                SerializedObject trapSettings = new SerializedObject(trap);
                trapSettings.FindProperty("disarmedColor").colorValue = new Color(0.08f, 0.08f, 0.09f);
                trapSettings.FindProperty("elementTintStrength").floatValue = 0.25f;
                trapSettings.FindProperty("playElementImpactOnFire").boolValue = true;
                trapSettings.FindProperty("elementImpactSize").floatValue =
                    path.Contains("Barrel") || path.Contains("Chest") || path.Contains("Fall Trap") ? 1.15f : 0.65f;
                if (path.Contains("Ground Hazard"))
                {
                    trapSettings.FindProperty("playElementImpactOnFire").boolValue = false;

                }

                trapSettings.ApplyModifiedPropertiesWithoutUndo();

                WorldTrapHitAreaDebug debug = trap.GetComponent<WorldTrapHitAreaDebug>();
                if (debug != null)
                {
                    SerializedObject preview = new SerializedObject(debug);
                    preview.FindProperty("showWhenDisarmed").boolValue = false;
                    preview.ApplyModifiedPropertiesWithoutUndo();

                }

                SerializedObject serialized = new SerializedObject(visuals);
                AssignIfEmpty(serialized, "firePrefab", fire);
                AssignIfEmpty(serialized, "hitPrefab", hit);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (dart != null && System.IO.Path.GetFileName(path).StartsWith("Shooter Trap", StringComparison.Ordinal))
                {
                    SerializedObject trapSerialized = new SerializedObject(trap);
                    AssignIfEmpty(trapSerialized, "projectileDefinition", dart);
                    trapSerialized.ApplyModifiedPropertiesWithoutUndo();

                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                count++;

            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);

            }

        }

        return count;

    }

    private static int AttachInteractionVfx(ParticleSystem spark)
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Missions/Interactables" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                WorldInteractable target = root.GetComponentInChildren<WorldInteractable>(true);
                if (target is WorldLootContainer)
                {
                    continue;

                }

                WorldTileTrigger trigger = root.GetComponentInChildren<WorldTileTrigger>(true);
                GameObject recipient = target != null ? target.gameObject :
                    trigger != null ? trigger.gameObject : null;
                if (recipient == null)
                {
                    continue;

                }

                WorldInteractableVfx visuals = recipient.GetComponent<WorldInteractableVfx>();
                if (visuals == null)
                {
                    visuals = recipient.AddComponent<WorldInteractableVfx>();

                }

                SerializedObject serialized = new SerializedObject(visuals);
                AssignIfEmpty(serialized, "interactionPrefab", spark);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                count++;

            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);

            }

        }

        return count;

    }

    private static int AttachMagicProjectile(ProjectileDefinition magic)
    {
        int count = 0;
        string[] paths = AssetDatabase.FindAssets("t:ActionDefinition", new[]
        {
            "Assets/Scriptable Objects/Action SOs/Actions/Attacks/Weapon Attacks/Magic"
        });
        foreach (string guid in paths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ActionDefinition action = AssetDatabase.LoadAssetAtPath<ActionDefinition>(path);
            if (action == null || action.DamageStat != CombatDamageStat.Magic ||
                action.Projectile != null)
            {
                continue;

            }

            bool directSpell = action.CombatBehavior == CombatActionBehavior.DamageUnit;
            bool launchedAreaSpell = action.CombatBehavior == CombatActionBehavior.DamageArea &&
                (action.Id == "fireball" || action.Id == "poison-cloud");
            if (!directSpell && !launchedAreaSpell)
            {
                continue;

            }

            SerializedObject serialized = new SerializedObject(action);
            serialized.FindProperty("projectileDefinition").objectReferenceValue = magic;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(action);
            count++;

        }

        return count;

    }

    private static ParticleSystem GetElementImpact(Material material)
    {
        string path = ImpactFolder + "/VFX_ElementImpact.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing.GetComponent<ParticleSystem>();

        }

        GameObject root = new GameObject("VFX_ElementImpact");
        ParticleSystem sparks = root.AddComponent<ParticleSystem>();
        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = sparks.main;
        main.loop = false;
        main.duration = 0.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.15f);
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.maxParticles = 80;

        ParticleSystem.EmissionModule emission = sparks.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });
        ParticleSystem.ShapeModule shape = sparks.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;
        ParticleSystem.ColorOverLifetimeModule fade = sparks.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;

        GameObject flash = new GameObject("Flash");
        flash.transform.SetParent(root.transform, false);
        ParticleSystem flashParticles = flash.AddComponent<ParticleSystem>();
        flashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule flashMain = flashParticles.main;
        flashMain.loop = false;
        flashMain.duration = 0.08f;
        flashMain.startLifetime = 0.13f;
        flashMain.startSpeed = 0f;
        flashMain.startSize = 0.42f;
        flashMain.playOnAwake = false;
        ParticleSystem.EmissionModule flashEmission = flashParticles.emission;
        flashEmission.rateOverTime = 0f;
        flashEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
        ParticleSystem.ColorOverLifetimeModule flashFade = flashParticles.colorOverLifetime;
        flashFade.enabled = true;
        flashFade.color = gradient;
        flashParticles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<ParticleSystem>();

    }

    private static void AssignExplosiveArrow(ProjectileDefinition arrow)
    {
        if (arrow == null)
        {
            return;

        }

        string path = "Assets/Scriptable Objects/Action SOs/Actions/Attacks/Weapon Attacks/Bow/Explosive Arrow.asset";
        ActionDefinition action = AssetDatabase.LoadAssetAtPath<ActionDefinition>(path);
        if (action == null || action.CombatBehavior != CombatActionBehavior.DamageArea ||
            action.Projectile != null)
        {
            return;

        }

        SerializedObject serialized = new SerializedObject(action);
        serialized.FindProperty("projectileDefinition").objectReferenceValue = arrow;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(action);

    }

    private static GameObject GetTrapElementTile()
    {
        string path = VfxFolder + "/VFX_TrapElementTile.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing;

        }

        string materialPath = MaterialFolder + "/MutaBorn_TrapFloorTint.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            if (shader == null)
            {
                throw new InvalidOperationException("Transparent shader for trap floor tint was not found.");

            }

            material = new Material(shader) { name = "MutaBorn Trap Floor Tint" };
            AssetDatabase.CreateAsset(material, materialPath);

        }

        GameObject root = new GameObject("VFX_TrapElementTile");
        root.layer = 2; // Ignore Raycast
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Floor Tint";
        quad.layer = 2;
        quad.transform.SetParent(root.transform, false);
        quad.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        Renderer renderer = quad.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;

    }

    private static int ConfigureGroundHazardSurface(GameObject tilePrefab)
    {
        string path = "Assets/Prefabs/Missions/Traps/Ground Hazard (End Turn).prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            return 0;

        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            WorldTrap trap = root.GetComponent<WorldTrap>();
            if (trap == null)
            {
                return 0;

            }

            WorldTrapAreaVisual surface = root.GetComponent<WorldTrapAreaVisual>();
            if (surface == null)
            {
                surface = root.AddComponent<WorldTrapAreaVisual>();

            }

            SerializedObject serialized = new SerializedObject(surface);
            serialized.FindProperty("tilePrefab").objectReferenceValue = tilePrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Replace the old fixed 2x2 red cube with tiled floor coloration.
            Transform oldVisual = root.transform.Find("Trap Visual");
            if (oldVisual != null)
            {
                Renderer renderer = oldVisual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;

                }

            }

            WorldTrapHitAreaDebug debug = root.GetComponent<WorldTrapHitAreaDebug>();
            if (debug != null)
            {
                SerializedObject warning = new SerializedObject(debug);
                warning.FindProperty("showInGame").boolValue = false;
                warning.ApplyModifiedPropertiesWithoutUndo();

            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return 1;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);

        }

    }

    private static void ConfigureTrail(TrailRenderer trail, Material material, float time, float startWidth, float endWidth)
    {
        trail.sharedMaterial = material;
        trail.time = time;
        trail.startWidth = startWidth;
        trail.endWidth = endWidth;
        trail.minVertexDistance = 0.03f;
        trail.alignment = LineAlignment.View;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.generateLightingData = false;

    }

    private static ParticleSystem CreateFlightParticles(Transform parent, Material material, string childName, float rate, float radius, float startSizeMin, float startSizeMax)
    {
        GameObject flightObject = new GameObject(childName);
        flightObject.transform.SetParent(parent, false);
        ParticleSystem flight = flightObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = flight.main;
        main.loop = true;
        main.startLifetime = 0.25f;
        main.startSpeed = 0.10f;
        main.startSize = new ParticleSystem.MinMaxCurve(startSizeMin, startSizeMax);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;
        main.playOnAwake = true;
        ParticleSystem.EmissionModule emission = flight.emission;
        emission.rateOverTime = rate;
        ParticleSystem.ShapeModule shape = flight.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        ParticleSystemRenderer renderer = flight.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        return flight;

    }

    private static void ConfigureElementVisuals(ProjectileElementVfx visuals, Renderer renderer, TrailRenderer trail, ParticleSystem flight, ParticleSystem impact)
    {
        SerializedObject serialized = new SerializedObject(visuals);
        serialized.FindProperty("orbRenderer").objectReferenceValue = renderer;
        serialized.FindProperty("trail").objectReferenceValue = trail;
        serialized.FindProperty("flightParticles").objectReferenceValue = flight;
        serialized.FindProperty("impactPrefab").objectReferenceValue = impact;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(visuals);

    }

    private static void EnsureProjectileController(GameObject root)
    {
        if (root.GetComponent<ProjectileController>() == null)
        {
            root.AddComponent<ProjectileController>();

        }

    }

    private static Renderer FindFirstRenderer(Transform root)
    {
        return root.GetComponentInChildren<Renderer>(true);

    }

    private static T FindChildComponent<T>(Transform root, string name) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            if (component.name == name)
            {
                return component;

            }

        }

        return null;

    }

    private static ParticleSystem GetPressurePlateIdle(Material material)
    {
        string path = VfxFolder + "/VFX_PressurePlateIdle.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing.GetComponent<ParticleSystem>();

        }

        GameObject root = new GameObject("VFX_PressurePlateIdle");
        ParticleSystem effect = root.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = effect.main;
        main.loop = true;
        main.duration = 1.4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
        main.startColor = new Color(1f, 0.88f, 0.48f, 0.75f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.maxParticles = 48;
        ParticleSystem.EmissionModule emission = effect.emission;
        emission.rateOverTime = 12f;
        ParticleSystem.ShapeModule shape = effect.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.9f, 0.05f, 0.9f);
        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<ParticleSystem>();

    }

    private static int AttachPressurePlateVfx(ParticleSystem plateEffect)
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Missions" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;
                foreach (WorldTileTrigger trigger in root.GetComponentsInChildren<WorldTileTrigger>(true))
                {
                    SerializedObject triggerData = new SerializedObject(trigger);
                    Transform plate = triggerData.FindProperty("plateVisual").objectReferenceValue as Transform;
                    Transform host = plate != null ? plate : trigger.transform;
                    if (host.Find("VFX_PressurePlateIdle") != null)
                    {
                        continue;

                    }

                    GameObject sparkle = (GameObject)PrefabUtility.InstantiatePrefab(plateEffect.gameObject, host);
                    sparkle.transform.localPosition = new Vector3(0f, 0.13f, 0f);
                    sparkle.transform.localRotation = Quaternion.identity;
                    sparkle.transform.localScale = Vector3.one;
                    changed = true;
                    count++;

                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);

                }

            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);

            }

        }

        return count;

    }

    private static int ConfigureSwingHinges()
    {
        int count = 0;
        string[] names = { "Swing Trap (Horizontal)", "Swing Trap (Vertical)" };
        foreach (string name in names)
        {
            string path = "Assets/Prefabs/Missions/Traps/" + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                continue;

            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                WorldTrapVisual visual = root.GetComponent<WorldTrapVisual>();
                if (visual == null)
                {
                    continue;

                }

                SerializedObject data = new SerializedObject(visual);
                Transform moving = data.FindProperty("movingVisual").objectReferenceValue as Transform;
                if (moving == null)
                {
                    continue;

                }

                bool horizontal = name.Contains("Horizontal");
                Transform hinge = moving.name == "Swing Hinge" ? moving : root.transform.Find("Swing Hinge");
                Transform blade = moving.name == "Swing Hinge" ? (moving.childCount > 0 ? moving.GetChild(0) : null) : moving;
                if (blade == null)
                {
                    continue;

                }

                Transform originalParent = hinge != null ? hinge.parent : blade.parent;
                if (originalParent == null)
                {
                    originalParent = root.transform;

                }

                if (hinge == null)
                {
                    GameObject pivotObject = new GameObject("Swing Hinge");
                    hinge = pivotObject.transform;
                    hinge.SetParent(originalParent, false);

                }

                Renderer bladeRenderer = blade.GetComponentInChildren<Renderer>(true);
                Vector3 worldPivot = blade.position;
                if (bladeRenderer != null)
                {
                    Bounds bounds = bladeRenderer.bounds;
                    worldPivot = horizontal
                        ? new Vector3(bounds.min.x, bounds.center.y, bounds.center.z)
                        : new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

                }

                Vector3 bladeWorldPosition = blade.position;
                Quaternion bladeWorldRotation = blade.rotation;
                Vector3 bladeWorldScale = blade.lossyScale;
                hinge.position = worldPivot;
                hinge.rotation = Quaternion.identity;
                blade.SetParent(hinge, true);
                blade.position = bladeWorldPosition;
                blade.rotation = bladeWorldRotation;
                // We intentionally preserve the current world transform with SetParent(true).
                data.FindProperty("movingVisual").objectReferenceValue = hinge;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                count++;

            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);

            }

        }

        return count;

    }

    private static void CreateImpactCircles()
    {
        string path = ImpactFolder + "/VFX_ElementImpact.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            return;

        }

        Material material = GetImpactCircleMaterial();
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform oldRing = root.transform.Find("Shockwave Ring");
            if (oldRing != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRing.gameObject);

            }

            Transform oldDisk = root.transform.Find("Shockwave Disk");
            if (oldDisk != null)
            {
                UnityEngine.Object.DestroyImmediate(oldDisk.gameObject);

            }

            Transform sphere = root.transform.Find("Impact Sphere");
            if (sphere == null)
            {
                GameObject sphereObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphereObject.name = "Impact Sphere";
                sphere = sphereObject.transform;
                sphere.SetParent(root.transform, false);
                sphere.localPosition = Vector3.up * 0.02f;
                Collider collider = sphere.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);

                }

            }

            MeshRenderer renderer = sphere.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

            }

            ElementImpactSphereFade pulse = sphere.GetComponent<ElementImpactSphereFade>();
            if (pulse == null)
            {
                pulse = sphere.gameObject.AddComponent<ElementImpactSphereFade>();

            }

            SerializedObject pulseData = new SerializedObject(pulse);
            pulseData.FindProperty("duration").floatValue = 1.1f;
            pulseData.FindProperty("growSeconds").floatValue = 0.18f;
            pulseData.FindProperty("finalDiameter").floatValue = 2f;
            pulseData.FindProperty("startOpacity").floatValue = 0.2f;
            pulseData.FindProperty("fadeStart").floatValue = 0.5f;
            pulseData.FindProperty("grayStart").floatValue = 0.78f;
            pulseData.FindProperty("fadeTarget").colorValue = new Color(0.22f, 0.22f, 0.22f, 0f);
            pulseData.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);

        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);

        }

    }

    private static Mesh GetCircleMesh(string name, float innerRatio)
    {
        string path = MaterialFolder + "/" + name + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            return existing;

        }

        const int segments = 48;
        Vector3[] vertices = new Vector3[segments * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            vertices[i * 2] = new Vector3(x * innerRatio * 0.5f, 0f, z * innerRatio * 0.5f);
            vertices[i * 2 + 1] = new Vector3(x * 0.5f, 0f, z * 0.5f);
            uvs[i * 2] = new Vector2(0.5f + x * innerRatio * 0.5f, 0.5f + z * innerRatio * 0.5f);
            uvs[i * 2 + 1] = new Vector2(0.5f + x * 0.5f, 0.5f + z * 0.5f);
            int next = (i + 1) % segments;
            int at = i * 6;
            triangles[at] = i * 2;
            triangles[at + 1] = next * 2;
            triangles[at + 2] = i * 2 + 1;
            triangles[at + 3] = next * 2;
            triangles[at + 4] = next * 2 + 1;
            triangles[at + 5] = i * 2 + 1;

        }

        Mesh mesh = new Mesh { name = name, vertices = vertices, triangles = triangles, uv = uvs };
        mesh.RecalculateNormals();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;

    }

    private static Material GetImpactCircleMaterial()
    {
        string path = MaterialFolder + "/MutaBorn_ImpactCircle.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            if (shader == null)
            {
                throw new InvalidOperationException("Impact sphere requires a transparent shader.");

            }

            material = new Material(shader) { name = "MutaBorn Impact Circle" };
            AssetDatabase.CreateAsset(material, path);

        }

        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        EditorUtility.SetDirty(material);
        return material;

    }

    private static void AddCircle(Transform parent, string name, Mesh mesh, Material material,
        float duration, float diameter, float opacity)
    {
        if (parent.Find(name) != null)
        {
            return;

        }

        GameObject circle = new GameObject(name);
        circle.transform.SetParent(parent, false);
        circle.transform.localPosition = Vector3.up * (name == "Shockwave Disk" ? 0.07f : 0.075f);
        MeshFilter filter = circle.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = circle.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        ElementImpactRing motion = circle.AddComponent<ElementImpactRing>();
        SerializedObject data = new SerializedObject(motion);
        data.FindProperty("duration").floatValue = duration;
        data.FindProperty("finalDiameter").floatValue = diameter;
        data.FindProperty("opacity").floatValue = opacity;
        data.ApplyModifiedPropertiesWithoutUndo();

    }

    private static void AssignIfEmpty(SerializedObject target, string name, UnityEngine.Object value)
    {
        SerializedProperty property = target.FindProperty(name);
        if (property != null && property.objectReferenceValue == null)
        {
            property.objectReferenceValue = value;

        }

    }
}
#endif
