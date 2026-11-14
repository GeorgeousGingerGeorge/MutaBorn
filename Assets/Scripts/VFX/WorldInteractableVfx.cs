using UnityEngine;

public sealed class WorldInteractableVfx : MonoBehaviour
{
    [SerializeField] private ParticleSystem interactionPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private Color tint = new Color(0.95f, 0.78f, 0.35f);

    public void Play()
    {
        WorldTrapVfx.Spawn(interactionPrefab, transform.position + offset, tint);

    }
}
