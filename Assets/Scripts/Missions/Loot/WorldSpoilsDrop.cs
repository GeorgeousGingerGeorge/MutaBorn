using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Attach to a tree, crate, or other breakable. Call Break() when its existing destruction completes.
public sealed class WorldSpoilsDrop : MonoBehaviour
{
    [SerializeField, Min(1)] private int maximumHealth = 10;
    [SerializeField] private List<WorldLootEntry> drops = new();
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private UnityEvent onBroken = new();

    private int health;
    private bool broken;

    private void Awake()
    {
        health = maximumHealth;

    }

    public void TakeDamage(int amount)
    {
        if (broken || amount <= 0)
        {

            return;

        }

        health = Mathf.Max(0, health - amount);
        if (health == 0)
        {
            Break();

        }

    }

    public void Break()
    {
        if (broken)
        {

            return;

        }

        MissionSpoils spoils = MissionSpoils.Instance;
        if (spoils == null)
        {
            Debug.LogWarning("Cannot collect broken-object loot without MissionSpoils.", this);

            return;

        }

        broken = true;
        List<WorldLootStack> contents = new();
        foreach (WorldLootEntry entry in drops)
        {
            if (entry != null && entry.IsValid)
            {
                contents.Add(new WorldLootStack(entry));

            }

        }

        spoils.Collect(contents);
        MissionLootPopup.Show("Recovered Spoils", contents, "You found nothing.");
        if (visualRoot != null)
        {
            visualRoot.SetActive(false);

        }

        onBroken.Invoke();

    }

}
