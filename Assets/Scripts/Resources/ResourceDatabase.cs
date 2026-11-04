using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResourceDatabase", menuName = "MutaBorn/Resource Database")]
public class ResourceDatabase : ScriptableObject
{
    [SerializeField] private List<ResourceData> resources = new List<ResourceData>();

    public IReadOnlyList<ResourceData> Resources => resources;

    public ResourceData GetResource(string resourceID)
    {
        if (string.IsNullOrWhiteSpace(resourceID))
        {
            return null;

        }

        foreach (ResourceData resource in resources)
        {
            if (resource != null && string.Equals(resource.ResourceID, resourceID, StringComparison.Ordinal))
            {
                return resource;

            }

        }

        return null;

    }

    private void OnValidate()
    {
        HashSet<string> usedIDs = new HashSet<string>(StringComparer.Ordinal);

        foreach (ResourceData resource in resources)
        {
            if (resource == null || string.IsNullOrWhiteSpace(resource.ResourceID))
            {
                continue;

            }

            if (!usedIDs.Add(resource.ResourceID))
            {
                Debug.LogWarning($"ResourceDatabase has a duplicate resource ID: '{resource.ResourceID}'.", this);

            }

        }

    }
}
