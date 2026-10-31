namespace MutaBorn.Gameplay
{
    public enum StatIdentifier
    {
        None = 0,

        Strength = 1,
        Dexterity = 2,
        Vitality = 3,
        Constitution = 4,
        Perception = 5,
        Intelligence = 6,

        MaxHealth = 20,
        MaxMana = 21,
        MaxActionPoints = 22,

        MeleeDamage = 30,
        RangedDamage = 31,
        MagicDamage = 32,

        PhysicalDefense = 40,
        MagicDefense = 41,
        Will = 42

    }

    public enum ResourceIdentifier
    {
        // -1 preserves the serialized values of existing Health/Mana/AP assets.
        None = -1,
        Health = 0,
        Mana = 1,
        ActionPoints = 2

    }

}