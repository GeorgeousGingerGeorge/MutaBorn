using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BrigRansomProfile
{
    public string raceName;
    [Min(0)] public int baseGold = 25;
    [Min(0)] public int goldPerLevel = 15;
    [Min(0)] public int materialRolls = 1;
    public MissionLootTable materialLootTable;

}

// Persistent owner of captured enemy UnitData; no copied inventories or extra roster units.
public sealed class BrigManager : MonoBehaviour
{
    public static BrigManager Instance { get; private set; }

    public const int MaximumSlots = 5;
    public const int StartingSlots = 2;

    [Header("Unlocked Brig space")]
    [SerializeField, Range(StartingSlots, MaximumSlots)] private int unlockedSlots = StartingSlots;
    [Header("Equipment recovery on capture (death default is 5%)")]
    [SerializeField, Range(0, 100)] private int equipmentRecoveryPercent = 30;
    [Header("Recruitment")]
    [SerializeField, Min(0)] private int baseRecruitCost = 60;
    [SerializeField, Min(0)] private int recruitCostPerLevel = 30;
    [SerializeField, Range(0, 100)] private int recruitSuccessPercent = 45;
    [Header("Ransom by race (optional)")]
    [SerializeField] private List<BrigRansomProfile> ransomProfiles = new();

    private readonly List<UnitData> prisoners = new();
    [SerializeField, Min(0)] private int mutationResearchPoints;

    public IReadOnlyList<UnitData> Prisoners => prisoners;
    public int UnlockedSlots => unlockedSlots;
    public int MutationResearchPoints => mutationResearchPoints;
    public bool HasSpace => prisoners.Count < unlockedSlots;
    public event Action Changed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);

            return;

        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SyncResearchSlots();

    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;

        }

    }

    public void SyncResearchSlots()
    {
        int research = ResearchManager.Instance != null
            ? ResearchManager.Instance.BenefitTotal(ResearchBenefitType.BrigSlot) : 0;
        unlockedSlots = Mathf.Clamp(StartingSlots + research, StartingSlots, MaximumSlots);
        Changed?.Invoke();

    }

    public UnitData GetPrisoner(int index)
    {
        return index >= 0 && index < prisoners.Count ? prisoners[index] : null;

    }

    public bool Contains(UnitData unit)
    {
        return unit != null && prisoners.Contains(unit);

    }

    public bool TryCapture(UnitData unit)
    {
        if (unit == null || !HasSpace || Contains(unit) ||
            (UnitRoster.Instance != null && UnitRoster.Instance.TryGetUnit(unit.UnitId, out _)))
        {
            return false;

        }

        prisoners.Add(unit);
        foreach (UnitSlotId slot in UnitLoadoutSlots.All)
        {
            if (UnityEngine.Random.Range(0, 100) < equipmentRecoveryPercent)
            {
                TryTakeItem(unit, slot);

            }

        }

        Changed?.Invoke();

        return true;

    }

    public bool TryTakeItem(UnitData prisoner, UnitSlotId slot)
    {
        ItemStorage storage = ItemStorage.Instance;
        if (!Contains(prisoner) || storage == null)
        {
            return false;

        }

        StoredItem item = null;
        if (prisoner.TryGetEquipmentAtSlot(slot, out EquipmentInstance equipment))
        {
            item = new StoredItem(equipment);

        }
        else if (prisoner.TryGetConsumableAtSlot(slot, out ConsumableInstance consumable))
        {
            item = new StoredItem(consumable);

        }

        if (item == null || !storage.CanStoreTransferredItem(item) ||
            !prisoner.TryLoseItemAfterBattle(slot))
        {
            return false;

        }

        bool stored = storage.TryStoreTransferredItem(item);
        if (!stored)
        {
            Debug.LogError("[Brig] Item was removed but could not be stored. Check ItemStorage ownership.", this);

        }

        Changed?.Invoke();

        return stored;

    }

    private BrigRansomProfile GetProfile(UnitData unit)
    {
        foreach (BrigRansomProfile profile in ransomProfiles)
        {
            if (profile != null && string.Equals(profile.raceName, unit.raceName,
                StringComparison.OrdinalIgnoreCase))
            {
                return profile;

            }

        }

        return null;

    }

    public int RansomGold(UnitData unit)
    {
        BrigRansomProfile profile = unit == null ? null : GetProfile(unit);
        return unit == null ? 0 : profile == null ? 25 + unit.Level * 15 :
            profile.baseGold + unit.Level * profile.goldPerLevel;

    }

    public int RecruitGold(UnitData unit)
    {
        return unit == null ? 0 : baseRecruitCost + unit.Level * recruitCostPerLevel;

    }

    public bool TryRansom(UnitData unit)
    {
        ResourceStockpile bank = ResourceStockpile.Instance;
        if (!Contains(unit) || bank == null || !bank.AddResource("gold", RansomGold(unit)))
        {
            return false;

        }

        bank.DiscoverResource("gold");
        BrigRansomProfile profile = GetProfile(unit);
        if (profile != null && profile.materialLootTable != null)
        {
            for (int i = 0; i < profile.materialRolls; i++)
            {
                WorldLootEntry entry = profile.materialLootTable.Roll();
                if (entry != null && entry.kind == WorldLootKind.Resource)
                {
                    WorldLootStack stack = new WorldLootStack(entry);
                    int quantity = stack.quantity * Mathf.Max(1, Mathf.CeilToInt(unit.Level / 5f));
                    if (bank.AddResource(stack.resourceId, quantity))
                    {
                        bank.DiscoverResource(stack.resourceId);

                    }

                }

            }

        }

        prisoners.Remove(unit);
        Changed?.Invoke();

        return true;

    }

    // An unsuccessful offer consumes the gold, but the prisoner stays in the Brig.
    public bool TryRecruit(UnitData unit, out bool accepted)
    {
        accepted = false;
        ResourceStockpile bank = ResourceStockpile.Instance;
        UnitRoster roster = UnitRoster.Instance;
        if (!Contains(unit) || roster == null || bank == null ||
            !bank.RemoveResource("gold", RecruitGold(unit)))
        {
            return false;

        }

        if (UnityEngine.Random.Range(0, 100) < recruitSuccessPercent)
        {
            accepted = roster.TryRegisterUnit(unit);
            if (accepted)
            {
                prisoners.Remove(unit);

            }

        }

        Changed?.Invoke();

        return true;

    }

    public int MutationValue(UnitData unit)
    {
        if (unit == null)
        {
            return 0;

        }

        int value = Mathf.Max(0, unit.MutationPoints) +
            15 * Mathf.Max(unit.activeMutationNodes != null ? unit.activeMutationNodes.Count : 0,
                unit.activeMutations != null ? unit.activeMutations.Count : 0);
        if (unit.activeMutationNodes != null)
        {
            foreach (ActiveMutationRecord mutation in unit.activeMutationNodes)
            {
                if (mutation != null)
                {
                    value += Mathf.Max(0, mutation.ProgressPoints);

                }

            }

        }

        return value;

    }

    public bool TryConvertToMutationResearch(UnitData unit)
    {
        if (!Contains(unit))
        {
            return false;

        }

        int value = MutationValue(unit);
        if (value <= 0 || mutationResearchPoints > int.MaxValue - value)
        {
            return false;

        }

        mutationResearchPoints += value;
        prisoners.Remove(unit);
        Changed?.Invoke();

        return true;

    }
}
