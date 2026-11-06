using UnityEngine;

// Optional placement override: put this on a target, or its parent, to position its banner precisely.
[DisallowMultipleComponent]
public sealed class MissionObjectiveMarkerAnchor : MonoBehaviour
{
    [SerializeField] private Transform anchorPoint;
    [SerializeField] private Vector3 worldOffset;

    public Vector3 WorldPosition => (anchorPoint != null ? anchorPoint.position : transform.position) + worldOffset;
}
