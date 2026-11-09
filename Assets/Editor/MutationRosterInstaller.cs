using System;
using System.Collections.Generic;
using System.IO;
using MutaBorn.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MutationRosterInstaller
{
    private const string DatabasePath = "Assets/Scriptable Objects/DataBase/MutationDataBase.asset";
    private const string Root = "Assets/Scriptable Objects/Action SOs/Actions/Abilities/Mutations/Biological";

    private sealed class Family
    {
        public readonly MutationID[] Nodes;
        public readonly string Name;
        public readonly string Key;
        public readonly string Description;
        public readonly MutationStrain Strain;
        public readonly CombatActionBehavior Behavior;
        public readonly StatIdentifier PercentStat;
        public readonly StatIdentifier TradeoffStat;

        public Family(MutationID first, MutationID second, MutationID third,
            string name, string key, string description, MutationStrain strain,
            CombatActionBehavior behavior, StatIdentifier percentStat = StatIdentifier.None,
            StatIdentifier tradeoffStat = StatIdentifier.None)
        {
            Nodes = new[] { first, second, third };
            Name = name;
            Key = key;
            Description = description;
            Strain = strain;
            Behavior = behavior;
            PercentStat = percentStat;
            TradeoffStat = tradeoffStat;

        }

    }

    private static readonly Family[] Families =
    {
        new Family(MutationID.OsteodermalCarapace, MutationID.OsteodermalCarapaceII, MutationID.OsteodermalCarapaceIII,
            "Osteodermal Carapace", "osteodermal-carapace",
            "Overlapping mineralized osteoderms form under collagen-rich skin. Contractile tissue braces the plates into a temporary protective shell, reducing impact and occasionally deflecting a direct hit.",
            MutationStrain.Warden, CombatActionBehavior.ApplyGuard),
        new Family(MutationID.MyotendinousOverdrive, MutationID.MyotendinousOverdriveII, MutationID.MyotendinousOverdriveIII,
            "Myotendinous Overdrive", "myotendinous-overdrive",
            "Hypertrophied fast-twitch fibers and recoil tendons accumulate elastic energy, releasing it in a straight-line rush that strikes and displaces a foe.",
            MutationStrain.Brute, CombatActionBehavior.DamageUnit),
        new Family(MutationID.BiomineralExtrusion, MutationID.BiomineralExtrusionII, MutationID.BiomineralExtrusionIII,
            "Biomineral Extrusion", "biomineral-extrusion",
            "Mineral-secreting glands expel rapidly setting protein-ceramic scaffolds that become temporary, destructible battlefield cover.",
            MutationStrain.Warden, CombatActionBehavior.MutationDeployCover),
        new Family(MutationID.VibrosensoryLattice, MutationID.VibrosensoryLatticeII, MutationID.VibrosensoryLatticeIII,
            "Vibrosensory Lattice", "vibrosensory-lattice",
            "Distributed mechanoreceptors detect faint ground and air vibrations, mapping nearby movement, hidden traps and concealed combatants.",
            MutationStrain.MindBore, CombatActionBehavior.MutationScan),
        new Family(MutationID.MyxospindleGlands, MutationID.MyxospindleGlandsII, MutationID.MyxospindleGlandsIII,
            "Myxospindle Glands", "myxospindle-glands",
            "Specialized spinnerets extrude adhesive protein fibers and irritating biochemicals into a single-use biological snare on the battlefield.",
            MutationStrain.Vile, CombatActionBehavior.MutationDeployTrap),
        new Family(MutationID.HyperreflexiveArc, MutationID.HyperreflexiveArcII, MutationID.HyperreflexiveArcIII,
            "Hyperreflexive Arc", "hyperreflexive-arc",
            "Shortened reflex arcs and heightened proprioception allow the unit to prepare a defensive parry and retaliate immediately when it evades an adjacent attack.",
            MutationStrain.MindBore, CombatActionBehavior.ApplySelfStatus),
        new Family(MutationID.PneumaticOsteospines, MutationID.PneumaticOsteospinesII, MutationID.PneumaticOsteospinesIII,
            "Pneumatic Osteospines", "pneumatic-osteospines",
            "Osteogenic pockets create detachable keratin-bone darts. Compressed musculature ejects them without requiring a weapon.",
            MutationStrain.Eldritch, CombatActionBehavior.DamageUnit),
        new Family(MutationID.HypertrophicGrasp, MutationID.HypertrophicGraspII, MutationID.HypertrophicGraspIII,
            "Hypertrophic Grasp", "hypertrophic-grasp",
            "Strengthened flexor tendons, enlarged gripping muscles and reinforced joints enable violent short-range shoves and throws against small foes.",
            MutationStrain.Brute, CombatActionBehavior.DamageUnit),
        new Family(MutationID.MyostatinSuppression, MutationID.MyostatinSuppressionII, MutationID.MyostatinSuppressionIII,
            "Myostatin Suppression", "myostatin-suppression",
            "Reduced myostatin signaling encourages excessive skeletal-muscle growth but diverts metabolic reserves from mana.",
            MutationStrain.Brute, CombatActionBehavior.None, StatIdentifier.Strength, StatIdentifier.MaxMana),
        new Family(MutationID.TendonElasticity, MutationID.TendonElasticityII, MutationID.TendonElasticityIII,
            "Tendon Elasticity", "tendon-elasticity",
            "Highly aligned elastin-rich tendons improve rapid precise movements at the cost of some protective tissue density.",
            MutationStrain.Brute, CombatActionBehavior.None, StatIdentifier.Dexterity, StatIdentifier.PhysicalDefense),
        new Family(MutationID.CorticalDensification, MutationID.CorticalDensificationII, MutationID.CorticalDensificationIII,
            "Cortical Densification", "cortical-densification",
            "Thickening cortical bone improves structural endurance while heavier limbs reduce movement efficiency.",
            MutationStrain.Warden, CombatActionBehavior.None, StatIdentifier.Constitution, StatIdentifier.MovementEfficiency),
        new Family(MutationID.HematopoieticExpansion, MutationID.HematopoieticExpansionII, MutationID.HematopoieticExpansionIII,
            "Hematopoietic Expansion", "hematopoietic-expansion",
            "Expanded marrow and oxygen-carrying blood volume support greater stamina and vitality but drain magical reserves.",
            MutationStrain.Warden, CombatActionBehavior.None, StatIdentifier.Vitality, StatIdentifier.MaxMana),
        new Family(MutationID.NeuralMyelination, MutationID.NeuralMyelinationII, MutationID.NeuralMyelinationIII,
            "Neural Myelination", "neural-myelination",
            "Enhanced neural myelin and glial support increase cognitive throughput but divert resources from bodily resilience.",
            MutationStrain.MindBore, CombatActionBehavior.None, StatIdentifier.Intelligence, StatIdentifier.MaxHealth),
        new Family(MutationID.SensoryHypertrophy, MutationID.SensoryHypertrophyII, MutationID.SensoryHypertrophyIII,
            "Sensory Hypertrophy", "sensory-hypertrophy",
            "Expanding sensory epithelia and neural processing regions sharpen perception while leaving fewer resources for dermal protection.",
            MutationStrain.MindBore, CombatActionBehavior.None, StatIdentifier.Perception, StatIdentifier.PhysicalDefense)

    };

    [MenuItem("Tools/MutaBorn/Mutations/1 - Install Biological Roster (Safe)")]
    private static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Mutation Roster", "Exit Play Mode before installing.", "OK");

            return;

        }

        MutationDataBase database = AssetDatabase.LoadAssetAtPath<MutationDataBase>(DatabasePath);
        if (database == null)
        {
            EditorUtility.DisplayDialog("Mutation Roster", "Cannot find MutationDataBase at " + DatabasePath, "OK");

            return;

        }

        if (!EditorUtility.DisplayDialog("Install Biological Mutations",
            "Creates 42 fixed nodes and their actions. Existing nodes, prefabs, scenes and purchased mutations are preserved. Commit your project first.",
            "Install", "Cancel"))
        {

            return;

        }

        EnsureFolder(Root);
        GameObject cover;
        GameObject[] traps = new GameObject[3];
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            cover = CreateCoverPrefab(preview);
            for (int level = 1; level <= 3; level++)
            {
                traps[level - 1] = CreateTrapPrefab(level, preview);

            }

        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);

        }

        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty nodes = serialized.FindProperty("mutationNodes");
        int created = 0;
        foreach (Family family in Families)
        {
            for (int level = 1; level <= 3; level++)
            {
                MutationID id = family.Nodes[level - 1];
                if (database.GetNode(id) != null)
                {

                    continue;

                }

                ActionDefinition action = family.Behavior == CombatActionBehavior.None ? null :
                    CreateAction(family, level, cover, traps[level - 1]);
                SerializedProperty node = nodes.GetArrayElementAtIndex(nodes.arraySize++);
                SetEnum(node, "id", (int)id);
                SetString(node, "displayName", family.Name + " " + Roman(level));
                SetString(node, "description", family.Description);
                SetBool(node, "hiddenFromAcquisition", false);
                SetString(node, "familyId", family.Key);
                SetEnum(node, "strain", (int)family.Strain);
                SetInt(node, "level", level);

                SerializedProperty bonus = node.FindPropertyRelative("bonus");
                foreach (string stat in new[]
                {
                    "strength", "dexterity", "vitality", "constitution", "perception", "intelligence",
                    "health", "mana", "actionPoints", "meleeDamage", "rangeDamage", "magicDamage", "armor", "will"

                })
                {
                    SetInt(bonus, stat, 0);

                }

                if (family.Nodes[0] == MutationID.OsteodermalCarapace)
                {
                    SetInt(bonus, "armor", level * 3);
                    SetInt(bonus, "health", level * 5);

                }

                SerializedProperty multipliers = node.FindPropertyRelative("percentBonuses");
                multipliers.arraySize = family.PercentStat == StatIdentifier.None ? 0 : 2;
                if (multipliers.arraySize > 0)
                {
                    SerializedProperty gain = multipliers.GetArrayElementAtIndex(0);
                    SetEnum(gain, "stat", (int)family.PercentStat);
                    SetFloat(gain, "percent", 10f * level);
                    SerializedProperty penalty = multipliers.GetArrayElementAtIndex(1);
                    SetEnum(penalty, "stat", (int)family.TradeoffStat);
                    SetFloat(penalty, "percent", -4f * level);

                }

                SerializedProperty granted = node.FindPropertyRelative("grantedActions");
                granted.arraySize = action == null ? 0 : 1;
                if (action != null)
                {
                    granted.GetArrayElementAtIndex(0).objectReferenceValue = action;

                }

                node.FindPropertyRelative("grantedEffects").arraySize = 0;
                node.FindPropertyRelative("actionSet").objectReferenceValue = null;
                node.FindPropertyRelative("effectCollection").objectReferenceValue = null;
                node.FindPropertyRelative("occupiedEquipmentSlots").arraySize = 0;
                SerializedProperty next = node.FindPropertyRelative("nextEvolutions");
                next.arraySize = level == 3 ? 0 : 1;
                if (level < 3)
                {
                    next.GetArrayElementAtIndex(0).intValue = (int)family.Nodes[level];

                }

                created++;

            }

        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Mutation Roster",
            $"Added {created} mutation nodes. Existing nodes left unchanged. See the mutation database asset and test in Barracks.", "OK");

    }

    [MenuItem("Tools/MutaBorn/Mutations/2 - Hide Legacy Test Mutations From New Purchases (Optional)")]
    private static void RetireLegacy()
    {
        MutationDataBase database = AssetDatabase.LoadAssetAtPath<MutationDataBase>(DatabasePath);
        if (database == null || !EditorUtility.DisplayDialog("Retire Legacy Test Purchases",
            "Hides the older test nodes from the acquisition dropdown. Current owners, evolution paths and assets stay intact. Continue?",
            "Hide Old Purchases", "Cancel"))
        {

            return;

        }

        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty nodes = serialized.FindProperty("mutationNodes");
        for (int i = 0; i < nodes.arraySize; i++)
        {
            SerializedProperty node = nodes.GetArrayElementAtIndex(i);
            if (node.FindPropertyRelative("id").intValue < (int)MutationID.OsteodermalCarapace)
            {
                SetBool(node, "hiddenFromAcquisition", true);

            }

        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Legacy Mutations", "Legacy purchases hidden. Nothing was deleted.", "OK");

    }

    private static ActionDefinition CreateAction(Family family, int level, GameObject cover, GameObject trap)
    {
        string path = Root + "/" + family.Key + "-" + level + ".asset";
        ActionDefinition existing = AssetDatabase.LoadAssetAtPath<ActionDefinition>(path);
        if (existing != null)
        {

            return existing;

        }

        ActionDefinition action = ScriptableObject.CreateInstance<ActionDefinition>();
        action.name = family.Name + " " + Roman(level);
        SerializedObject serialized = new SerializedObject(action);
        SetString(serialized, "actionId", family.Key + "-" + level);
        SetString(serialized, "displayName", family.Name + " " + Roman(level));
        SetString(serialized, "description", family.Description);
        SetEnum(serialized, "activationType", 0);
        SetEnum(serialized, "category", (int)ActionMenuCategory.Abilities);
        SetString(serialized, "submenuGroup", "Mutations");
        SetInt(serialized, "displayOrder", 200 + level);
        SetFloat(serialized, "apCost", 4f + (level == 3 ? 1f : 0f));
        SetEnum(serialized, "controlTags", (int)ActionControlTags.MagicOrMutation);
        SetEnum(serialized, "combatBehavior", (int)family.Behavior);
        SetEnum(serialized, "damageStat", (int)CombatDamageStat.Melee);
        SetFloat(serialized, "combatRange", 2.5f);
        SetFloat(serialized, "damageMultiplier", 1f);
        SetInt(serialized, "minimumDamage", 1);
        SetInt(serialized, "maxUsesPerTurn", 1);

        switch (family.Nodes[0])
        {
            case MutationID.OsteodermalCarapace:
                SetFloat(serialized, "guardDamageReductionPercent", 20f + level * 10f);
                SetInt(serialized, "guardFlatDamageReduction", 2 + level * 2);
                SetFloat(serialized, "mutationNegateChancePercent", 5f + level * 5f);
                SetInt(serialized, "mutationGuardOwnerTurns", level == 1 ? 1 : 2);

                break;

            case MutationID.MyotendinousOverdrive:
                SetFloat(serialized, "combatRange", 4f + level * 2f);
                SetFloat(serialized, "damageMultiplier", 1f + level * 0.25f);
                SetInt(serialized, "flatDamageBonus", 4 + level * 5);
                SetEnum(serialized, "damageType", (int)DamageType.Blunt);
                SetBool(serialized, "mutationRushToTarget", true);
                SetInt(serialized, "mutationKnockbackTiles", level == 3 ? 2 : 1);
                SetFloat(serialized, "apCost", 6f);

                break;

            case MutationID.BiomineralExtrusion:
                SetInt(serialized, "areaThrowRangeInTiles", 2 + level);
                SetInt(serialized, "mutationDeployDurationTurns", level + 1);
                SetInt(serialized, "mutationStructureHealth", 15 + level * 10);
                SetObject(serialized, "mutationDeployPrefab", cover);

                break;

            case MutationID.VibrosensoryLattice:
                SetInt(serialized, "areaRadiusInTiles", 1 + level * 2);
                SetFloat(serialized, "apCost", 3f);

                break;

            case MutationID.MyxospindleGlands:
                SetInt(serialized, "areaThrowRangeInTiles", 2 + level);
                SetInt(serialized, "mutationDeployDurationTurns", 1 + level);
                SetObject(serialized, "mutationDeployPrefab", trap);

                break;

            case MutationID.HyperreflexiveArc:
                SetObject(serialized, "selfStatus", CreateParryStatus(level));
                SetFloat(serialized, "apCost", 3f);

                break;

            case MutationID.PneumaticOsteospines:
                SetEnum(serialized, "damageStat", (int)CombatDamageStat.Ranged);
                SetEnum(serialized, "damageType", (int)DamageType.Pierce);
                SetFloat(serialized, "combatRange", 4f + level * 2f);
                SetFloat(serialized, "damageMultiplier", 0.8f + level * 0.2f);
                SetInt(serialized, "flatDamageBonus", 4 + level * 4);
                SetInt(serialized, "hitCount", level == 3 ? 2 : 1);

                break;

            case MutationID.HypertrophicGrasp:
                SetEnum(serialized, "damageType", (int)DamageType.Blunt);
                SetInt(serialized, "flatDamageBonus", 2 + level * 4);
                SetInt(serialized, "mutationKnockbackTiles", level + 1);

                break;

        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(action, path);

        return action;

    }

    private static StatusDefinition CreateParryStatus(int level)
    {
        string path = Root + "/hyperreflexive-parry-" + level + ".asset";
        StatusDefinition existing = AssetDatabase.LoadAssetAtPath<StatusDefinition>(path);
        if (existing != null)
        {

            return existing;

        }

        StatusDefinition status = ScriptableObject.CreateInstance<StatusDefinition>();
        status.name = "Hyperreflexive Parry " + Roman(level);
        SerializedObject serialized = new SerializedObject(status);
        SetString(serialized, "statusId", "hyperreflexive-parry-" + level);
        SetString(serialized, "displayName", status.name);
        SetBool(serialized, "isDebuff", false);
        SetInt(serialized, "durationOwnerTurns", level == 1 ? 1 : 2);
        SetFloat(serialized, "dodgeChancePercentPerStack", 10f + level * 10f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(status, path);

        return status;

    }

    private static StatusDefinition CreateSnareStatus(int level)
    {
        string path = Root + "/myxospindle-root-" + level + ".asset";
        StatusDefinition existing = AssetDatabase.LoadAssetAtPath<StatusDefinition>(path);
        if (existing != null)
        {

            return existing;

        }

        StatusDefinition status = ScriptableObject.CreateInstance<StatusDefinition>();
        status.name = "Myxospindle Adhesion " + Roman(level);
        SerializedObject serialized = new SerializedObject(status);
        SetString(serialized, "statusId", "myxospindle-root-" + level);
        SetString(serialized, "displayName", status.name);
        SetBool(serialized, "isDebuff", true);
        SetEnum(serialized, "controls", (int)StatusControlFlags.Root);
        SetInt(serialized, "durationOwnerTurns", level == 3 ? 2 : 1);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(status, path);

        return status;

    }

    private static GameObject CreateCoverPrefab(Scene preview)
    {
        string path = Root + "/Biomineral Cover.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {

            return existing;

        }

        string dataPath = Root + "/Biomineral Cover Data.asset";
        ObstacleData data = AssetDatabase.LoadAssetAtPath<ObstacleData>(dataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ObstacleData>();
            data.obstacleName = "Biomineral Cover";
            data.typeOfObstacle = ObstacleType.Solid;
            data.blocksMovement = true;
            data.blocksLineOfSight = true;
            data.coverState = CoverState.HIGH;
            data.directions = (CoverDirections)255;
            data.isDestructible = true;
            data.maxHealth = 45;
            AssetDatabase.CreateAsset(data, dataPath);

        }

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Biomineral Cover";
        SceneManager.MoveGameObjectToScene(root, preview);
        root.AddComponent<MutationDeployable>();
        ObstacleController obstacle = root.AddComponent<ObstacleController>();
        SerializedObject serialized = new SerializedObject(obstacle);
        SetObject(serialized, "data", data);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);

        return prefab;

    }

    private static GameObject CreateTrapPrefab(int level, Scene preview)
    {
        string path = Root + "/Myxospindle Snare " + level + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {

            return existing;

        }

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Myxospindle Snare " + Roman(level);
        SceneManager.MoveGameObjectToScene(root, preview);
        root.AddComponent<MutationDeployable>();
        WorldTrap trap = root.AddComponent<WorldTrap>();
        root.AddComponent<WorldTrapSensor>();
        SerializedObject serialized = new SerializedObject(trap);
        SetEnum(serialized, "shape", (int)TrapAreaShape.Rectangle);
        SetInt(serialized, "minimumDamage", 2 + level * 4);
        SetInt(serialized, "maximumDamage", 5 + level * 6);
        SetBool(serialized, "oneShot", true);
        SetBool(serialized, "startsArmed", true);
        SetBool(serialized, "startsHidden", false);
        SerializedProperty statusChances = serialized.FindProperty("statusChances");
        statusChances.arraySize = 1;
        SerializedProperty snare = statusChances.GetArrayElementAtIndex(0);
        SetObject(snare, "status", CreateSnareStatus(level));
        SetFloat(snare, "chancePercent", 50f + level * 15f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);

        return prefab;

    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {

            return;

        }

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));

    }

    private static string Roman(int level)
    {

        return level == 1 ? "I" : level == 2 ? "II" : "III";

    }

    private static void SetString(SerializedObject target, string name, string value)
    {
        target.FindProperty(name).stringValue = value;

    }

    private static void SetString(SerializedProperty target, string name, string value)
    {
        target.FindPropertyRelative(name).stringValue = value;

    }

    private static void SetInt(SerializedObject target, string name, int value)
    {
        target.FindProperty(name).intValue = value;

    }

    private static void SetInt(SerializedProperty target, string name, int value)
    {
        target.FindPropertyRelative(name).intValue = value;

    }

    private static void SetEnum(SerializedObject target, string name, int value)
    {
        target.FindProperty(name).intValue = value;

    }

    private static void SetEnum(SerializedProperty target, string name, int value)
    {
        target.FindPropertyRelative(name).intValue = value;

    }

    private static void SetFloat(SerializedObject target, string name, float value)
    {
        target.FindProperty(name).floatValue = value;

    }

    private static void SetFloat(SerializedProperty target, string name, float value)
    {
        target.FindPropertyRelative(name).floatValue = value;

    }

    private static void SetBool(SerializedObject target, string name, bool value)
    {
        target.FindProperty(name).boolValue = value;

    }

    private static void SetBool(SerializedProperty target, string name, bool value)
    {
        target.FindPropertyRelative(name).boolValue = value;

    }

    private static void SetObject(SerializedProperty target, string name, UnityEngine.Object value)
    {
        target.FindPropertyRelative(name).objectReferenceValue = value;

    }

    private static void SetObject(SerializedObject target, string name, UnityEngine.Object value)
    {
        target.FindProperty(name).objectReferenceValue = value;

    }

}
