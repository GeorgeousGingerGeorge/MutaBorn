using UnityEngine;

public class OutlineSync : MonoBehaviour
{
    [Header("Original Character")]
    [SerializeField] SkinnedMeshRenderer originalRenderer;
    [Header("Outline Character")]
    [SerializeField] SkinnedMeshRenderer outlineRenderer;

    void Awake() 
    {
        if (originalRenderer == null || outlineRenderer == null) 
        {
            return;
        }

        outlineRenderer.bones = originalRenderer.bones;
        outlineRenderer.rootBone = originalRenderer.rootBone;
    }
}
