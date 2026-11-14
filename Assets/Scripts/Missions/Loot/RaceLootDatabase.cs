using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RaceLootProfile
{
    public string raceName;
    public MissionLootTable lootTable;
    [Range(0, 100)] public int dropChancePercent = 65;

}

[CreateAssetMenu(fileName = "Race Loot Database", menuName = "MutaBorn/Missions/Race Loot Database")]
public sealed class RaceLootDatabase : ScriptableObject
{
    [SerializeField] private List<RaceLootProfile> profiles = new();

    public bool TryGetLoot(string raceName, out MissionLootTable lootTable, out int dropChancePercent)
    {
        foreach (RaceLootProfile profile in profiles)
        {
            if (profile == null ||
                !string.Equals(profile.raceName, raceName, StringComparison.OrdinalIgnoreCase))
            {
                continue;

            }

            lootTable = profile.lootTable;
            dropChancePercent = Mathf.Clamp(profile.dropChancePercent, 0, 100);

            return true;

        }

        lootTable = null;
        dropChancePercent = 0;

        return false;

    }

}
