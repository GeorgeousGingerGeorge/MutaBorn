using UnityEngine;

namespace MutaBorn.Gameplay
{
    // One instance per grant per unit, NOT on the shared ScriptableObject.
    public sealed class EffectRuntimeInstance
    {
        public EffectGrant Grant { get; }
        public EffectCollection Collection { get; }
        public int RemainingTurns { get; private set; }
        public int RemainingUses { get; private set; }
        public bool Expired { get; private set; }

        private long lastResolutionId = -1;
        private long lastTargetResolutionId = -1;
        private EntityId lastTargetId;

        public EffectRuntimeInstance(EffectCollection collection, EffectGrant grant)
        {
            Collection = collection;
            Grant = grant;
            RemainingTurns = grant != null && grant.Lifetime != null
                ? Mathf.Max(1, grant.Lifetime.DurationTurns) : 0;
            RemainingUses = grant != null && grant.Lifetime != null
                ? Mathf.Max(1, grant.Lifetime.MaximumUses) : 0;

        }

        public bool CanRun(long resolutionId, EntityId recipientId)
        {
            if (Expired || Grant == null || Grant.Lifetime == null || Grant.Activation == null)
            {
                return false;

            }

            switch (Grant.Activation.Frequency)
            {
                case EffectProcFrequency.OncePerResolution:
                case EffectProcFrequency.OncePerTriggerEvent:
                {
                    return lastResolutionId != resolutionId;

                }
                case EffectProcFrequency.OncePerTarget:
                {
                    return lastTargetResolutionId != resolutionId || !lastTargetId.Equals(recipientId);

                }
                default:
                {
                    return false;

                }

            }

        }

        public void RecordSuccessfulUse(long resolutionId, EntityId recipientId)
        {
            lastResolutionId = resolutionId;
            lastTargetResolutionId = resolutionId;
            lastTargetId = recipientId;

            if (Grant.Lifetime.Lifetime == EffectLifetime.Instant)
            {
                Expired = true;

            }
            else if (Grant.Lifetime.Lifetime == EffectLifetime.LimitedUses)
            {
                RemainingUses--;
                if (RemainingUses <= 0)
                {
                    Expired = true;

                }

            }

        }

        public void OnOwnerTurnEnded()
        {
            if (Expired || Grant == null || Grant.Lifetime.Lifetime != EffectLifetime.TurnCount)
            {
                return;

            }

            RemainingTurns--;
            if (RemainingTurns <= 0)
            {
                Expired = true;

            }

        }

    }
}