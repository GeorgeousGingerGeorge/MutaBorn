using UnityEngine;

[System.Serializable]
public class SoundEffect
{
    public string SoundName;
    public AudioClip audioClip;
}

public class AudioManager : MonoBehaviour
{
    private AudioSource audioSource;
    public static AudioManager Instance;

    [Range(0.0f, 1.0f)]
    public float volume = 1.0f;
    public SoundEffect[] soundEffects;

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
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
    public void PlaySound(string soundName)
    {
        SoundEffect sound = System.Array.Find(soundEffects, s => s.SoundName == soundName);

        if (sound != null)
        {
            PlaySound(sound.audioClip);
        }
        else
        {
            Debug.LogWarning("Sound not found: " + soundName);
        }
    }
}
