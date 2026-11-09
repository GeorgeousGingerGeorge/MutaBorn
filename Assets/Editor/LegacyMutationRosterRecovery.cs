using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LegacyMutationRosterRecovery
{
    private const string DatabasePath = "Assets/Scriptable Objects/DataBase/MutationDataBase.asset";
    private const string ActionFolder = "Assets/Scriptable Objects/Action SOs/Actions/Abilities/Mutations";

    private static readonly MutationID[] SageNodes =
    {
        MutationID.Sage,
        MutationID.SageII,
        MutationID.SageIII

    };

    private static readonly HashSet<MutationID> OriginalNodes = new HashSet<MutationID>
    {
        MutationID.Vampirism,
        MutationID.VampirismII,
        MutationID.VampirismIII,
        MutationID.BloodHunter,
        MutationID.AssassinClaws,
        MutationID.LaunchExplosive,
        MutationID.BioLauncher,
        MutationID.CloakSkin,
        MutationID.DodgeJet,
        MutationID.HealingHands

    };

    private static readonly string[] BonusFields =
    {
        "strength", "dexterity", "vitality", "constitution", "perception", "intelligence",
        "health", "mana", "actionPoints", "meleeDamage", "rangeDamage", "magicDamage",
        "armor", "will"

    };

    [MenuItem("Tools/MutaBorn/Mutations/3 - Restore Original Six + Add Sage")]
    private static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Mutations", "Exit Play Mode first.", "OK");

            return;

        }

        MutationDataBase database = AssetDatabase.LoadAssetAtPath<MutationDataBase>(DatabasePath);
        if (database == null)
        {
            EditorUtility.DisplayDialog("Mutations", "Mutation database was not found at " + DatabasePath, "OK");

            return;

        }

        if (!EditorUtility.DisplayDialog("Restore Original Mutations",
            "Make all six original families available again (including Cloak Skin and Dodge Jet) " +
            "and add Sage with three functional mana-restoration stages. Existing nodes, scenes, and prefabs will not be replaced. Continue?",
            "Restore", "Cancel"))
        {

            return;

        }

        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty nodes = serialized.FindProperty("mutationNodes");
        int restored = 0;
        int created = 0;

        for (int index = 0; index < nodes.arraySize; index++)
        {
            SerializedProperty node = nodes.GetArrayElementAtIndex(index);
            MutationID id = (MutationID)node.FindPropertyRelative("id").intValue;
            if (OriginalNodes.Contains(id) && node.FindPropertyRelative("hiddenFromAcquisition").boolValue)
            {
                node.FindPropertyRelative("hiddenFromAcquisition").boolValue = false;
                restored++;

            }

        }

        for (int level = 1; level <= 3; level++)
        {
            MutationID id = SageNodes[level - 1];
            ActionDefinition action = GetOrCreateSageAction(level);
            SerializedProperty node = null;

            for (int index = 0; index < nodes.arraySize; index++)
            {
                SerializedProperty existing = nodes.GetArrayElementAtIndex(index);
                if (existing.FindPropertyRelative("id").intValue == (int)id)
                {
                    node = existing;

                    break;

                }

            }

            if (node == null)
            {
                nodes.arraySize++;
                node = nodes.GetArrayElementAtIndex(nodes.arraySize - 1);
                ConfigureNewSageNode(node, id, level, action);
                created++;

            }
            else
            {
                node.FindPropertyRelative("hiddenFromAcquisition").boolValue = false;
                SerializedProperty grantedActions = node.FindPropertyRelative("grantedActions");
                if (grantedActions.arraySize == 0 &&
                    node.FindPropertyRelative("actionSet").objectReferenceValue == null)
                {
                    grantedActions.arraySize = 1;
                    grantedActions.GetArrayElementAtIndex(0).objectReferenceValue = action;

                }

            }

        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Legacy Mutations",
            $"Sage stages created: {created}/3. Original nodes made available again: {restored}. " +
            "Existing nodes were preserved. Test Sage's mana-restoration action in combat.", "OK");

    }

    private static void ConfigureNewSageNode(SerializedProperty node, MutationID id, int level,
        ActionDefinition action)
    {
        node.FindPropertyRelative("id").intValue = (int)id;
        node.FindPropertyRelative("displayName").stringValue = "Sage " + Roman(level);
        node.FindPropertyRelative("description").stringValue =
            "Expanded neuroglial tissue and specialized mana-binding reservoirs support mental " +
            "resilience. The unit can release stored biochemical energy to replenish its own mana.";
        node.FindPropertyRelative("hiddenFromAcquisition").boolValue = false;
        node.FindPropertyRelative("familyId").stringValue = "sage";
        node.FindPropertyRelative("strain").intValue = (int)MutationStrain.Warden;
        node.FindPropertyRelative("level").intValue = level;

        SerializedProperty bonus = node.FindPropertyRelative("bonus");
        foreach (string field in BonusFields)
        {
            bonus.FindPropertyRelative(field).intValue = 0;

        }

        bonus.FindPropertyRelative("intelligence").intValue = level + 1;
        bonus.FindPropertyRelative("perception").intValue = level;
        bonus.FindPropertyRelative("mana").intValue = level == 1 ? 10 : level == 2 ? 15 : 22;
        bonus.FindPropertyRelative("will").intValue = level == 1 ? 4 : level == 2 ? 7 : 10;

        node.FindPropertyRelative("percentBonuses").arraySize = 0;
        SerializedProperty actions = node.FindPropertyRelative("grantedActions");
        actions.arraySize = 1;
        actions.GetArrayElementAtIndex(0).objectReferenceValue = action;
        node.FindPropertyRelative("grantedEffects").arraySize = 0;
        node.FindPropertyRelative("actionSet").objectReferenceValue = null;
        node.FindPropertyRelative("effectCollection").objectReferenceValue = null;
        node.FindPropertyRelative("occupiedEquipmentSlots").arraySize = 0;
        SerializedProperty next = node.FindPropertyRelative("nextEvolutions");
        next.arraySize = level < 3 ? 1 : 0;
        if (level < 3)
        {
            next.GetArrayElementAtIndex(0).intValue = (int)SageNodes[level];

        }

    }

    private static ActionDefinition GetOrCreateSageAction(int level)
    {
        string path = ActionFolder + "/sage-neuroglial-recharge-" + level + ".asset";
        ActionDefinition action = AssetDatabase.LoadAssetAtPath<ActionDefinition>(path);
        if (action != null)
        {

            return action;

        }

        action = ScriptableObject.CreateInstance<ActionDefinition>();
        action.name = "Neuroglial Recharge " + Roman(level);
        SerializedObject serialized = new SerializedObject(action);
        serialized.FindProperty("actionId").stringValue = "sage-neuroglial-recharge-" + level;
        serialized.FindProperty("displayName").stringValue = action.name;
        serialized.FindProperty("description").stringValue =
            "Convert stored biological energy into mana. Restores " +
            (level == 1 ? 12 : level == 2 ? 20 : 30) + " MP to the user.";
        serialized.FindProperty("activationType").intValue = (int)ActionActivationType.Active;
        serialized.FindProperty("category").intValue = (int)ActionMenuCategory.Abilities;
        serialized.FindProperty("submenuGroup").stringValue = "Mutations";
        serialized.FindProperty("displayOrder").intValue = 190;
        serialized.FindProperty("apCost").floatValue = 4f;
        serialized.FindProperty("maxUsesPerTurn").intValue = 1;
        serialized.FindProperty("controlTags").intValue = (int)ActionControlTags.MagicOrMutation;
        serialized.FindProperty("combatBehavior").intValue = (int)CombatActionBehavior.RestoreMana;
        serialized.FindProperty("manaRestoreAmount").intValue = level == 1 ? 12 : level == 2 ? 20 : 30;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(action, path);

        return action;

    }

    private static string Roman(int level)
    {

        return level == 1 ? "I" : level == 2 ? "II" : "III";

    }

}
