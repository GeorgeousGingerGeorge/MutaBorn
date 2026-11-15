#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MutaBornUnitPreviewInstaller
{
    private const string StagesName = "Unit Preview Stages (Edit Mode)";
    private const string TextureFolder = "Assets/RenderTextures";

    [MenuItem("Tools/MutaBorn/UI/Install Live Unit Previews (Hub)")]
    private static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "Hub")
        {
            Debug.LogWarning("Open Hub.unity in Edit Mode before installing unit previews.");

            return;

        }

        if (!AssetDatabase.IsValidFolder(TextureFolder))
        {
            AssetDatabase.CreateFolder("Assets", "RenderTextures");

        }

        GameObject stages = FindRoot(scene, StagesName);
        if (stages == null)
        {
            stages = new GameObject(StagesName);
            Undo.RegisterCreatedObjectUndo(stages, "Create Unit Preview Stages");

        }

        int installed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (BarracksMenu menu in root.GetComponentsInChildren<BarracksMenu>(true))
            {
                Transform panel = FindChild(menu.transform, "UDS Preview");
                if (panel == null)
                {
                    continue;

                }

                SerializedObject menuData = new SerializedObject(menu);
                bool isPrisoner = menuData.FindProperty("prisonerListOnly").boolValue;
                string label = isPrisoner ? "Brig" : "Barracks";
                RenderTexture texture = FindOrCreateTexture(label + "UnitPreview");
                UnitLoadoutPreview stage = FindOrCreateStage(stages.transform, label, texture, installed);
                RawImage image = FindOrCreateImage(panel, texture);
                Image cyanBackground = panel.GetComponent<Image>();
                if (cyanBackground != null)
                {
                    Undo.RecordObject(cyanBackground, "Hide Cyan Preview Placeholder");
                    cyanBackground.enabled = false;

                }

                image.texture = texture;
                menuData.FindProperty("unitPreview").objectReferenceValue = stage;
                menuData.ApplyModifiedProperties();
                EditorUtility.SetDirty(menu);
                installed++;

            }

        }

        if (installed == 0)
        {
            Debug.LogWarning("No Barracks / Brig UDS Preview panels found in Hub.");

            return;

        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Installed {installed} editable unit previews. Save Hub.unity to keep them.");

    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;

            }

        }

        return null;

    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;

            }

        }

        return null;

    }

    private static RenderTexture FindOrCreateTexture(string name)
    {
        string path = TextureFolder + "/" + name + ".renderTexture";
        RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if (texture == null)
        {
            texture = new RenderTexture(780, 480, 16, RenderTextureFormat.ARGB32)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            AssetDatabase.CreateAsset(texture, path);

        }

        return texture;

    }

    private static UnitLoadoutPreview FindOrCreateStage(Transform parent, string label,
        RenderTexture texture, int index)
    {
        Transform existing = parent.Find(label + " Unit Preview");
        GameObject stageObject;
        if (existing == null)
        {
            stageObject = new GameObject(label + " Unit Preview");
            Undo.RegisterCreatedObjectUndo(stageObject, "Create Unit Preview");
            stageObject.transform.SetParent(parent, false);
            stageObject.transform.position = new Vector3(10000f + index * 100f, -10000f, 0f);

        }
        else
        {
            stageObject = existing.gameObject;

        }

        UnitLoadoutPreview stage = stageObject.GetComponent<UnitLoadoutPreview>();
        if (stage == null)
        {
            stage = Undo.AddComponent<UnitLoadoutPreview>(stageObject);

        }

        Transform anchor = stageObject.transform.Find("Model Anchor");
        if (anchor == null)
        {
            GameObject anchorObject = new GameObject("Model Anchor");
            Undo.RegisterCreatedObjectUndo(anchorObject, "Create Model Anchor");
            anchorObject.transform.SetParent(stageObject.transform, false);
            anchor = anchorObject.transform;

        }

        Transform cameraTransform = stageObject.transform.Find("Preview Camera");
        Camera camera;
        if (cameraTransform == null)
        {
            GameObject cameraObject = new GameObject("Preview Camera");
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create Preview Camera");
            cameraObject.transform.SetParent(stageObject.transform, false);
            camera = Undo.AddComponent<Camera>(cameraObject);

        }
        else
        {
            camera = cameraTransform.GetComponent<Camera>();

        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;
        camera.cullingMask = 1 << 2;
        camera.orthographic = true;
        camera.orthographicSize = 2f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 40f;
        camera.allowHDR = false;
        camera.targetTexture = texture;
        camera.enabled = false;

        Transform lightTransform = stageObject.transform.Find("Preview Light");
        if (lightTransform == null)
        {
            GameObject lightObject = new GameObject("Preview Light");
            Undo.RegisterCreatedObjectUndo(lightObject, "Create Preview Light");
            lightObject.transform.SetParent(stageObject.transform, false);
            Light light = Undo.AddComponent<Light>(lightObject);
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.cullingMask = 1 << 2;
            light.transform.localRotation = Quaternion.Euler(45f, -30f, 0f);

        }

        SerializedObject data = new SerializedObject(stage);
        data.FindProperty("modelAnchor").objectReferenceValue = anchor;
        data.FindProperty("previewCamera").objectReferenceValue = camera;
        data.FindProperty("renderTexture").objectReferenceValue = texture;
        data.ApplyModifiedProperties();

        return stage;

    }

    private static RawImage FindOrCreateImage(Transform parent, RenderTexture texture)
    {
        Transform existing = parent.Find("Live Unit Render");
        GameObject imageObject;
        if (existing == null)
        {
            imageObject = new GameObject("Live Unit Render", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));
            Undo.RegisterCreatedObjectUndo(imageObject, "Create Unit Render Image");
            imageObject.transform.SetParent(parent, false);

        }
        else
        {
            imageObject = existing.gameObject;

        }

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        RawImage image = imageObject.GetComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = false;

        return image;

    }

}
#endif
