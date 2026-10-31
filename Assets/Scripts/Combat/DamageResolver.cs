using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    // An attack may have a primary stat-scaled component and additional
    // fixed components. Armor and Ward apply once per damage category.
    public readonly struct DamageRequest
    {
        public UnitController Attacker { get; }
        public UnitController Target { get; }
        public ActionDefinition Action { get; }
        public int BaseDamage { get; }
        public float Multiplier { get; }
        public int FlatDamageBonus { get; }
        public int MinimumDamage { get; }
        public DamageType DamageType { get; }
        public DamageDeliveryType DeliveryType { get; }
        public IReadOnlyList<DamageComponent> AdditionalComponents { get; }
        public int FlatArmorPenetration { get; }
        public int FlatWardPenetration { get; }
        public bool IsCritical { get; }

        public DamageRequest(UnitController attacker, UnitController target, int baseDamage,
            float multiplier = 1f, int flatDamageBonus = 0, int minimumDamage = 0,
            ActionDefinition action = null, DamageType damageType = DamageType.Unspecified,
            DamageDeliveryType deliveryType = DamageDeliveryType.Unspecified,
            IReadOnlyList<DamageComponent> additionalComponents = null,
            int flatArmorPenetration = 0, int flatWardPenetration = 0,
            bool isCritical = false)
        {
            Attacker = attacker;
            Target = target;
            Action = action;
            BaseDamage = baseDamage;
            Multiplier = multiplier;
            FlatDamageBonus = flatDamageBonus;
            MinimumDamage = minimumDamage;
            DamageType = damageType;
            DeliveryType = deliveryType;
            AdditionalComponents = additionalComponents;
            FlatArmorPenetration = Mathf.Max(0, flatArmorPenetration);
            FlatWardPenetration = Mathf.Max(0, flatWardPenetration);
            IsCritical = isCritical;

        }

    }

    // The portion of actual HP damage credited to a specific damage type.
    // All contributions in a result add up to DamageResult.ActualDamage.
    public readonly struct DamageTypeAmount
    {
        public DamageType Type { get; }
        public int Amount { get; }

        public DamageTypeAmount(DamageType type, int amount)
        {
            Type = type;
            Amount = amount;

        }

    }

    // DamageAfterArmor is before percentage resistances and the minimum hit
    // rule. DamageAfterResistances includes the minimum, before Guard.
    public readonly struct DamageResult
    {
        public DamageRequest Request { get; }
        public int DamageBeforeArmor { get; }
        public int DamageAfterArmor { get; }
        public int DamageAfterResistances { get; }
        public int DamageAfterGuard { get; }
        public int ActualDamage { get; }
        public IReadOnlyList<DamageTypeAmount> DamageByType { get; }
        public int HealthBefore { get; }
        public int HealthAfter { get; }
        public bool IsCritical => Request.IsCritical;
        public bool Evaded { get; }
        public bool ReducedHealthToZero => HealthBefore > 0 && HealthAfter <= 0;

        public DamageResult(DamageRequest request, int damageBeforeArmor,
            int damageAfterArmor, int damageAfterResistances, int damageAfterGuard,
            int healthBefore, int healthAfter,
            IReadOnlyList<DamageTypeAmount> damageByType = null,
            bool evaded = false)
        {
            Request = request;
            DamageBeforeArmor = damageBeforeArmor;
            DamageAfterArmor = damageAfterArmor;
            DamageAfterResistances = damageAfterResistances;
            DamageAfterGuard = damageAfterGuard;
            HealthBefore = healthBefore;
            HealthAfter = healthAfter;
            ActualDamage = Mathf.Max(0, healthBefore - healthAfter);
            Evaded = evaded;
            DamageByType = damageByType ?? Array.Empty<DamageTypeAmount>();

        }

    }

    // Shared damage application. Resource costs, events, objective updates,
    // and defeat cleanup remain with CombatActionExecutor until migrated.
    public static class DamageResolver
    {
        public static bool TryResolveAction(UnitController attacker, UnitController target,
            ActionDefinition action, out DamageResult result, int sourceEquipmentIndex = -1,
            bool ignoreDodge = false)
        {
            result = default(DamageResult);

            if (attacker == null || attacker.data == null || action == null ||
                target == null || target.data == null)
            {
                return false;

            }

            int baseDamage = GetBaseDamage(attacker.data, action.DamageStat);
            DamageType type = action.DamageType;

            // Legacy assets have no damage type. Preserve a working default
            // until the team's ActionDefinition assets receive explicit types.
            if (type == DamageType.Unspecified)
            {
                switch (action.DamageStat)
                {
                    case CombatDamageStat.Ranged:
                    {
                        type = DamageType.Pierce;

                        break;

                    }
                    case CombatDamageStat.Magic:
                    {
                        type = DamageType.Arcane;

                        break;

                    }
                    default:
                    {
                        type = DamageType.Slash;

                        break;

                    }

                }

            }

            IReadOnlyList<DamageComponent> components = action.AdditionalDamage;
            if (sourceEquipmentIndex >= 0 && attacker.data.equippedItems != null &&
                sourceEquipmentIndex < attacker.data.equippedItems.Count)
            {
                EquipmentInstance equipment = attacker.data.equippedItems[sourceEquipmentIndex];
                if (action.DamageType == DamageType.Unspecified &&
                    equipment.primaryDamageType != DamageType.Unspecified)
                {
                    type = equipment.primaryDamageType;

                }

                if (equipment.bonusDamage != null && equipment.bonusDamage.Length > 0)
                {
                    List<DamageComponent> combined = new List<DamageComponent>();
                    if (components != null)
                    {
                        combined.AddRange(components);

                    }

                    combined.AddRange(equipment.bonusDamage);
                    components = combined;

                }

            }

            // One critical roll per action, not one per damage type.
            bool critical = action.CriticalChancePercent >= 100f ||
                (action.CriticalChancePercent > 0f &&
                 UnityEngine.Random.value * 100f < action.CriticalChancePercent);
            float multiplier = action.DamageMultiplier *
                (critical ? action.CriticalDamageMultiplier : 1f);

            DamageRequest request = new DamageRequest(attacker, target, baseDamage,
                multiplier, action.FlatDamageBonus, action.MinimumDamage,
                action, type, DamageDeliveryType.Unspecified, components,
                action.FlatArmorPenetration, action.FlatWardPenetration, critical);

            UnitStatusController defendingStatuses = target.GetComponent<UnitStatusController>();
            float dodgeChance = defendingStatuses != null ? defendingStatuses.DodgeChancePercent : 0f;
            if (!ignoreDodge && dodgeChance > 0f && UnityEngine.Random.value * 100f < dodgeChance)
            {
                int health = target.data.currentHealth;
                result = new DamageResult(request, 0, 0, 0, 0, health, health,
                    null, true);

                return true;

            }

            return TryResolve(request, out result);

        }

        public static bool TryResolve(DamageRequest request, out DamageResult result)
        {
            result = default(DamageResult);

            UnitController target = request.Target;
            if (target == null || target.data == null || target.data.currentHealth <= 0 ||
                float.IsNaN(request.Multiplier) || float.IsInfinity(request.Multiplier))
            {
                return false;

            }

            // Preserve the Step 6 primary-damage scaling (including rounding).
            int scaledDamage = Mathf.CeilToInt(Mathf.Max(0, request.BaseDamage) * request.Multiplier);
            long primaryDamage = Math.Max(0L, (long)scaledDamage + request.FlatDamageBonus);
            long beforeArmor = primaryDamage;

            // Combine identical types so extra components cannot change the
            // outcome simply by being split into separate entries.
            Dictionary<DamageType, long> components = new Dictionary<DamageType, long>();
            AddComponent(components, request.DamageType, primaryDamage);

            if (request.AdditionalComponents != null)
            {
                foreach (DamageComponent component in request.AdditionalComponents)
                {
                    long amount = Math.Max(0, component.Amount);
                    beforeArmor += amount;
                    AddComponent(components, component.Type, amount);

                }

            }

            long physicalDamage = 0L;
            long magicalDamage = 0L;
            long trueDamage = 0L;

            foreach (KeyValuePair<DamageType, long> component in components)
            {
                if (component.Key == DamageType.True)
                {
                    trueDamage += component.Value;

                }
                else if (IsPhysicalDamage(component.Key))
                {
                    physicalDamage += component.Value;

                }
                else
                {
                    magicalDamage += component.Value;

                }

            }

            // Each flat defense applies once; True damage bypasses both.
            long effectiveArmor = Math.Max(0L, (long)target.data.armor - request.FlatArmorPenetration);
            long effectiveWard = Math.Max(0L, (long)target.data.ward - request.FlatWardPenetration);
            long physicalAfterArmor = Math.Max(0L, physicalDamage - effectiveArmor);
            long magicalAfterWard = Math.Max(0L, magicalDamage - effectiveWard);
            long afterArmor = physicalAfterArmor + magicalAfterWard + trueDamage;
            UnitStatusController targetStatuses = target.GetComponent<UnitStatusController>();
            double incomingMultiplier = targetStatuses != null ? targetStatuses.IncomingOrdinaryDamageMultiplier : 1d;
            double afterResistances = trueDamage;
            Dictionary<DamageType, double> ordinaryWeights = new Dictionary<DamageType, double>();

            // Preserve each type's share after flat defense, then apply its
            // individual resistance or weakness. This makes splitting damage
            // across types unable to multiply the Armor/Ward subtraction.
            foreach (KeyValuePair<DamageType, long> component in components)
            {
                if (component.Key == DamageType.True)
                {
                    continue;

                }

                bool physical = IsPhysicalDamage(component.Key);
                long categoryTotal = physical ? physicalDamage : magicalDamage;
                long categoryRemaining = physical ? physicalAfterArmor : magicalAfterWard;

                if (categoryTotal <= 0 || categoryRemaining <= 0)
                {
                    continue;

                }

                double shareAfterDefense = (double)component.Value * categoryRemaining / categoryTotal;
                double resistance = target.data.GetDamageResistancePercent(component.Key);
                double multiplier = Math.Max(0d, 1d - resistance / 100d);
                double typeDamage = shareAfterDefense * multiplier * incomingMultiplier;
                afterResistances += typeDamage;
                if (typeDamage > 0d)
                {
                    ordinaryWeights[component.Key] = typeDamage;

                }

            }

            // One minimum for the entire hit, never one per component.
            // A zero-damage request with no minimum can still do zero damage.
            int damageAfterResistances = 0;
            if (beforeArmor > 0 || request.MinimumDamage > 0)
            {
                double rounded = Math.Ceiling(Math.Max(0d, afterResistances));
                long minimumDamage = Math.Max(1L, request.MinimumDamage);
                damageAfterResistances = (int)Math.Min(int.MaxValue,
                    Math.Max((double)minimumDamage, rounded));

            }

            // Guard reduces only ordinary damage; True damage is not guardable.
            int trueDamageToApply = (int)Math.Min(trueDamage, damageAfterResistances);
            int guardableDamage = damageAfterResistances - trueDamageToApply;
            CombatStatusHandler statuses = CombatStatusHandler.Instance;
            if (statuses != null && guardableDamage > 0)
            {
                guardableDamage = statuses.FilterIncomingDamage(target, guardableDamage);

            }

            int damageAfterGuard = (int)Math.Min(int.MaxValue,
                (long)Mathf.Max(0, guardableDamage) + trueDamageToApply);
            int healthBefore = target.data.currentHealth;
            target.data.ModifyHealth(-damageAfterGuard);
            int healthAfter = target.data.currentHealth;

            // These allocations are visual/diagnostic only. They never alter
            // the total damage or the existing defense/Guard/health calculation.
            // Shared damage is apportioned proportionally with deterministic
            // largest-remainder rounding (ties by DamageType enum value).
            Dictionary<DamageType, int> afterGuardByType = DistributeDamage(
                guardableDamage, ordinaryWeights, request.DamageType);
            if (trueDamageToApply > 0)
            {
                afterGuardByType.TryGetValue(DamageType.True, out int existingTrue);
                afterGuardByType[DamageType.True] = existingTrue + trueDamageToApply;

            }

            Dictionary<DamageType, double> finalWeights = new Dictionary<DamageType, double>();
            foreach (KeyValuePair<DamageType, int> entry in afterGuardByType)
            {
                finalWeights[entry.Key] = entry.Value;

            }

            int actualDamage = Mathf.Max(0, healthBefore - healthAfter);
            Dictionary<DamageType, int> actualByType = DistributeDamage(
                actualDamage, finalWeights, request.DamageType);
            List<DamageTypeAmount> breakdown = new List<DamageTypeAmount>();
            List<DamageType> sortedTypes = new List<DamageType>(actualByType.Keys);
            sortedTypes.Sort();
            foreach (DamageType type in sortedTypes)
            {
                int amount = actualByType[type];
                if (amount > 0)
                {
                    breakdown.Add(new DamageTypeAmount(type, amount));

                }

            }

            result = new DamageResult(request, ClampToInt(beforeArmor),
                ClampToInt(afterArmor), damageAfterResistances, damageAfterGuard,
                healthBefore, healthAfter, breakdown.ToArray());

            return true;

        }

        private sealed class WeightedShare
        {
            public DamageType Type;
            public int Amount;
            public double Remainder;

        }

        // A shared reduction or an overkill cap must not manufacture damage.
        // Use the exact requested total, distributing indivisible points by
        // largest fractional remainder; never round each type independently.
        private static Dictionary<DamageType, int> DistributeDamage(int amount,
            Dictionary<DamageType, double> weights, DamageType fallbackType)
        {
            Dictionary<DamageType, int> result = new Dictionary<DamageType, int>();
            if (amount <= 0)
            {
                return result;

            }

            double total = 0d;
            List<WeightedShare> shares = new List<WeightedShare>();
            foreach (KeyValuePair<DamageType, double> entry in weights)
            {
                if (entry.Value <= 0d || double.IsNaN(entry.Value) ||
                    double.IsInfinity(entry.Value))
                {
                    continue;

                }

                total += entry.Value;
                shares.Add(new WeightedShare { Type = entry.Key });

            }

            if (total <= 0d)
            {
                // Minimum hit damage may exist even when defenses zeroed
                // every component; attribute it to the attack's primary type.
                result[fallbackType] = amount;

                return result;

            }

            int assigned = 0;
            foreach (WeightedShare share in shares)
            {
                double exact = (double)amount * weights[share.Type] / total;
                share.Amount = (int)Math.Floor(exact);
                share.Remainder = exact - share.Amount;
                assigned += share.Amount;

            }

            shares.Sort((a, b) =>
            {
                int remainderOrder = b.Remainder.CompareTo(a.Remainder);
                return remainderOrder != 0 ? remainderOrder : a.Type.CompareTo(b.Type);

            });

            int leftovers = amount - assigned;
            for (int i = 0; i < leftovers; i++)
            {
                shares[i % shares.Count].Amount++;

            }

            foreach (WeightedShare share in shares)
            {
                if (share.Amount > 0)
                {
                    result[share.Type] = share.Amount;

                }

            }

            return result;

        }

        private static void AddComponent(Dictionary<DamageType, long> components,
            DamageType type, long damage)
        {
            if (damage <= 0)
            {
                return;

            }

            components.TryGetValue(type, out long existing);
            components[type] = existing + damage;

        }

        private static bool IsPhysicalDamage(DamageType type)
        {
            // Unspecified direct requests retain the legacy physical default.
            return type == DamageType.Unspecified || type == DamageType.Slash ||
                type == DamageType.Pierce || type == DamageType.Blunt;

        }

        private static int GetBaseDamage(UnitData attacker, CombatDamageStat damageStat)
        {
            switch (damageStat)
            {
                case CombatDamageStat.Ranged:
                {
                    return attacker.rangeDamage;

                }
                case CombatDamageStat.Magic:
                {
                    return attacker.magicDamage;

                }
                default:
                {
                    return attacker.meleeDamage;

                }

            }

        }

        private static int ClampToInt(long value)
        {
            return (int)Math.Max(int.MinValue, Math.Min(value, int.MaxValue));

        }

    }

}
