using UnityEngine;

namespace MutaBorn.Gameplay
{
    // One immutable snapshot of the original event. Never read damage from a
    // recipient's health after other effects have already changed it.
    public readonly struct EffectEventContext
    {
        public EffectTrigger Trigger { get; }
        public long ResolutionId { get; }
        public UnitController Actor { get; }
        public UnitController EventSource { get; }
        public UnitController EventTarget { get; }
        public UnitController ActionTarget { get; }
        public ActionDefinition Action { get; }
        public int ActualDamage { get; }
        public bool HasActualDamage { get; }
        public int AffectedTargetCount { get; }

        public EffectEventContext(EffectTrigger trigger, long resolutionId,
            UnitController actor, UnitController eventSource,
            UnitController eventTarget, UnitController actionTarget,
            ActionDefinition action, int actualDamage, bool hasActualDamage,
            int affectedTargetCount)
        {
            Trigger = trigger;
            ResolutionId = resolutionId;
            Actor = actor;
            EventSource = eventSource;
            EventTarget = eventTarget;
            ActionTarget = actionTarget;
            Action = action;
            ActualDamage = actualDamage;
            HasActualDamage = hasActualDamage;
            AffectedTargetCount = affectedTargetCount;

        }

    }
}