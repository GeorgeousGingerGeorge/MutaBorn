using System;

namespace MutaBorn.Gameplay
{
    // Adapts the existing UnitData resource methods without modifying UnitData.
    // It contains no independent resource storage or regeneration rules.
    public sealed class UnitDataResourceTarget : IResourceTarget
    {
        private readonly UnitData data;

        public UnitDataResourceTarget(UnitData data)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));

        }

        public bool TryGetResource(ResourceIdentifier resource, out int current, out int maximum)
        {
            current = 0;
            maximum = 0;

            switch (resource)
            {
                case ResourceIdentifier.Health:
                {
                    current = data.currentHealth;
                    maximum = data.maxHealth;

                    return true;

                }
                case ResourceIdentifier.Mana:
                {
                    current = data.currentMana;
                    maximum = data.maxMana;

                    return true;

                }
                case ResourceIdentifier.ActionPoints:
                {
                    current = data.currentActionPoints;
                    maximum = data.maxActionPoints;

                    return true;

                }
                default:
                {
                    return false;

                }

            }

        }

        public bool TryChangeResource(ResourceIdentifier resource, int amount)
        {
            switch (resource)
            {
                case ResourceIdentifier.Health:
                {
                    data.ModifyHealth(amount);

                    return true;

                }
                case ResourceIdentifier.Mana:
                {
                    data.ModifyMana(amount);

                    return true;

                }
                case ResourceIdentifier.ActionPoints:
                {
                    data.ModifyActionPoints(amount);

                    return true;

                }
                default:
                {
                    return false;

                }

            }

        }

    }

}
