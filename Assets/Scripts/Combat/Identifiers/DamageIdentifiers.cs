using System;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    // Describes what kind of damage is dealt, independently of delivery or defenses.
    // Explicit values keep previously serialized entries stable.
    public enum DamageType
    {
        Unspecified = 0,

        Slash = 1,
        Pierce = 2,
        Blunt = 3,
        Arcane = 4,

        Fire = 11,
        Ice = 12,
        Lightning = 13,
        Poison = 14,
        Acid = 15,
        Dark = 16,
        Holy = 17,
        Wind = 18,
        Earth = 19,
        Water = 20,
        Nature = 21,

        // Ignores Armor, Ward, resistances and Guard.
        True = 22

    }

    // Melee/ranged describe delivery, not damage type or an area targeting shape.
    public enum DamageDeliveryType
    {
        Unspecified = 0,

        Melee = 1,
        Ranged = 2,
        Magic = 3,

        Direct = 4,
        Environmental = 5

    }


    // An additional fixed-damage part of an attack, such as Fire on a sword.
    [Serializable]
    public struct DamageComponent
    {
        [SerializeField] private DamageType damageType;
        [SerializeField, Min(0)] private int amount;

        public DamageType Type => damageType;
        public int Amount => amount;

        public DamageComponent(DamageType type, int damage)
        {
            damageType = type;
            amount = damage;

        }

    }

    // Positive percentage resists the type; negative percentage is weakness.
    // 100%+ resistance is permitted. Immunity is a separate future mechanic.
    [Serializable]
    public struct DamageResistance
    {
        [SerializeField] private DamageType damageType;
        [SerializeField] private float percent;

        public DamageType Type => damageType;
        public float Percent => percent;

        public DamageResistance(DamageType type, float resistancePercent)
        {
            damageType = type;
            percent = resistancePercent;

        }

    }

}
