using UnityEngine;
using System.Collections.Generic;
using System;
using MutaBorn.Gameplay;



public class UnitData
{

    [Header("Character Information- Name/RaceName")]

    // Stable identity for the permanent player roster; assigned once at creation.
    // Later save restore will reconstruct UnitData with its saved ID.
    public string UnitId { get; private set; }

    public string characterName;
    public event Action IdentityChanged;

    public bool TryRename(string proposedName)
    {
        if (string.IsNullOrWhiteSpace(proposedName))
        {
            return false;

        }

        string cleaned = proposedName.Trim();
        if (cleaned.Length > 40)
        {
            return false;

        }

        if (string.Equals(characterName, cleaned, StringComparison.Ordinal))
        {
            return true;

        }

        characterName = cleaned;
        IdentityChanged?.Invoke();

        return true;

    }

    public string raceName;
    DynamicStats race;
    public DynamicStats RaceDefinition => race;

    [Header("Character Information- startingStats")]
    private int startingStrength;
    private int startingDexterity;
    private int startingVitality;
    private int startingConstitution;
    private int startingPerception;
    private int startingIntelligence;

    [Header("Character Inforamtion- CoreStats/LevelUpStats")]

    public int leveledStrength { get; private set; }
    public int leveledDexterity { get; private set; }
    public int leveledVitality { get; private set; }
    public int leveledConstitution { get; private set; }
    public int leveledPerception { get; private set; }
    public int leveledIntelligence { get; private set; }

    [Header("Character Information- MutationBonus")]
    public int mutationStrength { get; set; }
    public int mutationDexterity { get; set; }
    public int mutationVitality { get; set; }
    public int mutationConstitution { get; set; }
    public int mutationPerception { get; set; }
    public int mutationIntelligence { get; set; }

    [Header("Character Information- EquipmentBonus")]
    public int equipmentStrength { get; set; }
    public int equipmentDexterity { get; set; }
    public int equipmentVitality { get; set; }
    public int equipmentConstitution { get; set; }
    public int equipmentPerception { get; set; }
    public int equipmentIntelligence { get; set; }

    [Header("Character Information- Total Core Stats")]
    public int totalStrength => CalculateModifiedStat(StatIdentifier.Strength, leveledStrength + startingStrength + mutationStrength + equipmentStrength);
    public int totalDexterity => CalculateModifiedStat(StatIdentifier.Dexterity, leveledDexterity + startingDexterity + mutationDexterity + equipmentDexterity);
    public int totalVitality => CalculateModifiedStat(StatIdentifier.Vitality, leveledVitality + startingVitality + mutationVitality + equipmentVitality);
    public int totalConstitution => CalculateModifiedStat(StatIdentifier.Constitution, leveledConstitution + startingConstitution + mutationConstitution + equipmentConstitution);
    public int totalPerception => CalculateModifiedStat(StatIdentifier.Perception, leveledPerception + startingPerception + mutationPerception + equipmentPerception);
    public int totalIntelligence => CalculateModifiedStat(StatIdentifier.Intelligence, leveledIntelligence + startingIntelligence + mutationIntelligence + equipmentIntelligence);

    [Header("Character Information- Equipped Mutations/Gear Plus Tracker")]
    // Legacy mutation scripts remain for pre-existing scenes until migration is verified.
    public Dictionary<BodySlots, BaseMutation> activeMutations = new Dictionary<BodySlots, BaseMutation>();
    // Fixed nodes are stored by ID, not body slot. Multiple families may share a slot.
    public List<ActiveMutationRecord> activeMutationNodes = new List<ActiveMutationRecord>();
    public Dictionary<BodySlots, Dictionary<MutationStrain, Dictionary<MutationID, MutationProgress>>> mutationTracker
        = new Dictionary<BodySlots, Dictionary<MutationStrain, Dictionary<MutationID, MutationProgress>>>();
    public List<EquipmentInstance> equippedItems = new List<EquipmentInstance>();

    // The four physical slots are separate from progression availability.
    public const int ConsumableCapacity = 4;
    public List<ConsumableInstance> carriedConsumables = new List<ConsumableInstance>();

    // Slot assignments refer to the actual instances above; they are not copies.
    private readonly List<UnitLoadoutSlotState> loadoutSlots = new List<UnitLoadoutSlotState>();
    public IReadOnlyList<UnitLoadoutSlotState> LoadoutSlots
    {
        get
        {
            EnsureLoadoutSlots();

            return loadoutSlots;

        }

    }

    public event Action LoadoutChanged;

    [Header("Character Information- Current Status (mutationBonus)")]
    public int mutationHealth;
    public int mutationMana;
    public int mutationActionPoints;
    public int mutationMeleeDamage;
    public int mutationRangeDamage;
    public int mutationMagicDamage;
    public int mutationArmor;
    public int mutationWill;
    
    [Header("Character Information- Current Status (equipmentBonus)")]
    public int equipmentHealth;
    public int equipmentMana;
    public int equipmentActionPoints;
    public int equipmentMeleeDamage;
    public int equipmentRangeDamage;
    public int equipmentMagicDamage;
    public int equipmentArmor;
    public int equipmentWill;

    [Header("Character Information- Current Status")]
    public int maxHealth { get; private set; }
    public int currentHealth { get; private set; }
    public int maxMana { get; private set; }
    public int currentMana { get; private set; }
    public int maxActionPoints { get; private set; }
    public int currentActionPoints { get; private set; }
    public int meleeDamage { get; private set; }
    public int rangeDamage { get; private set; }
    public int magicDamage { get; private set; }
    public int armor { get; private set; }
    public int accuracyBonus { get; private set; }
    public int movementPenalty { get; private set; }
    public int maxLineOfSight { get; private set; }
    public int currentLineOfSight { get; private set; }
    public int maxFieldOfView { get; private set; }
    public int currentFieldOfView { get; private set; }

    // Ward and damage resistances are derived from equipped gear, not cached
    // separately. Future stat modifiers can extend these accessors.
    public int ward
    {
        get
        {
            long total = 0;

            foreach (EquipmentInstance equipment in equippedItems)
            {
                total += Math.Max(0, EquipmentGenerator.GetEffectiveStats(equipment, this).ward);

            }

            return CalculateModifiedStat(StatIdentifier.MagicDefense, Math.Min(int.MaxValue, total));

        }

    }

    public float GetDamageResistancePercent(DamageType damageType)
    {
        double total = 0d;

        foreach (EquipmentInstance equipment in equippedItems)
        {
            if (equipment.resistances == null)
            {
                continue;

            }

            foreach (DamageResistance resistance in equipment.resistances)
            {
                if (resistance.Type != damageType || float.IsNaN(resistance.Percent) ||
                    float.IsInfinity(resistance.Percent))
                {
                    continue;

                }

                total += resistance.Percent;

            }

        }

        return (float)Math.Max(-float.MaxValue, Math.Min(float.MaxValue, total));

    }
    public int will { get; private set; }

    // Applied effects live on the unit, keyed by their exact runtime grant.
    // Existing mutation and equipment bonuses remain the base values.
    private sealed class RuntimeStatBonus
    {
        public object Source;
        public StatIdentifier Stat;
        public StatModifierMode Mode;
        public double Value;

    }

    private readonly List<RuntimeStatBonus> runtimeStatBonuses = new List<RuntimeStatBonus>();

    // A single status can modify several stats while retaining one lifetime/source.
    public readonly struct StatModifierEntry
    {
        public readonly StatIdentifier Stat;
        public readonly StatModifierMode Mode;
        public readonly double Value;

        public StatModifierEntry(StatIdentifier stat, StatModifierMode mode, double value)
        {
            Stat = stat;
            Mode = mode;
            Value = value;

        }

    }

    public bool TryReplaceStatModifiers(object source, IReadOnlyList<StatModifierEntry> modifiers)
    {
        if (source == null || modifiers == null)
        {
            return false;

        }

        foreach (StatModifierEntry modifier in modifiers)
        {
            if (modifier.Stat == StatIdentifier.None ||
                !Enum.IsDefined(typeof(StatIdentifier), modifier.Stat) ||
                !Enum.IsDefined(typeof(StatModifierMode), modifier.Mode) ||
                double.IsNaN(modifier.Value) || double.IsInfinity(modifier.Value))
            {
                return false;

            }

        }

        bool changed = false;
        for (int i = runtimeStatBonuses.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(runtimeStatBonuses[i].Source, source))
            {
                runtimeStatBonuses.RemoveAt(i);
                changed = true;

            }

        }

        foreach (StatModifierEntry modifier in modifiers)
        {
            if (modifier.Value == 0d)
            {
                continue;

            }

            runtimeStatBonuses.Add(new RuntimeStatBonus
            {
                Source = source,
                Stat = modifier.Stat,
                Mode = modifier.Mode,
                Value = modifier.Value

            });
            changed = true;

        }

        if (changed)
        {
            RecalculateStats();

        }

        return true;

    }

    // Movement pathfinding uses this to scale the usable movement budget;
    // spending AP uses the reciprocal, so a -10% penalty costs ~11% more AP.
    public float MovementEfficiency => Mathf.Max(0.1f,
        CalculateModifiedStat(StatIdentifier.MovementEfficiency, 100d) / 100f);


    public bool TryApplyStatModifier(object source, StatIdentifier stat,
        StatModifierMode mode, double value)
    {
        if (source == null || stat == StatIdentifier.None ||
            !Enum.IsDefined(typeof(StatIdentifier), stat) ||
            !Enum.IsDefined(typeof(StatModifierMode), mode) ||
            double.IsNaN(value) || double.IsInfinity(value))
        {
            return false;

        }

        // A re-trigger updates this grant; it cannot silently stack itself.
        for (int i = runtimeStatBonuses.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(runtimeStatBonuses[i].Source, source))
            {
                runtimeStatBonuses.RemoveAt(i);

            }

        }

        runtimeStatBonuses.Add(new RuntimeStatBonus
        {
            Source = source,
            Stat = stat,
            Mode = mode,
            Value = value

        });
        RecalculateStats();

        return true;

    }

    public bool RemoveStatModifier(object source)
    {
        bool removed = false;
        for (int i = runtimeStatBonuses.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(runtimeStatBonuses[i].Source, source))
            {
                runtimeStatBonuses.RemoveAt(i);
                removed = true;

            }

        }

        if (removed)
        {
            RecalculateStats();

        }

        return removed;

    }

    private int CalculateModifiedStat(StatIdentifier stat, double baseValue)
    {
        double flat = 0d;
        double percent = 0d;
        foreach (RuntimeStatBonus bonus in runtimeStatBonuses)
        {
            if (bonus.Stat != stat)
            {
                continue;

            }

            if (bonus.Mode == StatModifierMode.Flat)
            {
                flat += bonus.Value;

            }
            else
            {
                percent += bonus.Value;

            }

        }

        // Flat bonuses first, additive percentages second; round once.
        double result = (baseValue + flat) * (1d + percent / 100d);
        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            return 0;

        }

        return result >= int.MaxValue ? int.MaxValue :
            result <= int.MinValue ? int.MinValue : (int)Math.Ceiling(result);

    }

    //Local StatCalculations for the CurrentStatus
    public void CalcMaxHealth(DynamicStats race)
    {
        maxHealth = Mathf.Max(1, CalculateModifiedStat(StatIdentifier.MaxHealth, (float)race.baseHealth + ((float)totalVitality * ((float)race.vitalityHealthBonus * race.vitalityModifier))
            + (float)mutationHealth + (float)equipmentHealth));
    }
    public void CalcMaxMana(DynamicStats race)
    {
        maxMana = Mathf.Max(0, CalculateModifiedStat(StatIdentifier.MaxMana, (float)race.baseMana + ((float)totalIntelligence * ((float)race.intelligenceManaBonus * race.intelligenceModifier))
            + (float) mutationMana + (float)equipmentMana));

    }
    public void CalcMaxActionPoints(DynamicStats race)
    {
        maxActionPoints = Mathf.Max(0, CalculateModifiedStat(StatIdentifier.MaxActionPoints, (float)race.actionPoints + ((float)totalVitality * ((float)race.vitalityActionPointBonus * race.vitalityModifier))
            + (float)mutationActionPoints + (float)equipmentActionPoints));

    }
    public void CalcMeleeDamage(DynamicStats race) 
    {
        meleeDamage = Mathf.Max(0, CalculateModifiedStat(StatIdentifier.MeleeDamage, (float)race.meleeDamage + ((float)totalStrength * ((float)race.strengthMeleeDMGBonus * race.strengthModifier))
        + ((float)totalDexterity * ((float)race.dexterityMeleeDMGBonus * race.dexterityModifier)) + (float)mutationMeleeDamage + (float)equipmentMeleeDamage));
    }
    public void CalcRangeDamage(DynamicStats race) 
    {
        rangeDamage = Mathf.Max(0, CalculateModifiedStat(StatIdentifier.RangedDamage, (float)race.rangeDamage + ((float)totalPerception * ((float)race.perceptionRangeDMGBonus * race.perceptionModifier))
        + ((float)totalDexterity * ((float)race.dexterityRangeDMGBonus * race.perceptionModifier)) + (float)mutationRangeDamage + (float)equipmentRangeDamage));
    }
    public void CalcMagicDamage(DynamicStats race) 
    {
        magicDamage = Mathf.Max(0, CalculateModifiedStat(StatIdentifier.MagicDamage, (float)race.magicDamage + ((float)totalIntelligence * ((float)race.intelligenceMagicDMGBonus * race.intelligenceModifier))
     + (float)mutationMagicDamage + (float)equipmentMagicDamage));
    }

    public void CalcArmor(DynamicStats race) 
    {
        armor = Mathf.Max(0, CalculateModifiedStat(StatIdentifier.PhysicalDefense, (float)race.armorDef + ((float)totalConstitution * ((float)race.constitutionArmorBonus * race.constitutionModifier))
            + (float)mutationArmor + (float)equipmentArmor));
    }
    public void CalcWill(DynamicStats race) 
    {
        will = Mathf.Max(0, CalculateModifiedStat(StatIdentifier.Will, (double)race.will + mutationWill + equipmentWill));
    }

    //Takes both negative/positive values for health
    //if negative you lose the values. if positive you restore the value
    public void ModifyHealth(int amount) 
    {
        int before = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        if (currentHealth > before)
        {
            CombatFloatingFeedback.ReportResourceGain(this, ResourceIdentifier.Health, currentHealth - before);

        }

    }
    public void ModifyMana(int amount)
    {
        int before = currentMana;
        currentMana = Mathf.Clamp(currentMana + amount, 0, maxMana);
        if (currentMana > before)
        {
            CombatFloatingFeedback.ReportResourceGain(this, ResourceIdentifier.Mana, currentMana - before);

        }

    }
    public void ModifyActionPoints(int amount) 
    {
        int before = currentActionPoints;
        currentActionPoints = Mathf.Clamp(currentActionPoints + amount, 0, maxActionPoints);
        if (currentActionPoints > before)
        {
            CombatFloatingFeedback.ReportResourceGain(this, ResourceIdentifier.ActionPoints, currentActionPoints - before);

        }

    }
    public bool TryRestoreSavedResources(int health, int mana, int actionPoints)
    {
        // Validate saved values against current maximums.
        if (health < 0 || health > maxHealth || mana < 0 || mana > maxMana || actionPoints < 0 || actionPoints > maxActionPoints)
        {
            Debug.LogWarning("Saved resources exceed valid unit limits.");
            return false;
        }

        // Direct restoration avoids combat feedback.
        currentHealth = health;
        currentMana = mana;
        currentActionPoints = actionPoints;

        return true;
    }
    public void ModifyFieldOfView(int amount) 
    {
        currentFieldOfView = Mathf.Clamp(currentFieldOfView + amount, 45, maxFieldOfView);

    }
    public void ModifyLineOfSight(int amount) 
    {
        currentLineOfSight = Mathf.Clamp(currentLineOfSight + amount, 2, maxLineOfSight);

    }

    public void ApplyMutationBonuses(MutationBonus bonus, int multiplier, bool recalculate = true) 
    {
        if (bonus == null)
            return;

        //CoreBonus
        mutationStrength += bonus.strength * multiplier;
        mutationDexterity += bonus.dexterity * multiplier;
        mutationVitality += bonus.vitality * multiplier;
        mutationConstitution += bonus.constitution * multiplier;
        mutationPerception += bonus.perception * multiplier;
        mutationIntelligence += bonus.intelligence * multiplier;

        //DerivedBonus 
        mutationHealth += bonus.health * multiplier;
        mutationMana += bonus.mana * multiplier;
        mutationActionPoints += bonus.actionPoints * multiplier;
        mutationMeleeDamage += bonus.meleeDamage * multiplier;
        mutationRangeDamage += bonus.rangeDamage * multiplier;
        mutationMagicDamage += bonus.magicDamage * multiplier;
        mutationArmor += bonus.armor * multiplier;
        mutationWill += bonus.will * multiplier;


        if (recalculate)
        {
            RecalculateStats();

        }

    }
    public EquipmentStatValues GetEffectiveEquipmentStats(EquipmentInstance item)
    {
        return EquipmentGenerator.GetEffectiveStats(item, this);
    }

    // Preserves the existing call-site API; equippedItems is the single source of truth.
    public void ApplyEquipmentBonuses(EquipmentInstance item, int multiplier)
    {
        RecalculateStats();
    }

    public void RecalculateStats()
    {
        equipmentMeleeDamage = 0;
        equipmentRangeDamage = 0;
        equipmentMagicDamage = 0;
        equipmentArmor = 0;
        accuracyBonus = 0;
        movementPenalty = 0;
        foreach (EquipmentInstance item in equippedItems)
        {
            EquipmentStatValues effective = GetEffectiveEquipmentStats(item);
            equipmentMeleeDamage += effective.meleeDamage;
            equipmentRangeDamage += effective.rangeDamage;
            equipmentMagicDamage += effective.magicDamage;
            equipmentArmor += effective.armor;
            accuracyBonus += effective.accuracy;
            movementPenalty += effective.movementPenalty;
        }

        CalcMaxHealth(race);
        CalcMaxMana(race);
        CalcMaxActionPoints(race);
        CalcMeleeDamage(race);
        CalcRangeDamage(race);
        CalcMagicDamage(race);
        CalcArmor(race);
        CalcWill(race);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        currentActionPoints = Mathf.Clamp(currentActionPoints, 0, maxActionPoints);
    }

    private void EnsureLoadoutSlots()
    {
        foreach (UnitSlotId slot in UnitLoadoutSlots.All)
        {
            if (loadoutSlots.Exists(entry => entry != null && entry.Slot == slot))
            {
                continue;

            }

            loadoutSlots.Add(new UnitLoadoutSlotState(slot));

        }

    }

    public UnitLoadoutSlotState GetSlot(UnitSlotId slot)
    {
        if (!UnitLoadoutSlots.IsValid(slot))
        {
            return null;

        }

        EnsureLoadoutSlots();

        return loadoutSlots.Find(entry => entry != null && entry.Slot == slot);

    }

    private bool TryGetEquipmentInstance(string instanceId, out EquipmentInstance item)
    {
        foreach (EquipmentInstance equipped in equippedItems)
        {
            if (string.Equals(equipped.instanceId, instanceId, StringComparison.Ordinal))
            {
                item = equipped;

                return true;

            }

        }

        item = default(EquipmentInstance);

        return false;

    }

    private bool IsTwoHandedBlocked(UnitSlotId slot)
    {
        UnitSlotId otherHand = UnitLoadoutSlots.OtherHand(slot);
        if (otherHand == UnitSlotId.None)
        {
            return false;

        }

        UnitLoadoutSlotState other = GetSlot(otherHand);
        return other != null && !string.IsNullOrEmpty(other.ItemInstanceId) &&
            TryGetEquipmentInstance(other.ItemInstanceId, out EquipmentInstance item) &&
            item.template != null && item.template.RequiresBothHands;

    }

    public UnitSlotBlockReason GetSlotBlockReason(UnitSlotId slot)
    {
        UnitLoadoutSlotState state = GetSlot(slot);
        if (state == null)
        {
            return UnitSlotBlockReason.ProgressionLocked;

        }

        if (state.OccupyingMutation != MutationID.none)
        {
            return UnitSlotBlockReason.MutationOccupied;

        }

        if (IsTwoHandedBlocked(slot))
        {
            return UnitSlotBlockReason.TwoHandedWeapon;

        }

        if (!state.ProgressionUnlocked)
        {
            return UnitSlotBlockReason.ProgressionLocked;

        }

        if (state.PlayerLocked)
        {
            return UnitSlotBlockReason.PlayerLocked;

        }

        return string.IsNullOrEmpty(state.ItemInstanceId)
            ? UnitSlotBlockReason.None : UnitSlotBlockReason.ItemOccupied;

    }

    public bool TrySetPlayerSlotLock(UnitSlotId slot, bool locked)
    {
        UnitLoadoutSlotState state = GetSlot(slot);
        if (state == null || state.PlayerLocked == locked)
        {
            return false;

        }

        state.SetPlayerLock(locked);
        LoadoutChanged?.Invoke();

        return true;

    }

    // Level-up and research systems grant permanent slot unlocks later.
    public bool TryUnlockProgressionSlot(UnitSlotId slot)
    {
        UnitLoadoutSlotState state = GetSlot(slot);
        if (state == null || state.ProgressionUnlocked)
        {
            return false;

        }

        state.SetProgressionUnlocked();
        LoadoutChanged?.Invoke();

        return true;

    }

    private bool TryChangeMutationSlotOccupancy(MutationID previousId, MutationID nextId,
        IReadOnlyList<UnitSlotId> nextSlots)
    {
        EnsureLoadoutSlots();
        HashSet<UnitSlotId> requested = new HashSet<UnitSlotId>();
        if (nextSlots != null)
        {
            foreach (UnitSlotId slot in nextSlots)
            {
                if (!UnitLoadoutSlots.IsValid(slot) || !requested.Add(slot))
                {
                    return false;

                }

                UnitLoadoutSlotState state = GetSlot(slot);
                if (state == null || !string.IsNullOrEmpty(state.ItemInstanceId) ||
                    (state.OccupyingMutation != MutationID.none &&
                     state.OccupyingMutation != previousId) || IsTwoHandedBlocked(slot))
                {
                    return false;

                }

            }

        }

        foreach (UnitLoadoutSlotState state in loadoutSlots)
        {
            if (previousId != MutationID.none && state.OccupyingMutation == previousId)
            {
                state.SetMutation(MutationID.none);

            }

        }

        foreach (UnitSlotId slot in requested)
        {
            GetSlot(slot).SetMutation(nextId);

        }

        // Callers notify listeners only after their mutation state is committed.
        return true;

    }

    // Future mutation integrations can occupy a free slot without creating an item.
    // Existing equipped gear must first be safely transferred to storage.
    public bool TryOccupySlotWithMutation(UnitSlotId slot, MutationID id)
    {
        if (id == MutationID.none)
        {
            return false;

        }

        if (!TryChangeMutationSlotOccupancy(MutationID.none, id,
            new UnitSlotId[] { slot }))
        {
            return false;

        }

        LoadoutChanged?.Invoke();

        return true;

    }

    public bool TryReleaseMutationSlot(UnitSlotId slot, MutationID id)
    {
        UnitLoadoutSlotState state = GetSlot(slot);
        if (id == MutationID.none || state == null || state.OccupyingMutation != id)
        {
            return false;

        }

        state.SetMutation(MutationID.none);
        LoadoutChanged?.Invoke();

        return true;

    }

    public bool TryGetEquipmentAtSlot(UnitSlotId slot, out EquipmentInstance item)
    {
        UnitLoadoutSlotState state = GetSlot(slot);
        if (state != null && !string.IsNullOrEmpty(state.ItemInstanceId))
        {
            return TryGetEquipmentInstance(state.ItemInstanceId, out item);

        }

        item = default(EquipmentInstance);

        return false;

    }

    public bool CanEquipInstance(EquipmentInstance item, UnitSlotId slot)
    {
        if (item.template == null || !UnitLoadoutSlots.IsValid(slot) ||
            !UnitLoadoutSlots.Accepts(slot, item.template.EquipmentFit) ||
            GetSlotBlockReason(slot) != UnitSlotBlockReason.None)
        {
            return false;

        }

        if (!string.IsNullOrWhiteSpace(item.instanceId) &&
            (TryGetEquipmentInstance(item.instanceId, out EquipmentInstance existing) ||
             carriedConsumables.Exists(consumable => consumable.instanceId == item.instanceId)))
        {
            return false;

        }

        return !item.template.RequiresBothHands ||
            (UnitLoadoutSlots.IsHand(slot) &&
             GetSlotBlockReason(UnitLoadoutSlots.OtherHand(slot)) == UnitSlotBlockReason.None);

    }

    public bool TryEquipInstance(EquipmentInstance item, UnitSlotId slot)
    {
        if (!CanEquipInstance(item, slot))
        {
            return false;

        }

        if (string.IsNullOrWhiteSpace(item.instanceId))
        {
            item.instanceId = Guid.NewGuid().ToString("N");

        }

        GetSlot(slot).SetItem(item.instanceId);
        equippedItems.Add(item);
        ApplyEquipmentBonuses(item, 1);
        LoadoutChanged?.Invoke();

        return true;

    }

    public bool TryUnequipInstance(UnitSlotId slot, out EquipmentInstance item)
    {
        item = default(EquipmentInstance);
        UnitLoadoutSlotState state = GetSlot(slot);
        if (state == null || state.PlayerLocked || state.OccupyingMutation != MutationID.none ||
            !state.ProgressionUnlocked || string.IsNullOrEmpty(state.ItemInstanceId))
        {
            return false;

        }

        for (int i = 0; i < equippedItems.Count; i++)
        {
            if (equippedItems[i].instanceId != state.ItemInstanceId)
            {
                continue;

            }

            item = equippedItems[i];
            equippedItems.RemoveAt(i);
            state.SetItem(string.Empty);
            ApplyEquipmentBonuses(item, -1);
            LoadoutChanged?.Invoke();

            return true;

        }

        return false;

    }

    // Legacy test-scene helper: generate fresh starting equipment only, not storage transfers.
    public bool TryEquipStartingItem(EquipmentData template)
    {
        return TryEquipStartingItem(template, out _);
    }

    public bool TryEquipStartingItem(EquipmentData template, out UnitSlotId equippedSlot)
    {
        equippedSlot = UnitSlotId.None;

        if (template == null)
        {
            return false;
        }

        EquipmentInstance item = EquipmentGenerator.CreateEquipment(template);

        foreach (UnitSlotId slot in UnitLoadoutSlots.All)
        {
            if (CanEquipInstance(item, slot))
            {
                bool success = TryEquipInstance(item, slot);

                if (success)
                {
                    equippedSlot = slot;
                }

                return success;
            }
        }

        return false;
    }

    public bool CanEquipItem(EquipmentInstance item)
    {
        foreach (UnitSlotId slot in UnitLoadoutSlots.All)
        {
            if (CanEquipInstance(item, slot))
            {
                return true;

            }

        }

        return false;

    }

    public void EquipItem(ItemData equipmentData)
    {
        if (equipmentData is EquipmentData equipment)
        {
            TryEquipStartingItem(equipment);

        }

    }

    public bool TryUpgradeEquippedItem(int index)
    {
        if (index < 0 || index >= equippedItems.Count)
        {
            return false;

        }

        EquipmentInstance upgraded = equippedItems[index];
        if (!EquipmentGenerator.TryUpgrade(ref upgraded))
        {
            return false;

        }

        equippedItems[index] = upgraded;
        RecalculateStats();
        LoadoutChanged?.Invoke();

        return true;

    }

    public bool TryGetConsumableAtSlot(UnitSlotId slot, out ConsumableInstance item)
    {
        UnitLoadoutSlotState state = GetSlot(slot);
        if (state != null && UnitLoadoutSlots.IsConsumable(slot) &&
            !string.IsNullOrEmpty(state.ItemInstanceId))
        {
            foreach (ConsumableInstance carried in carriedConsumables)
            {
                if (carried.instanceId == state.ItemInstanceId)
                {
                    item = carried;

                    return true;

                }

            }

        }

        item = default(ConsumableInstance);

        return false;

    }

    public bool TryGetConsumableIndex(UnitSlotId slot, out int index)
    {
        UnitLoadoutSlotState state = GetSlot(slot);
        if (state != null && UnitLoadoutSlots.IsConsumable(slot))
        {
            for (int i = 0; i < carriedConsumables.Count; i++)
            {
                if (carriedConsumables[i].instanceId == state.ItemInstanceId &&
                    !string.IsNullOrEmpty(state.ItemInstanceId))
                {
                    index = i;

                    return true;

                }

            }

        }

        index = -1;

        return false;

    }

    public bool TryAssignConsumable(ConsumableInstance item, UnitSlotId slot)
    {
        if (!UnitLoadoutSlots.IsConsumable(slot) || item.definition == null ||
            carriedConsumables.Count >= ConsumableCapacity ||
            GetSlotBlockReason(slot) != UnitSlotBlockReason.None ||
            (!string.IsNullOrWhiteSpace(item.instanceId) &&
             (carriedConsumables.Exists(carried => carried.instanceId == item.instanceId) ||
              TryGetEquipmentInstance(item.instanceId, out EquipmentInstance existing))))
        {
            return false;

        }

        if (string.IsNullOrWhiteSpace(item.instanceId))
        {
            item.instanceId = Guid.NewGuid().ToString("N");

        }

        item.remainingUses = Mathf.Clamp(item.remainingUses, 0, item.definition.MaxUses);
        GetSlot(slot).SetItem(item.instanceId);
        carriedConsumables.Add(item);
        LoadoutChanged?.Invoke();

        return true;

    }

    public bool TryRemoveConsumable(UnitSlotId slot, out ConsumableInstance item)
    {
        item = default(ConsumableInstance);
        UnitLoadoutSlotState state = GetSlot(slot);
        if (state == null || !UnitLoadoutSlots.IsConsumable(slot) ||
            state.PlayerLocked || state.OccupyingMutation != MutationID.none ||
            !state.ProgressionUnlocked || string.IsNullOrEmpty(state.ItemInstanceId))
        {
            return false;

        }

        for (int i = 0; i < carriedConsumables.Count; i++)
        {
            if (carriedConsumables[i].instanceId != state.ItemInstanceId)
            {
                continue;

            }

            item = carriedConsumables[i];
            carriedConsumables.RemoveAt(i);
            state.SetItem(string.Empty);
            LoadoutChanged?.Invoke();

            return true;

        }

        return false;

    }

    // Legacy scene setup still creates items; Barracks will transfer existing instances.
    public bool TryAddConsumable(ConsumableData template)
    {
        if (template == null)
        {
            return false;

        }

        ConsumableInstance item = ConsumableGenerator.CreateConsumable(template);
        foreach (UnitSlotId slot in UnitLoadoutSlots.All)
        {
            if (UnitLoadoutSlots.IsConsumable(slot) && TryAssignConsumable(item, slot))
            {
                return true;

            }

        }

        return false;

    }

    // Spending a use never removes the item from its mission slot.
    public bool TrySpendConsumableUse(int index)
    {
        if (index < 0 || index >= carriedConsumables.Count)
        {
            return false;

        }

        ConsumableInstance item = carriedConsumables[index];
        if (item.definition == null || item.remainingUses <= 0)
        {
            return false;

        }

        item.remainingUses--;
        carriedConsumables[index] = item;

        return true;

    }

    public bool TryRefillConsumableUses(int index)
    {
        if (index < 0 || index >= carriedConsumables.Count)
        {
            return false;

        }

        ConsumableInstance item = carriedConsumables[index];
        if (item.definition == null)
        {
            return false;

        }

        item.remainingUses = item.definition.MaxUses;
        carriedConsumables[index] = item;
        return true;

    }

    public bool TryEvolveMutation(MutationID currentId, MutationID nextId)
    {
        MutationDataBase database = GameDataBaseLoader.Instance != null
            ? GameDataBaseLoader.Instance.mutationData : null;
        if (database == null)
        {
            return false;

        }

        MutationNodeDefinition previous = database.GetNode(currentId);
        MutationNodeDefinition next = database.GetNode(nextId);
        if (previous == null || next == null || !previous.CanEvolveTo(nextId) ||
            !string.Equals(previous.FamilyId, next.FamilyId, StringComparison.OrdinalIgnoreCase) ||
            next.Level <= previous.Level)
        {
            return false;

        }

        foreach (ActiveMutationRecord record in activeMutationNodes)
        {
            if (record == null || record.MutationId != currentId)
            {
                continue;

            }

            // A changed mutation can claim new slots only if those slots are free.
            if (!TryChangeMutationSlotOccupancy(currentId, nextId,
                next.OccupiedEquipmentSlots))
            {
                return false;

            }

            // The replacement contains the complete new level, not an incremental bonus.
            ApplyMutationBonuses(previous.Bonus, -1, false);
            record.EvolveTo(nextId);
            ApplyMutationBonuses(next.Bonus, 1);
            LoadoutChanged?.Invoke();

            return true;

        }

        return false;

    }

    public bool TryAddFixedMutation(MutationID id, MutationDataBase database)
    {
        MutationNodeDefinition node = database != null ? database.GetNode(id) : null;
        if (node == null || string.IsNullOrEmpty(node.FamilyId))
        {
            return false;

        }

        foreach (ActiveMutationRecord existing in activeMutationNodes)
        {
            MutationNodeDefinition existingNode = database.GetNode(existing.MutationId);
            if (existingNode != null &&
                string.Equals(existingNode.FamilyId, node.FamilyId, StringComparison.OrdinalIgnoreCase))
            {
                // A unit can only own one current node within each evolution family.
                return false;

            }

        }

        if (!TryChangeMutationSlotOccupancy(MutationID.none, id,
            node.OccupiedEquipmentSlots))
        {
            return false;

        }

        activeMutationNodes.Add(new ActiveMutationRecord(id));
        ApplyMutationBonuses(node.Bonus, 1);
        LoadoutChanged?.Invoke();

        return true;

    }

    //Mutations- Equipping Mutations, Initializing Trackers
    public bool EquipMutation(MutationID id) 
    {
        MutationDataBase database = GameDataBaseLoader.Instance != null
            ? GameDataBaseLoader.Instance.mutationData : null;
        if (database != null && database.GetNode(id) != null)
        {
            return TryAddFixedMutation(id, database);

        }

        if (!CanEquipMutation(id))
        {
            return false;

        }

        BaseMutation mutation = database.CreateMutation(id);
        activeMutations.Add(mutation.slot, mutation);

        return true;
    }

    public bool CanEquipMutation(MutationID id)
    {
        if (id == MutationID.none)
        {
            return false;
        }


        MutationDataBase database = GameDataBaseLoader.Instance != null
            ? GameDataBaseLoader.Instance.mutationData : null;
        if (database == null)
        {
            return false;

        }

        MutationNodeDefinition fixedNode = database.GetNode(id);
        if (fixedNode != null)
        {
            foreach (ActiveMutationRecord record in activeMutationNodes)
            {
                MutationNodeDefinition owned = database.GetNode(record.MutationId);
                if (owned != null && string.Equals(owned.FamilyId, fixedNode.FamilyId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return false;

                }

            }

            return true;

        }

        BaseMutation currentMutation = database.CreateMutation(id);

        if (currentMutation == null)
        {
            return false;
        }

        if (activeMutations.ContainsKey(currentMutation.slot) && activeMutations[currentMutation.slot] != null)
        {
            return false;
        }


        return true;
    }

    public void InitializeTrackers() 
    {
        
        foreach (BodySlots slot in Enum.GetValues(typeof(BodySlots)))
        {
            mutationTracker[slot] = new Dictionary<MutationStrain, Dictionary<MutationID, MutationProgress>>();

            foreach (MutationStrain strain in Enum.GetValues(typeof(MutationStrain))) 
            {
                mutationTracker[slot][strain] = new Dictionary<MutationID, MutationProgress>();

                List<MutationID> currentMutations = GameDataBaseLoader.Instance.mutationData.GetMutationsForSlots(slot, strain);
               
                if (currentMutations == null) 
                {
                    continue;
                }
                foreach (MutationID id in currentMutations) 
                {
                    if (id == MutationID.none) continue;
                    MutationDefinition mutationCosts = GameDataBaseLoader.Instance.mutationData.MutationCost[id];
                    mutationTracker[slot][strain][id] = new MutationProgress(id, mutationCosts.intialCost, mutationCosts.stageMultiplier);
                    
                }
            }
        }

    }


    // Scene-bound status/effect sources must not keep modifying permanent data
    // after their UnitController is destroyed. Future mid-mission restore will
    // capture and explicitly restore temporary effects from a mission snapshot.
    public void ClearSceneRuntimeStatModifiers()
    {
        if (runtimeStatBonuses.Count == 0)
        {
            return;

        }

        runtimeStatBonuses.Clear();
        RecalculateStats();

    }

    public void RefillResourcesToMax()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentActionPoints = maxActionPoints;

    }

    //Initializes the unit
    public static UnitData CreateNewUnit(string name = "Unit",DynamicStats race = null) 
    {
        //Setting Character name and race name for search later
        UnitData newUnit = new UnitData();
        newUnit.UnitId = Guid.NewGuid().ToString("N");
        newUnit.characterName = name;
        newUnit.raceName = race.RaceName;
        newUnit.race = race; 
       

        //Setting Starting Stats 
        newUnit.startingStrength = race.startStrength;
        newUnit.startingDexterity = race.startDexterity;
        newUnit.startingVitality = race.startVitality;
        newUnit.startingConstitution = race.startConstitution;
        newUnit.startingPerception = race.startPerception;
        newUnit.startingIntelligence = race.startIntelligence;

        newUnit.CalcMaxHealth(race);
        newUnit.CalcMaxMana(race);
        newUnit.CalcMaxActionPoints(race);
        newUnit.CalcMeleeDamage(race);
        newUnit.CalcRangeDamage(race);
        newUnit.CalcMagicDamage(race);
        newUnit.CalcArmor(race);
        newUnit.CalcWill(race);
        newUnit.maxLineOfSight = race.lineOfSight;
        newUnit.maxFieldOfView = race.fieldOfView;
        newUnit.InitializeTrackers();
        newUnit.EnsureLoadoutSlots();

        newUnit.currentHealth = newUnit.maxHealth;
        newUnit.currentMana = newUnit.maxMana;
        newUnit.currentActionPoints = newUnit.maxActionPoints;
        newUnit.currentLineOfSight = newUnit.maxLineOfSight;
        newUnit.currentFieldOfView = newUnit.maxFieldOfView;
        return newUnit; 
        
    }
}
