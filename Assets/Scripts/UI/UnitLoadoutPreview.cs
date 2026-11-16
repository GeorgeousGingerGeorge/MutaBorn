using UnityEngine;

// A visual-only copy of a unit, rendered into the existing Barracks / Brig UI.
// The camera, stage, and RawImage are authored in the Hub scene by the editor installer.
public sealed class UnitLoadoutPreview : MonoBehaviour
{
    [SerializeField] private Transform modelAnchor;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RenderTexture renderTexture;

    private static readonly string[] SocketNames =
    {
        "wpnSkt_R", "wpnSkt_L", "armorSkt_Head", "armorSkt_Body",
        "armorSkt_Arms", "armorSkt_Legs", "armorSkt_Boots",
        "accessorySkt_1", "accessorySkt_2", "accessorySkt_3"
    };

    private const int PreviewLayer = 2;
    private UnitData selectedUnit;
    private GameObject model;

    public void Show(UnitData unit)
    {
        if (ReferenceEquals(selectedUnit, unit))
        {
            return;

        }

        if (selectedUnit != null)
        {
            selectedUnit.LoadoutChanged -= RefreshEquipment;

        }

        selectedUnit = unit;
        if (model != null)
        {
            Destroy(model);
            model = null;

        }

        if (previewCamera != null)
        {
            previewCamera.enabled = unit != null;

        }

        if (unit == null || modelAnchor == null || unit.RaceDefinition == null ||
            unit.RaceDefinition.RaceModelPrefab == null)
        {
            return;

        }

        GameObject source = unit.RaceDefinition.RaceModelPrefab;
        model = Instantiate(source, modelAnchor, false);
        model.name = source.name + " (Preview)";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        SetPreviewLayer(model);

        unit.LoadoutChanged += RefreshEquipment;
        RefreshEquipment();

    }

    private void RefreshEquipment()
    {
        if (model == null || selectedUnit == null)
        {
            return;

        }

        foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Preview Equipment")
            {
                Destroy(child.gameObject);

            }

        }

        foreach (UnitSlotId slot in UnitLoadoutSlots.All)
        {
            if (!selectedUnit.TryGetEquipmentAtSlot(slot, out EquipmentInstance item) ||
                item.ModelPrefab == null)
            {
                continue;

            }

            EquipmentTemplate definition = null;
            if (item.typeDefinition != null)
            {
                item.typeDefinition.TryGetTemplate(item.templateId, out definition);

            }

            EquipmentAttachmentSocket socket = definition != null &&
                definition.SocketOverride != EquipmentAttachmentSocket.Auto
                ? definition.SocketOverride : item.typeDefinition != null
                    ? item.typeDefinition.DefaultSocket : EquipmentAttachmentSocket.Auto;
            int index = socket == EquipmentAttachmentSocket.Auto
                ? (int)slot - 1 : (int)socket - 1;
            if (index < 0 || index >= SocketNames.Length)
            {
                continue;

            }

            Transform attachment = FindSocket(model.transform, SocketNames[index]);
            if (attachment == null)
            {
                continue;

            }

            GameObject equipment = Instantiate(item.ModelPrefab, attachment, false);
            equipment.name = "Preview Equipment";
            equipment.transform.localPosition = Vector3.zero;
            equipment.transform.localRotation = Quaternion.identity;

            if (definition != null && item.typeDefinition != null)
            {
                bool overridden = definition.OverrideAttachment;
                equipment.transform.localPosition = overridden
                    ? definition.PositionOffset : item.typeDefinition.PositionOffset;
                equipment.transform.localRotation = Quaternion.Euler(overridden
                    ? definition.RotationOffset : item.typeDefinition.RotationOffset);
                equipment.transform.localScale = Vector3.Scale(equipment.transform.localScale,
                    overridden ? definition.Scale : item.typeDefinition.Scale);

                if (definition.OverrideColors || item.typeDefinition.ApplyColors)
                {
                    Color primary = definition.OverrideColors
                        ? definition.PrimaryColor : item.typeDefinition.PrimaryColor;
                    Color secondary = definition.OverrideColors
                        ? definition.SecondaryColor : item.typeDefinition.SecondaryColor;
                    Color accent = definition.OverrideColors
                        ? definition.AccentColor : item.typeDefinition.AccentColor;
                    ApplyColors(equipment, primary, secondary, accent);

                }

            }

            SetPreviewLayer(equipment);

        }

        FrameModel();

    }

    private void FrameModel()
    {
        if (previewCamera == null || model == null)
        {
            return;

        }

        bool found = false;
        Bounds bounds = new Bounds(modelAnchor.position + Vector3.up, Vector3.one);
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            if ((! (renderer is MeshRenderer) && ! (renderer is SkinnedMeshRenderer)) ||
                !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;

            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;

            }
            else
            {
                bounds.Encapsulate(renderer.bounds);

            }

        }

        Vector3 center = bounds.center;
        previewCamera.transform.position = center + Vector3.forward * 7f;
        previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
        previewCamera.orthographicSize = Mathf.Max(0.9f, bounds.extents.y * 1.3f,
            bounds.extents.x * 1.3f / Mathf.Max(0.1f, previewCamera.aspect));

    }

    private static Transform FindSocket(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;

            }

        }

        return null;

    }

    private static void SetPreviewLayer(GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = PreviewLayer;

        }

    }

    private static void ApplyColors(GameObject equipment, Color primary, Color secondary, Color accent)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        foreach (Renderer renderer in equipment.GetComponentsInChildren<Renderer>(true))
        {
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                Material material = renderer.sharedMaterials[i];
                if (material == null)
                {
                    continue;

                }

                block.Clear();
                renderer.GetPropertyBlock(block, i);
                if (material.HasProperty("_BaseColor")) block.SetColor("_BaseColor", primary);
                if (material.HasProperty("_Color")) block.SetColor("_Color", primary);
                if (material.HasProperty("_PrimaryColor")) block.SetColor("_PrimaryColor", primary);
                if (material.HasProperty("_SecondaryColor")) block.SetColor("_SecondaryColor", secondary);
                if (material.HasProperty("_AccentColor")) block.SetColor("_AccentColor", accent);
                renderer.SetPropertyBlock(block, i);

            }

        }

    }

    private void OnDestroy()
    {
        if (selectedUnit != null)
        {
            selectedUnit.LoadoutChanged -= RefreshEquipment;

        }

    }

}
