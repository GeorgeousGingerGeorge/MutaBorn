using System;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    // Converts serialized formulas into numbers, without changing gameplay state.
    public static class EffectValueEvaluator
    {
        public static bool TryEvaluate(EffectValueFormula formula,
            UnitController owner, UnitController recipient,
            EffectEventContext context, out double result)
        {
            result = 0d;
            if (formula == null)
            {
                return false;

            }

            result = formula.Constant;
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                return false;

            }

            if (formula.Terms == null)
            {
                return true;

            }

            foreach (EffectValueTerm term in formula.Terms)
            {
                if (term == null || !TryReadTerm(term, owner, recipient, context, out double value))
                {
                    result = 0d;

                    return false;

                }

                result += value * term.Multiplier;
                if (double.IsNaN(result) || double.IsInfinity(result))
                {
                    result = 0d;

                    return false;

                }

            }

            return true;

        }

        private static bool TryReadTerm(EffectValueTerm term,
            UnitController owner, UnitController recipient,
            EffectEventContext context, out double value)
        {
            value = 0d;
            switch (term.Source)
            {
                case EffectValueSource.Constant:
                {
                    value = term.LiteralValue;

                    return true;

                }
                case EffectValueSource.EventActualDamage:
                {
                    value = context.ActualDamage;

                    return context.HasActualDamage;

                }
                case EffectValueSource.EventAffectedTargetCount:
                {
                    value = context.AffectedTargetCount;

                    return context.HasActualDamage;

                }
                case EffectValueSource.OwnerStat:
                {
                    return TryReadStat(owner, term.Stat, out value);

                }
                case EffectValueSource.RecipientStat:
                {
                    return TryReadStat(recipient, term.Stat, out value);

                }
                case EffectValueSource.OwnerResourceCurrent:
                case EffectValueSource.OwnerResourceMaximum:
                case EffectValueSource.OwnerResourceMissing:
                {
                    return TryReadResource(owner, term.Resource, term.Source, out value);

                }
                case EffectValueSource.RecipientResourceCurrent:
                case EffectValueSource.RecipientResourceMaximum:
                case EffectValueSource.RecipientResourceMissing:
                {
                    return TryReadResource(recipient, term.Resource, term.Source, out value);

                }
                // Attempted damage and applied healing are not exposed by the
                // current CombatDamageEvent. Never silently invent those values.
                default:
                {
                    return false;

                }

            }

        }

        private static bool TryReadResource(UnitController unit, ResourceIdentifier resource,
            EffectValueSource source, out double value)
        {
            value = 0d;
            if (unit == null || unit.data == null)
            {
                return false;

            }

            IResourceTarget target = new UnitDataResourceTarget(unit.data);
            if (!target.TryGetResource(resource, out int current, out int maximum))
            {
                return false;

            }

            switch (source)
            {
                case EffectValueSource.OwnerResourceCurrent:
                case EffectValueSource.RecipientResourceCurrent:
                {
                    value = current;

                    return true;

                }
                case EffectValueSource.OwnerResourceMaximum:
                case EffectValueSource.RecipientResourceMaximum:
                {
                    value = maximum;

                    return true;

                }
                case EffectValueSource.OwnerResourceMissing:
                case EffectValueSource.RecipientResourceMissing:
                {
                    value = EffectCalculations.Missing(maximum, current);

                    return true;

                }
                default:
                {
                    return false;

                }

            }

        }

        private static bool TryReadStat(UnitController unit, StatIdentifier stat, out double value)
        {
            value = 0d;
            if (unit == null || unit.data == null)
            {
                return false;

            }

            UnitData data = unit.data;
            switch (stat)
            {
                case StatIdentifier.Strength:
                {
                    value = data.totalStrength;

                    break;

                }
                case StatIdentifier.Dexterity:
                {
                    value = data.totalDexterity;

                    break;

                }
                case StatIdentifier.Vitality:
                {
                    value = data.totalVitality;

                    break;

                }
                case StatIdentifier.Constitution:
                {
                    value = data.totalConstitution;

                    break;

                }
                case StatIdentifier.Perception:
                {
                    value = data.totalPerception;

                    break;

                }
                case StatIdentifier.Intelligence:
                {
                    value = data.totalIntelligence;

                    break;

                }
                case StatIdentifier.MaxHealth:
                {
                    value = data.maxHealth;

                    break;

                }
                case StatIdentifier.MaxMana:
                {
                    value = data.maxMana;

                    break;

                }
                case StatIdentifier.MaxActionPoints:
                {
                    value = data.maxActionPoints;

                    break;

                }
                case StatIdentifier.MeleeDamage:
                {
                    value = data.meleeDamage;

                    break;

                }
                case StatIdentifier.RangedDamage:
                {
                    value = data.rangeDamage;

                    break;

                }
                case StatIdentifier.MagicDamage:
                {
                    value = data.magicDamage;

                    break;

                }
                // Transitional read-only mapping until the physical/magic defense migration.
                case StatIdentifier.PhysicalDefense:
                {
                    value = data.armor;

                    break;

                }
                case StatIdentifier.Will:
                {
                    value = data.will;

                    break;

                }
                // MagicDefense is defined but doesn't exist in UnitData yet.
                default:
                {
                    return false;

                }

            }

            return true;

        }

    }
}