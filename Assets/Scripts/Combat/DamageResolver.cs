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

        public DamageRequest(UnitController attacker, UnitController target, int baseDamage,
            float multiplier = 1f, int flatDamageBonus = 0, int minimumDamage = 0,
            ActionDefinition action = null, DamageType damageType = DamageType.Unspecified,
            DamageDeliveryType deliveryType = DamageDeliveryType.Unspecified,
            IReadOnlyList<DamageComponent> additionalComponents = null)
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
        public int HealthBefore { get; }
        public int HealthAfter { get; }
        public bool ReducedHealthToZero => HealthBefore > 0 && HealthAfter <= 0;

        public DamageResult(DamageRequest request, int damageBeforeArmor,
            int damageAfterArmor, int damageAfterResistances, int damageAfterGuard,
            int healthBefore, int healthAfter)
        {
            Request = request;
            DamageBeforeArmor = damageBeforeArmor;
            DamageAfterArmor = damageAfterArmor;
            DamageAfterResistances = damageAfterResistances;
            DamageAfterGuard = damageAfterGuard;
            HealthBefore = healthBefore;
            HealthAfter = healthAfter;
            ActualDamage = Mathf.Max(0, healthBefore - healthAfter);

        }

    }

    // Shared damage application. Resource costs, events, objective updates,
    // and defeat cleanup remain with CombatActionExecutor until migrated.
    public static class DamageResolver
    {
        public static bool TryResolveAction(UnitController attacker, UnitController target,
            ActionDefinition action, out DamageResult result)
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

            DamageRequest request = new DamageRequest(attacker, target, baseDamage,
                action.DamageMultiplier, action.FlatDamageBonus, action.MinimumDamage,
                action, type, DamageDeliveryType.Unspecified, action.AdditionalDamage);

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

            foreach (KeyValuePair<DamageType, long> component in components)
            {
                if (IsPhysicalDamage(component.Key))
                {
                    physicalDamage += component.Value;

                }
                else
                {
                    magicalDamage += component.Value;

                }

            }

            // Flat defenses apply once to the total physical and magical
            // portions of the hit, not once for every damage type.
            long physicalAfterArmor = Math.Max(0L, physicalDamage - Math.Max(0, target.data.armor));
            long magicalAfterWard = Math.Max(0L, magicalDamage - Math.Max(0, target.data.ward));
            long afterArmor = physicalAfterArmor + magicalAfterWard;
            double afterResistances = 0d;

            // Preserve each type's share after flat defense, then apply its
            // individual resistance or weakness. This makes splitting damage
            // across types unable to multiply the Armor/Ward subtraction.
            foreach (KeyValuePair<DamageType, long> component in components)
            {
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
                afterResistances += shareAfterDefense * multiplier;

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

            int damageAfterGuard = damageAfterResistances;
            CombatStatusHandler statuses = CombatStatusHandler.Instance;
            if (statuses != null)
            {
                damageAfterGuard = statuses.FilterIncomingDamage(target, damageAfterGuard);

            }

            damageAfterGuard = Mathf.Max(0, damageAfterGuard);
            int healthBefore = target.data.currentHealth;
            target.data.ModifyHealth(-damageAfterGuard);
            int healthAfter = target.data.currentHealth;

            result = new DamageResult(request, ClampToInt(beforeArmor),
                ClampToInt(afterArmor), damageAfterResistances, damageAfterGuard,
                healthBefore, healthAfter);

            return true;

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
