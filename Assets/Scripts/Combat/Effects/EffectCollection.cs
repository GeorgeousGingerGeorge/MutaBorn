using System.Collections.Generic;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    // Optional reusable container for multiple effect grants.
    // Future mutations, actions, equipment, and consumables can reference
    // collections or embed a List<EffectGrant> when per-source tuning is preferred.
    [CreateAssetMenu(fileName = "NewEffectCollection", menuName = "MutaBorn/Effects/Effect Collection")]
    public sealed class EffectCollection : ScriptableObject
    {
        [SerializeField] private List<EffectGrant> effects = new List<EffectGrant>();

        public IReadOnlyList<EffectGrant> Effects => effects;

    }
}
