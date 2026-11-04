using UnityEngine;
using System.Collections.Generic;
using System;
using MutaBorn.Gameplay;
using UnityEngine.Analytics;



public class UnitData
{

    [Header("Character Information- Name/RaceName")]

    public string characterName;
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

    public const int ConsumableCapacity = 2;
    public List<ConsumableInstance> carriedConsumables = new List<ConsumableInstance>();

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
                total += Math.Max(0, equipment.wardRating);

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
    public void ApplyEquipmentBonuses(EquipmentInstance item, int multiplier)
    {
        if (item.Equals(default(EquipmentInstance)))
            return;
       
        equipmentMeleeDamage += item.meleeDamage * multiplier;
        equipmentRangeDamage += item.rangeDamage * multiplier;
        equipmentMagicDamage += item.magicDamage * multiplier;
        equipmentArmor += item.armorRating * multiplier;
       


        RecalculateStats();

    }
    public void RecalculateStats() 
    {
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



    // During combat testing, equipment has no slot restrictions.
    public bool CanEquipItem(EquipmentInstance item)
    {
        return !item.Equals(default(EquipmentInstance));

    }

    public void EquipItem(ItemData equipmentData)
    {
        if (equipmentData is not EquipmentData)
        {
            return;

        }

        EquipmentInstance item = (EquipmentInstance)ItemGenerator.ItemInstance(equipmentData);

        if (!CanEquipItem(item))
        {
            return;

        }

        equippedItems.Add(item);
        ApplyEquipmentBonuses(item, 1);

    }


    public bool TryUpgradeEquippedItem(int index)
    {
        if (index < 0 || index >= equippedItems.Count)
        {
            return false;

        }

        EquipmentInstance previous = equippedItems[index];
        EquipmentInstance upgraded = previous;
        if (!EquipmentGenerator.TryUpgrade(ref upgraded))
        {
            return false;

        }

        equippedItems[index] = upgraded;
        equipmentMeleeDamage += upgraded.meleeDamage - previous.meleeDamage;
        equipmentRangeDamage += upgraded.rangeDamage - previous.rangeDamage;
        equipmentMagicDamage += upgraded.magicDamage - previous.magicDamage;
        equipmentArmor += upgraded.armorRating - previous.armorRating;
        RecalculateStats();

        return true;

    }

    // Consumables are selected before a mission and stored separately from gear.
    // Each entry occupies one of the two available consumable slots.
    public bool TryAddConsumable(ConsumableData template)
    {
        if (template == null || carriedConsumables.Count >= ConsumableCapacity)
        {
            return false;

        }

        ConsumableInstance instance = ConsumableGenerator.CreateConsumable(template);
        carriedConsumables.Add(instance);

        return true;

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

            // The replacement contains the complete new level, not an incremental bonus.
            ApplyMutationBonuses(previous.Bonus, -1, false);
            record.EvolveTo(nextId);
            ApplyMutationBonuses(next.Bonus, 1);

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

        activeMutationNodes.Add(new ActiveMutationRecord(id));
        ApplyMutationBonuses(node.Bonus, 1);

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


    public void RefillResourcesToMax()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentActionPoints = maxActionPoints;

    }

    //Initializes the unit
    public static UnitData CreateNewUnit(string name = "harry",DynamicStats race = null) 
    {
        //Setting Character name and race name for search later
        UnitData newUnit = new UnitData();
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

        newUnit.currentHealth = newUnit.maxHealth;
        newUnit.currentMana = newUnit.maxMana;
        newUnit.currentActionPoints = newUnit.maxActionPoints;
        newUnit.currentLineOfSight = newUnit.maxLineOfSight;
        newUnit.currentFieldOfView = newUnit.maxFieldOfView;
        return newUnit; 
        
    }
}
