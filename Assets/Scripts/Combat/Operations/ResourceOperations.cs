using System;

namespace MutaBorn.Gameplay
{
    // Actual change is not necessarily the requested amount (e.g., healing at max HP).
    // This can be used by a future effect processor when publishing events.
    public readonly struct ResourceChangeResult
    {
        public ResourceIdentifier Resource { get; }
        public int RequestedAmount { get; }
        public int AppliedAmount { get; }
        public int Before { get; }
        public int After { get; }
        public int Maximum { get; }

        public bool Changed => AppliedAmount != 0;

        public ResourceChangeResult(ResourceIdentifier resource, int requestedAmount,
            int before, int after, int maximum)
        {
            Resource = resource;
            RequestedAmount = requestedAmount;
            AppliedAmount = after - before;
            Before = before;
            After = after;
            Maximum = maximum;

        }

    }

    // Pure application layer. Calculations supply an int; this layer applies it.
    // Does not spend ability costs, publish combat events, or handle defeat.
    public static class ResourceOperations
    {
        public static bool TryApply(UnitData data, ResourceIdentifier resource, int amount,
            out ResourceChangeResult result)
        {
            result = default(ResourceChangeResult);

            if (data == null)
            {
                return false;

            }

            return TryApply(new UnitDataResourceTarget(data), resource, amount, out result);

        }

        public static bool TryApply(IResourceTarget target, ResourceIdentifier resource, int amount,
            out ResourceChangeResult result)
        {
            result = default(ResourceChangeResult);

            if (target == null || !target.TryGetResource(resource, out int before, out int maximum))
            {
                return false;

            }

            // A damaged unit's Health is a resource, but attack damage must go
            // through the dedicated Damage Resolver (Step 6), not this operation.
            // Negative Health changes will get a distinct cost/sacrifice API later.
            if (resource == ResourceIdentifier.Health && amount < 0)
            {
                return false;

            }

            if (maximum < 0 || before < 0 || before > maximum)
            {
                return false;

            }

            // Calculate in long to prevent overflow when extremely large values
            // are applied to current int-based UnitData resource values.
            long desired = (long)before + amount;
            long clamped = Math.Min(Math.Max(desired, 0L), (long)maximum);
            int safeDelta = (int)(clamped - before);

            if (safeDelta != 0 && !target.TryChangeResource(resource, safeDelta))
            {
                return false;

            }

            if (!target.TryGetResource(resource, out int after, out int maximumAfter))
            {
                return false;

            }

            result = new ResourceChangeResult(resource, amount, before, after, maximumAfter);

            return true;

        }

        public static bool TryHeal(UnitData data, int amount, out ResourceChangeResult result)
        {
            result = default(ResourceChangeResult);

            if (amount < 0)
            {
                return false;

            }

            return TryApply(data, ResourceIdentifier.Health, amount, out result);

        }

        public static bool TryHeal(IResourceTarget target, int amount, out ResourceChangeResult result)
        {
            result = default(ResourceChangeResult);

            if (amount < 0)
            {
                return false;

            }

            return TryApply(target, ResourceIdentifier.Health, amount, out result);

        }

    }

}
