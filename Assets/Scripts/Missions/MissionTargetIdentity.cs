using UnityEngine;

// Optional metadata on a unit or an object prefab, for objectives that target
// something more specific than a faction. PrefabTypeId can be reused on many instances.
public sealed class MissionTargetIdentity : MonoBehaviour
{
    [Header("Objective Matching")]
    [SerializeField] private string targetId;
    [SerializeField] private string prefabTypeId;
    [SerializeField] private string categoryId;

    public string TargetId => targetId;
    public string PrefabTypeId => prefabTypeId;
    public string CategoryId => categoryId;

}
