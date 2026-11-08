using UnityEngine;

public sealed class ResourcesEditorPreviewRows : MonoBehaviour
{
    private void Awake()
    {
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Resource Preview - "))
            {
                child.gameObject.SetActive(false);

            }

        }

    }
}
