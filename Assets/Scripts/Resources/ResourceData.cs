using System;
using UnityEngine;

public enum ResourceType
{
    Currency,
    Basic,
    Advanced,
    Complex,
    Special

}

[Serializable]
public class ResourceData
{
    [SerializeField] private string resourceID;
    [SerializeField] private string resourceName;

    [TextArea(3, 5)]
    [SerializeField] private string description;

    [SerializeField] private Sprite icon;
    [SerializeField] private ResourceType resourceType;

    public string ResourceID => resourceID;
    public string ResourceName => resourceName;
    public string Description => description;
    public Sprite Icon => icon;
    public ResourceType ResourceType => resourceType;

}
