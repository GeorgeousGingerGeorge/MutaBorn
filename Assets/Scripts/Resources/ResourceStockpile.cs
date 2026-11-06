using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResourceEntry
{
    [SerializeField] private string resourceID;
    [Min(0)] [SerializeField] private int quantity;
    [SerializeField] private bool isDiscovered;

    public string ResourceID => resourceID;
    public int Quantity => quantity;
    public bool IsDiscovered => isDiscovered;

    public ResourceEntry(string resourceID)
    {
        this.resourceID = resourceID;
        quantity = 0;
        isDiscovered = false;

    }

    internal void SetQuantity(int newQuantity)
    {
        quantity = Mathf.Max(0, newQuantity);

    }

    internal bool Discover()
    {
        if (isDiscovered)
        {
            return false;

        }

        isDiscovered = true;

        return true;

    }

    internal void ClampQuantity()
    {
        quantity = Mathf.Max(0, quantity);

    }
}

public class ResourceStockpile : MonoBehaviour
{
    public static ResourceStockpile Instance { get; private set; }

    [SerializeField] private ResourceDatabase resourceDatabase;
    [SerializeField] private List<ResourceEntry> resources = new List<ResourceEntry>();

    public ResourceDatabase Database => resourceDatabase;
    public IReadOnlyList<ResourceEntry> Resources => resources;

    public event Action<ResourceEntry> ResourceChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;

        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeResources();

    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;

        }

    }

    private void OnValidate()
    {
        if (resources == null)
        {
            return;

        }

        foreach (ResourceEntry entry in resources)
        {
            entry?.ClampQuantity();

        }

    }

    public void InitializeResources()
    {
        if (resourceDatabase == null)
        {
            Debug.LogError("ResourceStockpile needs a ResourceDatabase assigned.", this);
            return;

        }

        foreach (ResourceData definition in resourceDatabase.Resources)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.ResourceID))
            {
                continue;

            }

            if (GetEntry(definition.ResourceID) == null)
            {
                resources.Add(new ResourceEntry(definition.ResourceID));

            }

        }

    }

    public ResourceEntry GetEntry(string resourceID)
    {
        if (string.IsNullOrWhiteSpace(resourceID))
        {
            return null;

        }

        return resources.Find(entry => entry != null && entry.ResourceID == resourceID);

    }

    public ResourceData GetResourceData(string resourceID)
    {
        return resourceDatabase != null ? resourceDatabase.GetResource(resourceID) : null;

    }

    public int GetQuantity(string resourceID)
    {
        ResourceEntry entry = GetEntry(resourceID);

        return entry != null ? entry.Quantity : 0;

    }

    public bool HasResource(string resourceID, int amount)
    {
        return amount > 0 && GetQuantity(resourceID) >= amount;

    }

    public bool IsResourceDiscovered(string resourceID)
    {
        ResourceEntry entry = GetEntry(resourceID);

        return entry != null && entry.IsDiscovered;

    }

    public bool AddResource(string resourceID, int amount)
    {
        ResourceEntry entry = GetValidEntry(resourceID);

        if (entry == null || amount <= 0 || entry.Quantity > int.MaxValue - amount)
        {
            return false;

        }

        entry.SetQuantity(entry.Quantity + amount);
        ResourceChanged?.Invoke(entry);

        return true;

    }

    public bool RemoveResource(string resourceID, int amount)
    {
        ResourceEntry entry = GetValidEntry(resourceID);

        if (entry == null || amount <= 0 || entry.Quantity < amount)
        {
            return false;

        }

        entry.SetQuantity(entry.Quantity - amount);
        ResourceChanged?.Invoke(entry);

        return true;

    }

    public bool DiscoverResource(string resourceID)
    {
        ResourceEntry entry = GetValidEntry(resourceID);

        if (entry == null || !entry.Discover())
        {
            return false;

        }

        ResourceChanged?.Invoke(entry);

        return true;

    }

    public List<ResourceEntry> GetResourcesByType(ResourceType resourceType, bool includeUndiscovered = false)
    {
        List<ResourceEntry> matchingResources = new List<ResourceEntry>();

        foreach (ResourceEntry entry in resources)
        {
            if (entry == null || (!includeUndiscovered && !entry.IsDiscovered))
            {
                continue;

            }

            ResourceData data = GetResourceData(entry.ResourceID);

            if (data != null && data.ResourceType == resourceType)
            {
                matchingResources.Add(entry);

            }

        }

        return matchingResources;

    }

    private ResourceEntry GetValidEntry(string resourceID)
    {
        if (GetResourceData(resourceID) == null)
        {
            return null;

        }

        ResourceEntry entry = GetEntry(resourceID);

        if (entry == null)
        {
            entry = new ResourceEntry(resourceID);
            resources.Add(entry);

        }

        return entry;

    }
}
