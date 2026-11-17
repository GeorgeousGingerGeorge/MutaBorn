using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]

public class SoundEntry
{
    public string soundName;
    public AudioClip[] audioClips;

    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    [Range(0f, 0.5f)] public float pitchVariation = 0f;
    public bool loop = false;

    public AudioMixerGroup outputGroup;
}

[CreateAssetMenu(fileName = "NewSoundBank", menuName = "Audio/Sound Bank")]
public class SoundBank : ScriptableObject
{
    public SoundEntry[] sounds;
}
