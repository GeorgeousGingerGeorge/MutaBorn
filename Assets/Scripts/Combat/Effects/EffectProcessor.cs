using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    // Step 5: a deliberately small executor for direct, single-recipient
    // resource effects. Other operations are NOT silently simulated.
    public static class EffectProcessor
    {
        private readonly struct PlannedEffect
        {
            public EffectRuntimeInstance Instance { get; }
            public UnitController Recipient { get; }
            public int Amount { get; }
            public EffectResolutionPhase Phase { get; }
            public int OriginalOrder { get; }

            public PlannedEffect(EffectRuntimeInstance instance, UnitController recipient,
                int amount, EffectResolutionPhase phase, int originalOrder)
            {
                Instance = instance;
                Recipient = recipient;
                Amount = amount;
                Phase = phase;
                OriginalOrder = originalOrder;

            }

        }

        // Plan every compatible grant from the same original snapshot, then
        // commit the planned operations in phase order. No recursive publishing.
        public static int Process(UnitController owner,
            IReadOnlyList<EffectRuntimeInstance> instances,
            EffectEventContext context, FactionManager factions, bool logResults)
        {
            if (owner == null || owner.data == null || owner.data.currentHealth <= 0 ||
                instances == null || context.Trigger == EffectTrigger.None)
            {
                return 0;

            }

            List<PlannedEffect> planned = new List<PlannedEffect>();
            for (int i = 0; i < instances.Count; i++)
            {
                EffectRuntimeInstance instance = instances[i];
                if (instance == null || instance.Expired || instance.Grant == null)
                {
                    continue;

                }

                EffectGrant grant = instance.Grant;
                if (grant.Definition == null || grant.Activation == null ||
                    grant.Targeting == null || grant.OperationParameters == null || grant.Value == null ||
                    grant.Activation.Trigger != context.Trigger ||
                    !MatchesActor(owner, context.Actor, grant.Activation.EventActor, factions))
                {
                    continue;

                }

                if (grant.Activation.RequireGrantingSourceMatch)
                {
                    // The current collection binder does not know which gear
                    // instance caused a hit. Fail closed until source IDs exist.
                    WarnSkipped(grant, "source matching requires source attribution (later integration)", owner, logResults);

                    continue;

                }

                if (grant.Targeting.Selection != EffectTargetSelection.Single)
                {
                    WarnSkipped(grant, "area/nearest/random targeting is not implemented in Step 5", owner, logResults);

                    continue;

                }

                if (grant.Definition.Operation != EffectOperation.ModifyResource)
                {
                    WarnSkipped(grant, "operation waits for the Damage Resolver or Stat/Status system", owner, logResults);

                    continue;

                }

                if (!TryResolveRecipient(owner, context, grant.Targeting, factions, out UnitController recipient))
                {
                    continue;

                }

                if (!instance.CanRun(context.ResolutionId, recipient.GetEntityId()) ||
                    !ConditionsPass(grant, owner, recipient, context))
                {
                    continue;

                }

                if (!EffectValueEvaluator.TryEvaluate(grant.Value, owner, recipient, context, out double value))
                {
                    WarnSkipped(grant, "formula uses an unavailable input or invalid number", owner, logResults);

                    continue;

                }

                try
                {
                    int amount = EffectCalculations.ToInt(value, grant.Value.Rounding);
                    planned.Add(new PlannedEffect(instance, recipient, amount, grant.Activation.Phase, i));

                }
                catch (Exception exception) when (exception is ArgumentException || exception is OverflowException)
                {
                    Debug.LogWarning($"Effect '{grant.Definition.DisplayName}' calculation failed: {exception.Message}", owner);

                }

            }

            planned.Sort((a, b) =>
            {
                int phaseOrder = a.Phase.CompareTo(b.Phase);

                return phaseOrder != 0 ? phaseOrder : a.OriginalOrder.CompareTo(b.OriginalOrder);

            });

            int applied = 0;
            foreach (PlannedEffect plan in planned)
            {
                ResourceIdentifier resource = plan.Instance.Grant.OperationParameters.Resource;
                if (!ResourceOperations.TryApply(plan.Recipient.data, resource, plan.Amount,
                    out ResourceChangeResult change))
                {
                    WarnSkipped(plan.Instance.Grant, "invalid resource or change (health damage must use Damage Resolver)",
                        owner, logResults);

                    continue;

                }

                plan.Instance.RecordSuccessfulUse(context.ResolutionId, plan.Recipient.GetEntityId());
                applied++;

                if (logResults)
                {
                    Debug.Log($"Effect '{plan.Instance.Grant.Definition.DisplayName}': " +
                        $"{owner.data.characterName} -> {plan.Recipient.data.characterName} " +
                        $"{change.Resource} {change.AppliedAmount:+#;-#;0} " +
                        $"({change.Before} -> {change.After}), trigger {context.Trigger}.", owner);

                }

            }

            return applied;

        }

        private static bool ConditionsPass(EffectGrant grant, UnitController owner,
            UnitController recipient, EffectEventContext context)
        {
            if (grant.Conditions == null)
            {
                return true;

            }

            foreach (EffectCondition condition in grant.Conditions)
            {
                if (condition == null ||
                    !EffectValueEvaluator.TryEvaluate(condition.Left, owner, recipient, context, out double left) ||
                    !EffectValueEvaluator.TryEvaluate(condition.Right, owner, recipient, context, out double right))
                {
                    return false;

                }

                bool valid;
                switch (condition.Comparison)
                {
                    case EffectComparison.Equal:
                    {
                        valid = left == right;

                        break;

                    }
                    case EffectComparison.NotEqual:
                    {
                        valid = left != right;

                        break;

                    }
                    case EffectComparison.Greater:
                    {
                        valid = left > right;

                        break;

                    }
                    case EffectComparison.GreaterOrEqual:
                    {
                        valid = left >= right;

                        break;

                    }
                    case EffectComparison.Less:
                    {
                        valid = left < right;

                        break;

                    }
                    case EffectComparison.LessOrEqual:
                    {
                        valid = left <= right;

                        break;

                    }
                    default:
                    {
                        return false;

                    }

                }

                if (!valid)
                {
                    return false;

                }

            }

            return true;

        }

        private static bool MatchesActor(UnitController owner, UnitController actor,
            EffectEventActorFilter filter, FactionManager factions)
        {
            if (actor == null)
            {
                return false;

            }

            switch (filter)
            {
                case EffectEventActorFilter.OwnerOnly:
                {
                    return owner == actor;

                }
                case EffectEventActorFilter.OwnerOrAllies:
                {
                    return owner == actor || HasRelation(owner, actor, factions, FactionRelation.Allied);

                }
                case EffectEventActorFilter.HostilesOnly:
                {
                    return HasRelation(owner, actor, factions, FactionRelation.Hostile);

                }
                case EffectEventActorFilter.Anyone:
                {
                    return true;

                }
                default:
                {
                    return false;

                }

            }

        }

        private static bool TryResolveRecipient(UnitController owner, EffectEventContext context,
            EffectTargetRules rules, FactionManager factions, out UnitController recipient)
        {
            recipient = null;
            switch (rules.Reference)
            {
                case EffectTargetReference.Owner:
                {
                    recipient = owner;

                    break;

                }
                case EffectTargetReference.EventSource:
                {
                    recipient = context.EventSource;

                    break;

                }
                case EffectTargetReference.EventTarget:
                {
                    recipient = context.EventTarget;

                    break;

                }
                case EffectTargetReference.ActionTarget:
                {
                    recipient = context.ActionTarget;

                    break;

                }
                default:
                {
                    return false;

                }

            }

            if (recipient == null || recipient.data == null || !recipient.isActiveAndEnabled ||
                recipient.data.currentHealth <= 0 || (!rules.IncludeOwner && recipient == owner) ||
                rules.FactionMask == EffectFactionMask.None)
            {
                return false;

            }

            if (rules.FactionMask == EffectFactionMask.Any)
            {
                return true;

            }

            if (owner == recipient)
            {
                return (rules.FactionMask & EffectFactionMask.Allied) != 0;

            }

            if (!TryGetRelation(owner, recipient, factions, out FactionRelation relation))
            {
                return false;

            }

            EffectFactionMask needed = relation == FactionRelation.Allied ? EffectFactionMask.Allied :
                relation == FactionRelation.Hostile ? EffectFactionMask.Hostile : EffectFactionMask.Neutral;

            return (rules.FactionMask & needed) != 0;

        }

        private static bool HasRelation(UnitController owner, UnitController other,
            FactionManager factions, FactionRelation expected)
        {
            return TryGetRelation(owner, other, factions, out FactionRelation relation) && relation == expected;

        }

        private static bool TryGetRelation(UnitController owner, UnitController other,
            FactionManager factions, out FactionRelation relation)
        {
            relation = FactionRelation.Neutral;
            if (owner == null || other == null)
            {
                return false;

            }

            UnitFaction ownerFaction = owner.GetComponent<UnitFaction>();
            UnitFaction otherFaction = other.GetComponent<UnitFaction>();
            if (ownerFaction == null || otherFaction == null ||
                ownerFaction.Faction == null || otherFaction.Faction == null)
            {
                return false;

            }

            if (ownerFaction.Faction == otherFaction.Faction)
            {
                relation = FactionRelation.Allied;

                return true;

            }

            if (factions == null)
            {
                return false;

            }

            relation = factions.GetRelation(ownerFaction.Faction, otherFaction.Faction);

            return true;

        }

        private static void WarnSkipped(EffectGrant grant, string reason,
            UnitController owner, bool logResults)
        {
            if (logResults && grant != null && grant.Definition != null)
            {
                Debug.LogWarning($"Effect '{grant.Definition.DisplayName}' skipped: {reason}.", owner);

            }

        }

    }
}