using MutaBorn.Gameplay;
using UnityEngine;

public static class ElementVfxColors
{
    public static Color For(DamageType type)
    {

        return type switch
        {
            DamageType.Fire => new Color(1f, 0.3f, 0.06f),
            DamageType.Ice => new Color(0.4f, 0.9f, 1f),
            DamageType.Lightning => new Color(0.98f, 0.9f, 0.25f),
            DamageType.Poison => new Color(0.36f, 0.12f, 0.46f),
            DamageType.Acid => new Color(0.7f, 1f, 0.16f),
            DamageType.Dark => new Color(0.54f, 0.2f, 0.85f),
            DamageType.Holy => new Color(1f, 0.95f, 0.67f),
            DamageType.Wind => new Color(0.55f, 0.98f, 0.8f),
            DamageType.Earth => new Color(0.76f, 0.52f, 0.24f),
            DamageType.Water => new Color(0.15f, 0.55f, 1f),
            DamageType.Nature => new Color(0.25f, 0.75f, 0.36f),
            DamageType.Arcane => new Color(0.52f, 0.4f, 1f),
            DamageType.True => Color.white,
            _ => new Color(0.8f, 0.85f, 0.9f)

        };

    }
}
