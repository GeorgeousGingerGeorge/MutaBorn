using System;
using UnityEngine;

// Optional Play Mode testing helper. NEVER grants gear automatically on New/Load.
// Delete or disable this component when real starting inventory/rewards are connected.
[RequireComponent(typeof(ItemStorage))]
public sealed class BarracksSampleAccessorySeeder : MonoBehaviour
{
    [SerializeField] private EquipmentData[] sampleAccessories = new EquipmentData[0];
    private bool alreadySeeded;

    [ContextMenu("Test / Add Sample Accessories Once")]
    public void AddSampleAccessoriesOnce()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Accessory samples can only be granted in Play Mode.", this);

            return;

        }

        if (alreadySeeded)
        {
            Debug.Log("[Barracks] Sample accessories were already added this session.", this);

            return;

        }

        ItemStorage storage = GetComponent<ItemStorage>();
        if (storage == null || ItemStorage.Instance != storage)
        {
            Debug.LogWarning("[Barracks] The active ItemStorage must own this seeder.", this);

            return;

        }

        int added = 0;
        foreach (EquipmentData definition in sampleAccessories)
        {
            if (definition == null)
            {
                continue;

            }

            EquipmentInstance item = EquipmentGenerator.CreateEquipment(definition);
            if (storage.TryStoreEquipment(item))
            {
                added++;

            }

        }

        alreadySeeded = true;
        Debug.Log("[Barracks] Added " + added + " sample accessory instances to ItemStorage.", this);

    }
}
