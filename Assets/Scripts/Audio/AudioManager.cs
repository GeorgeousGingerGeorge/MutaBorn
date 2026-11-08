using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private AudioSource audioSource;
    public static AudioManager Instance;

    [Range(0.0f, 1.0f)]
    public float volume = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
