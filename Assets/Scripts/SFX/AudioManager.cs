using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource loopAudio;
    public static AudioManager Instance;
    public SoundBank[] soundBanks;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
        {
            Debug.LogError("AudioManager requires an AudioSource for sound effects.");
        }

        if (loopAudio == null)
        {
            Debug.LogError("AudioManager requires an AudioSource for looping audio.");
        }
        else
        {
            loopAudio.playOnAwake = false;
            loopAudio.loop = true;
            loopAudio.spatialBlend = 0f;
        }

    }
    public void PlaySound(string soundName)
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is missing");
            return;
        }

        if (soundBanks == null)
        {
            Debug.LogError("No Sound Banks assigned to AudioManager.");
            return;
        }

        for (int i = 0; i < soundBanks.Length; i++)
        {
            SoundBank bank = soundBanks[i];

            if (bank == null || bank.sounds == null)
            {
                continue;
            }

            for (int j = 0; j < bank.sounds.Length; j++)
            {
                SoundEntry sound = bank.sounds[j];

                if (sound == null)
                {
                    continue;
                }

                if (sound.soundName == soundName)
                {
                    if (sound.audioClips == null || sound.audioClips.Length == 0)
                    {
                        Debug.LogWarning("No audio clips assigned to: " + soundName);
                        return;
                    }

                    int randomIndex = Random.Range(0, sound.audioClips.Length);

                    AudioClip clip = sound.audioClips[randomIndex];

                    if (clip != null)
                    {
                        audioSource.outputAudioMixerGroup = sound.outputGroup;

                        float randomPitch = Random.Range(sound.pitch - sound.pitchVariation, sound.pitch + sound.pitchVariation);

                        audioSource.pitch = randomPitch;
                        audioSource.PlayOneShot(clip, sound.volume);
                    }

                    return;
                }
            }
        }

        Debug.LogWarning("Sound not found: " + soundName);

    }

    public void PlayLoop(string soundName)
    {
        if (loopAudio == null)
        {
            Debug.LogError("Looping AudioSource is missing");
            return;
        }

        if (soundBanks == null)
        {
            Debug.LogError("No Sound Banks assigned to AudioManager.");
            return;
        }

        if (loopAudio.isPlaying)
        {
            return;
        }

        for (int i = 0; i < soundBanks.Length; i++)
        {
            SoundBank bank = soundBanks[i];

            if (bank == null || bank.sounds == null)
            {
                continue;
            }

            for (int j = 0; j < bank.sounds.Length; j++)
            {
                SoundEntry sound = bank.sounds[j];

                if (sound == null)
                {
                    continue;
                }

                if (sound.soundName == soundName)
                {
                    if (sound.audioClips == null || sound.audioClips.Length == 0)
                    {
                        Debug.LogWarning("No audio clips assigned to: " + soundName);
                        return;
                    }

                    int randomIndex = Random.Range(0, sound.audioClips.Length);
                    AudioClip clip = sound.audioClips[randomIndex];

                    if (clip != null)
                    {
                        loopAudio.outputAudioMixerGroup = sound.outputGroup;

                        float randomPitch = Random.Range(sound.pitch - sound.pitchVariation,
                        sound.pitch + sound.pitchVariation);

                        loopAudio.pitch = randomPitch;
                        loopAudio.volume = sound.volume;
                        loopAudio.clip = clip;
                        loopAudio.Play();
                    }

                    return;
                }
            }
        }

        Debug.LogWarning("Sound not found: " + soundName);
    }

    public void StopLoop()
    {
        if (loopAudio == null)
        {
            Debug.LogError("Looping AudioSource is missing");
            return;
        }

        loopAudio.Stop();
        loopAudio.clip = null;
    }
}