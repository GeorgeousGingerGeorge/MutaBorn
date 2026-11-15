using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MutaBornDatabaseMotionInstaller
{
    [MenuItem("Tools/MutaBorn/UI/Install Database Transitions (Hub Only)")]
    private static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before installing Database transitions.");

            return;

        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Hub")
        {
            Debug.LogWarning("Open Hub.unity before installing Database transitions.");

            return;

        }

        GameObject database = FindObject(scene, "hub_DatabaseScreen");
        if (database == null)
        {
            Debug.LogWarning("hub_DatabaseScreen was not found. No changes made.");

            return;

        }

        int count = 0;
        string[] buttons = { "Mutations", "Units", "Equipment" };
        for (int i = 0; i < buttons.Length; i++)
        {
            Transform button = database.transform.Find(buttons[i]);
            if (button != null)
            {
                count += AddMotion(button.gameObject, MenuMotion.TransitionStyle.Slide,
                    new Vector2(0f, -90f - i * 25f), 1f, 0.22f + i * 0.05f);

            }

        }

        string[] panels = { "Mutations_InfoPanel", "Units_InfoPanel", "Equipment_InfoPanel" };
        string[] descriptions = { "Mutations_ItemDesc_Panel", "Units_DescPanel", "Equipment_DescPanel" };
        for (int i = 0; i < panels.Length; i++)
        {
            GameObject panel = FindObject(scene, panels[i]);
            if (panel != null)
            {
                count += AddMotion(panel, MenuMotion.TransitionStyle.Grow,
                    Vector2.zero, 0.86f, 0.26f);

            }

            GameObject descriptionGroup = FindObject(scene, descriptions[i]);
            if (descriptionGroup == null)
            {
                continue;

            }

            foreach (Transform description in descriptionGroup.transform)
            {
                count += AddMotion(description.gameObject, MenuMotion.TransitionStyle.Slide,
                    new Vector2(0f, -24f), 1f, 0.18f);

            }

        }

        if (count == 0)
        {
            Debug.Log("[MutaBorn UI] Database transitions were already installed. Existing Inspector settings were preserved.");

            return;

        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MutaBorn UI] Installed {count} editable Database transitions in Hub.unity.");

    }

    private static GameObject FindObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child.gameObject;

                }

            }

        }

        return null;

    }

    private static int AddMotion(GameObject target, MenuMotion.TransitionStyle style,
        Vector2 slideOffset, float startScale, float duration)
    {
        if (target.GetComponent<MenuMotion>() != null)
        {
            return 0;

        }

        MenuMotion motion = Undo.AddComponent<MenuMotion>(target);
        SerializedObject settings = new SerializedObject(motion);
        settings.FindProperty("transition").enumValueIndex = (int)style;
        settings.FindProperty("slideOffset").vector2Value = slideOffset;
        settings.FindProperty("startScale").floatValue = startScale;
        settings.FindProperty("duration").floatValue = duration;
        settings.ApplyModifiedProperties();

        return 1;

    }

}
