using System;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    [Serializable]
    public struct DamageIndicatorStyle
    {
        public DamageType damageType;
        public Sprite backgroundSprite;
        public Color backgroundColor;
        public Color numberColor;
        public string label;

    }

    // Optional Resources asset: Resources/DamageIndicators/DamageIndicatorPalette.asset
    // Each damage type can have its own PNG and color without separate prefabs.
    [CreateAssetMenu(fileName = "DamageIndicatorPalette", menuName = "MutaBorn/UI/Damage Indicator Palette")]
    public sealed class DamageIndicatorPalette : ScriptableObject
    {
        [Header("Types")]
        [SerializeField] private DamageIndicatorStyle[] styles = Array.Empty<DamageIndicatorStyle>();

        [Header("Combat Feedback Icons (Optional)")]
        [Tooltip("Optional. Leave empty to load HealIcon.png from Resources/DamageIndicators.")]
        [SerializeField] private Sprite healIcon;
        [Tooltip("Optional. Leave empty to load APIcon.png from Resources/DamageIndicators.")]
        [SerializeField] private Sprite apIcon;
        [Tooltip("Optional. Leave empty to load ManaIcon.png from Resources/DamageIndicators.")]
        [SerializeField] private Sprite manaIcon;
        [Tooltip("Optional. Leave empty to load BuffIcon.png from Resources/DamageIndicators.")]
        [SerializeField] private Sprite buffIcon;
        [Tooltip("Optional. Leave empty to load DebuffIcon.png from Resources/DamageIndicators.")]
        [SerializeField] private Sprite debuffIcon;
        [Tooltip("Optional. Leave empty to load BlockedIcon.png from Resources/DamageIndicators.")]
        [SerializeField] private Sprite blockedIcon;
        [Tooltip("Optional. Leave empty to load GuardActivationIcon.png from Resources/DamageIndicators.")]
        [SerializeField] private Sprite guardActivationIcon;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float lifetime = 1.05f;
        [SerializeField, Min(0f)] private float holdDuration = 0.16f;
        [SerializeField, Min(0f)] private float driftSpeed = 25f;
        [SerializeField, Min(0f)] private float acceleration = 220f;
        [SerializeField, Min(0f)] private float spawnHeight = 1.1f;
        [SerializeField, Min(1f)] private float sideSpacing = 46f;
        [SerializeField, Min(0f)] private float clusterWindow = 0.25f;

        public Sprite HealIcon => healIcon;
        public Sprite APIcon => apIcon;
        public Sprite ManaIcon => manaIcon;
        public Sprite BuffIcon => buffIcon;
        public Sprite DebuffIcon => debuffIcon;
        public Sprite BlockedIcon => blockedIcon;
        public Sprite GuardActivationIcon => guardActivationIcon;

        public float Lifetime => Mathf.Max(0.01f, lifetime);
        public float HoldDuration => Mathf.Clamp(holdDuration, 0f, Lifetime);
        public float DriftSpeed => Mathf.Max(0f, driftSpeed);
        public float Acceleration => Mathf.Max(0f, acceleration);
        public float SpawnHeight => Mathf.Max(0f, spawnHeight);
        public float SideSpacing => Mathf.Max(1f, sideSpacing);
        public float ClusterWindow => Mathf.Max(0f, clusterWindow);

        public bool TryGetStyle(DamageType type, out DamageIndicatorStyle style)
        {
            if (styles != null)
            {
                foreach (DamageIndicatorStyle entry in styles)
                {
                    if (entry.damageType == type)
                    {
                        style = entry;

                        return true;

                    }

                }

            }

            style = default(DamageIndicatorStyle);

            return false;

        }

    }

}
