using UnityEngine;

namespace MutaBorn.Gameplay
{
    [CreateAssetMenu(fileName = "NewEffectDefinition", menuName = "MutaBorn/Effects/Effect Definition")]
    public sealed class EffectDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string effectId = "new-effect";
        [SerializeField] private string displayName = "New Effect";
        [TextArea(2, 5)]
        [SerializeField] private string description;

        [Header("Shared Operation")]
        [SerializeField] private EffectOperation operation = EffectOperation.None;

        public string Id => effectId;
        public string DisplayName => displayName;
        public string Description => description;
        public EffectOperation Operation => operation;

    }
}
