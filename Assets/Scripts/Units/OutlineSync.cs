using UnityEngine;
using System.Collections.Generic;

public class OutlineSync : MonoBehaviour
{
    [Header("Original Character")]
    [SerializeField] List<SkinnedMeshRenderer> originalRenderer = new List<SkinnedMeshRenderer>();
    [Header("Outline Character")]
    [SerializeField] List<SkinnedMeshRenderer> outlineRenderer = new List<SkinnedMeshRenderer>();

    void Awake() 
    {
        if (originalRenderer == null || outlineRenderer == null) 
        {
            return;
        }
        for (int bone_count = 0; bone_count < originalRenderer.Count; bone_count++)
        {
            outlineRenderer[bone_count].bones = originalRenderer[bone_count].bones;
            outlineRenderer[bone_count].rootBone = originalRenderer[bone_count].rootBone;
        }
    }
}
