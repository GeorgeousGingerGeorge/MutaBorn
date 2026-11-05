using System;
using System.Collections.Generic;
using UnityEngine;

// A deployment slot is a mission placement decision, not the identity of a unit.
// Barracks can change which stable UnitId occupies each slot later.
[Serializable]
public sealed class UnitRosterDeployment
{
    public int SlotIndex { get; private set; }
    public string UnitId { get; private set; }

    public UnitRosterDeployment(int slotIndex, string unitId)
    {
        SlotIndex = slotIndex;
        UnitId = unitId;

    }

    internal void SetUnit(string unitId)
    {
        UnitId = unitId;

    }

}

// Persistent runtime owner of PLAYER UnitData. The roster never owns
// UnitController GameObjects or copies equipment/consumable instances.
// Disk snapshots are intentionally deferred to the save-system step.
public sealed class UnitRoster : MonoBehaviour
{
    public static UnitRoster Instance { get; private set; }

    private readonly List<UnitData> units = new List<UnitData>();
    private readonly List<UnitRosterDeployment> deployments = new List<UnitRosterDeployment>();
    private readonly Dictionary<string, UnitController> activeBindings =
        new Dictionary<string, UnitController>(StringComparer.Ordinal);

    public IReadOnlyList<UnitData> Units => units;
    public IReadOnlyList<UnitRosterDeployment> Deployments => deployments;
    public event Action RosterChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);

            return;

        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;

        }

    }

    public bool TryGetUnit(string unitId, out UnitData unit)
    {
        foreach (UnitData candidate in units)
        {
            if (candidate != null &&
                string.Equals(candidate.UnitId, unitId, StringComparison.Ordinal))
            {
                unit = candidate;

                return true;

            }

        }

        unit = null;

        return false;

    }

    public bool TryCreateNewUnit(string characterName, DynamicStats race, out UnitData unit)
    {
        unit = null;
        if (race == null)
        {
            return false;

        }

        // This path is for NEW recruits only. Never use it to restore a save.
        UnitData generated = UnitData.CreateNewUnit(characterName, race);
        if (!TryRegisterUnit(generated))
        {
            return false;

        }

        unit = generated;

        return true;

    }

    // Attach an existing data model WITHOUT calling the creation factory.
    // This also validates unique item ownership at initial registration.
    public bool TryRegisterUnit(UnitData unit)
    {
        if (!CanRegisterUnit(unit))
        {
            return false;

        }

        units.Add(unit);
        RosterChanged?.Invoke();

        return true;

    }

    private bool CanRegisterUnit(UnitData unit)
    {
        if (unit == null || string.IsNullOrWhiteSpace(unit.UnitId) ||
            unit.RaceDefinition == null || TryGetUnit(unit.UnitId, out UnitData duplicate))
        {
            return false;

        }

        HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (EquipmentInstance item in unit.equippedItems)
        {
            if (string.IsNullOrWhiteSpace(item.instanceId) || !itemIds.Add(item.instanceId))
            {
                return false;

            }

        }

        foreach (ConsumableInstance item in unit.carriedConsumables)
        {
            if (string.IsNullOrWhiteSpace(item.instanceId) || !itemIds.Add(item.instanceId))
            {
                return false;

            }

        }

        foreach (UnitData existing in units)
        {
            foreach (EquipmentInstance item in existing.equippedItems)
            {
                if (itemIds.Contains(item.instanceId))
                {
                    return false;

                }

            }

            foreach (ConsumableInstance item in existing.carriedConsumables)
            {
                if (itemIds.Contains(item.instanceId))
                {
                    return false;

                }

            }

        }

        // ItemStorage owns only UNASSIGNED items; no instance may exist there
        // and inside a roster unit at the same time.
        ItemStorage storage = ItemStorage.Instance;
        if (storage != null)
        {
            foreach (StoredItem item in storage.Items)
            {
                if (item != null && itemIds.Contains(item.InstanceId))
                {
                    return false;

                }

            }

        }

        return true;

    }

    public bool TryGetDeploymentUnit(int slotIndex, out UnitData unit)
    {
        foreach (UnitRosterDeployment deployment in deployments)
        {
            if (deployment.SlotIndex == slotIndex)
            {
                return TryGetUnit(deployment.UnitId, out unit);

            }

        }

        unit = null;

        return false;

    }

    public bool TryAssignDeploymentSlot(int slotIndex, string unitId)
    {
        if (slotIndex < 0 || !TryGetUnit(unitId, out UnitData unit))
        {
            return false;

        }

        foreach (UnitRosterDeployment deployment in deployments)
        {
            if (deployment.SlotIndex != slotIndex &&
                string.Equals(deployment.UnitId, unitId, StringComparison.Ordinal))
            {
                return false;

            }

        }

        foreach (UnitRosterDeployment deployment in deployments)
        {
            if (deployment.SlotIndex == slotIndex)
            {
                if (IsUnitBound(deployment.UnitId) || IsUnitBound(unitId))
                {
                    return false;

                }

                deployment.SetUnit(unitId);
                RosterChanged?.Invoke();

                return true;

            }

        }

        if (IsUnitBound(unitId))
        {
            return false;

        }

        deployments.Add(new UnitRosterDeployment(slotIndex, unitId));
        RosterChanged?.Invoke();

        return true;

    }

    public bool TryUnassignDeploymentSlot(int slotIndex)
    {
        for (int i = 0; i < deployments.Count; i++)
        {
            if (deployments[i].SlotIndex != slotIndex || IsUnitBound(deployments[i].UnitId))
            {
                continue;

            }

            deployments.RemoveAt(i);
            RosterChanged?.Invoke();

            return true;

        }

        return false;

    }

    // Called by a roster-mode scene wrapper BEFORE GridManager registers it.
    public bool TryBindDeploymentUnit(int slotIndex, UnitController wrapper, out UnitData unit)
    {
        unit = null;
        if (wrapper == null || !TryGetDeploymentUnit(slotIndex, out UnitData candidate) ||
            IsUnitBound(candidate.UnitId))
        {
            return false;

        }

        activeBindings[candidate.UnitId] = wrapper;
        unit = candidate;

        return true;

    }

    // Temporary test bootstrap: initialize a scene unit ONCE through the
    // existing starter path, then enroll that exact model and actual items.
    public bool TryRegisterAndBindNewUnit(UnitData unit, int slotIndex, UnitController wrapper)
    {
        if (slotIndex < 0 || wrapper == null ||
            TryGetDeploymentUnit(slotIndex, out UnitData assigned) ||
            !CanRegisterUnit(unit))
        {
            return false;

        }

        foreach (UnitRosterDeployment deployment in deployments)
        {
            if (deployment.SlotIndex == slotIndex)
            {
                return false;

            }

        }

        units.Add(unit);
        deployments.Add(new UnitRosterDeployment(slotIndex, unit.UnitId));
        activeBindings.Add(unit.UnitId, wrapper);
        RosterChanged?.Invoke();

        return true;

    }

    private bool IsUnitBound(string unitId)
    {
        if (!activeBindings.TryGetValue(unitId, out UnitController wrapper))
        {
            return false;

        }

        if (wrapper == null)
        {
            activeBindings.Remove(unitId);

            return false;

        }

        return true;

    }

    public void ReleaseSceneBinding(string unitId, UnitController wrapper)
    {
        if (activeBindings.TryGetValue(unitId, out UnitController current) &&
            ReferenceEquals(current, wrapper))
        {
            activeBindings.Remove(unitId);

        }

    }

    // New Game coordination will also reset the economy/storage/campaign.
    // Never clear an active roster unit from under its scene wrapper.
    public bool TryClearForNewGame()
    {
        foreach (UnitData unit in units)
        {
            if (IsUnitBound(unit.UnitId))
            {
                return false;

            }

        }

        units.Clear();
        deployments.Clear();
        activeBindings.Clear();
        RosterChanged?.Invoke();

        return true;

    }

    [ContextMenu("Debug / Log Roster")]
    private void DebugLogRoster()
    {
        Debug.Log($"[UnitRoster] {units.Count} unit(s), {deployments.Count} deployment slot(s).", this);
        foreach (UnitRosterDeployment deployment in deployments)
        {
            if (TryGetUnit(deployment.UnitId, out UnitData unit))
            {
                Debug.Log($"[UnitRoster] Slot {deployment.SlotIndex}: {unit.characterName} " +
                    $"[{unit.UnitId}] - {unit.equippedItems.Count} equipment, " +
                    $"{unit.carriedConsumables.Count} consumables.", this);

                foreach (EquipmentInstance equipment in unit.equippedItems)
                {
                    Debug.Log($"[UnitRoster] Gear {equipment.instanceId} / {equipment.itemName} / {equipment.rarity}", this);

                }

                foreach (ConsumableInstance consumable in unit.carriedConsumables)
                {
                    Debug.Log($"[UnitRoster] Consumable {consumable.instanceId} / {consumable.remainingUses} uses", this);

                }

            }

        }

    }

}
