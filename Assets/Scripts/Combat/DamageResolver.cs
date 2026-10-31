using System;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    // Raw damage input. The source may be an action, a status, or a future
    // environmental effect; the resolver does not choose how it was created.
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

        public DamageRequest(UnitController attacker, UnitController target, int baseDamage,
            float multiplier = 1f, int flatDamageBonus = 0, int minimumDamage = 0,
            ActionDefinition action = null, DamageType damageType = DamageType.Unspecified,
            DamageDeliveryType deliveryType = DamageDeliveryType.Unspecified)
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

        }

    }

    // Snapshot of one application. ActualDamage is capped by health remaining;
    // DamageAfterGuard is the amount requested before health clamps it.
    public readonly struct DamageResult
    {
        public DamageRequest Request { get; }
        public int DamageBeforeArmor { get; }
        public int DamageAfterArmor { get; }
        public int DamageAfterGuard { get; }
        public int ActualDamage { get; }
        public int HealthBefore { get; }
        public int HealthAfter { get; }
        public bool ReducedHealthToZero => HealthBefore > 0 && HealthAfter <= 0;

        public DamageResult(DamageRequest request, int damageBeforeArmor,
            int damageAfterArmor, int damageAfterGuard, int healthBefore, int healthAfter)
        {
            Request = request;
            DamageBeforeArmor = damageBeforeArmor;
            DamageAfterArmor = damageAfterArmor;
            DamageAfterGuard = damageAfterGuard;
            HealthBefore = healthBefore;
            HealthAfter = healthAfter;
            ActualDamage = Mathf.Max(0, healthBefore - healthAfter);

        }

    }

    // Shared damage calculator and application layer. Action costs, combat
    // event publication, defeat cleanup, and targeting remain with the caller.
    public static class DamageResolver
    {
        // Compatibility adapter for existing ActionDefinition-based attacks.
        // Other sources can build a DamageRequest directly, but their callers
        // still need to publish combat events and handle defeat.
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
            DamageRequest request = new DamageRequest(attacker, target, baseDamage,
                action.DamageMultiplier, action.FlatDamageBonus, action.MinimumDamage, action);

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

            // Keep the existing MutaBorn attack formula and rounding intact.
            int scaledDamage = Mathf.CeilToInt(Mathf.Max(0, request.BaseDamage) * request.Multiplier);
            long damageBeforeArmor = (long)scaledDamage + request.FlatDamageBonus;
            long damageAfterArmor = damageBeforeArmor - Mathf.Max(0, target.data.armor);
            long minimumDamage = Math.Max(0L, request.MinimumDamage);
            long resolvedDamage = Math.Max(minimumDamage, damageAfterArmor);

            int damageBeforeArmorInt = ClampToInt(damageBeforeArmor);
            int damageAfterArmorInt = (int)Math.Min(resolvedDamage, int.MaxValue);
            int damageAfterGuard = damageAfterArmorInt;

            CombatStatusHandler statuses = CombatStatusHandler.Instance;
            if (statuses != null)
            {
                damageAfterGuard = statuses.FilterIncomingDamage(target, damageAfterGuard);

            }

            damageAfterGuard = Mathf.Max(0, damageAfterGuard);
            int healthBefore = target.data.currentHealth;
            target.data.ModifyHealth(-damageAfterGuard);
            int healthAfter = target.data.currentHealth;

            result = new DamageResult(request, damageBeforeArmorInt,
                damageAfterArmorInt, damageAfterGuard, healthBefore, healthAfter);

            return true;

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
