using System;
using UnityEngine;
using MutaBorn.Gameplay;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource loopAudio;
    public static AudioManager Instance;
    public SoundBank[] soundBanks;

    private readonly System.Collections.Generic.Dictionary<UnitController, float> hurtCooldowns =
    new System.Collections.Generic.Dictionary<UnitController, float>();

    private const float hurtCooldown = 0.3f;
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

    private void OnEnable()
    {

        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("AUDIO MANAGER SUBSCRIBING TO COMBAT EVENTS!");

        CombatActionExecutor.DamageApplied += HandleDamageApplied;
        CombatActionExecutor.UnitDefeated += HandleUnitDefeat;

    }

    private void OnDisable()
    {

        SceneManager.sceneLoaded -= OnSceneLoaded;

        CombatActionExecutor.DamageApplied -= HandleDamageApplied;
        CombatActionExecutor.UnitDefeated -= HandleUnitDefeat;
    }


    private void Start()
    {
        SubscribeToMissionEvents();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SubscribeToMissionEvents();
    }

    private void SubscribeToMissionEvents()
    {
        if (MissionObjectiveManager.Instance != null)
        {
            MissionObjectiveManager.Instance.MissionFinished -= HandleMissionFinished;
            MissionObjectiveManager.Instance.MissionFinished += HandleMissionFinished;
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

                    int randomIndex = UnityEngine.Random.Range(0, sound.audioClips.Length);

                    AudioClip clip = sound.audioClips[randomIndex];

                    if (clip != null)
                    {
                        audioSource.outputAudioMixerGroup = sound.outputGroup;

                        float randomPitch = UnityEngine.Random.Range(sound.pitch - sound.pitchVariation, sound.pitch + sound.pitchVariation);

                        audioSource.pitch = randomPitch;
                        audioSource.PlayOneShot(clip, sound.volume);
                    }

                    return;
                }
            }
        }

        Debug.LogWarning("Sound not found: " + soundName);

    }

    public void PlaySoundAtPosition(string soundName, Vector3 position)
    {
        if (soundBanks == null)
        {
            Debug.LogError("No Sound Banks assigned to AudioManager.");
            return;
        }

        foreach (SoundBank bank in soundBanks)
        {
            if (bank == null || bank.sounds == null)
                continue;

            foreach (SoundEntry sound in bank.sounds)
            {
                if (sound == null || sound.soundName != soundName)
                    continue;

                if (sound.audioClips == null || sound.audioClips.Length == 0)
                    return;

                AudioClip clip = sound.audioClips[
                    UnityEngine.Random.Range(0, sound.audioClips.Length)];

                if (clip == null)
                    return;

                GameObject soundObject = new GameObject("Positional Audio: " + soundName);
                soundObject.transform.position = position;

                AudioSource source = soundObject.AddComponent<AudioSource>();
                source.clip = clip;
                source.volume = sound.volume;
                source.pitch = UnityEngine.Random.Range(
                    sound.pitch - sound.pitchVariation,
                    sound.pitch + sound.pitchVariation);

                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;

                source.minDistance = 20f;
                source.maxDistance = 120f;

                source.outputAudioMixerGroup = sound.outputGroup;

                source.Play();

                Destroy(soundObject, clip.length / Mathf.Abs(source.pitch));

                return;
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

                    int randomIndex = UnityEngine.Random.Range(0, sound.audioClips.Length);
                    AudioClip clip = sound.audioClips[randomIndex];

                    if (clip != null)
                    {
                        loopAudio.outputAudioMixerGroup = sound.outputGroup;

                        float randomPitch = UnityEngine.Random.Range(sound.pitch - sound.pitchVariation,
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

    private void HandleDamageApplied(CombatDamageEvent report)
    {
        UnitController target = report.Target;

        if (report.ReducedHealthToZero) return;

        // Select the impact sound based on damage type.
        string impactSound = report.DamageType switch
        {
            DamageType.Slash => "Stab",
            DamageType.Pierce => "Stab",
            DamageType.Blunt => "Bonk",
            DamageType.Arcane => "Magic Sound",
            DamageType.Fire => "Fire Sound",
            DamageType.Ice => "Ice Sound",
            DamageType.Lightning => "Lightning Sound",
            DamageType.Poison => "Splat",
            DamageType.Acid => "Acid Sound",
            DamageType.Dark => "Dark Sound",
            DamageType.Holy => "Holy Sound",
            DamageType.Wind => "Wind Sound",
            DamageType.Earth => "Rock Sound",
            DamageType.Water => "Splash",
            DamageType.Nature => "Splat",
            DamageType.True => "Wack",
            _ => "Thunk"
        };
        PlaySoundAtPosition(impactSound, target.transform.position);  
        if (hurtCooldowns.ContainsKey(target) && Time.time < hurtCooldowns[target])
            return;

        hurtCooldowns[target] = Time.time + hurtCooldown;

        string hurtSound = target.raceName + " Hurt";
        PlaySoundAtPosition(hurtSound, target.transform.position);
    }

    private void HandleUnitDefeat(UnitController attacker, UnitController target)
    {
        Debug.Log("DEATH AUDIO EVENT RECEIVED!");

        string soundName = target.raceName + " Death";
        PlaySoundAtPosition(soundName, target.transform.position);
    }

    private void HandleMissionFinished(MissionStatus result)
    {
        if (result == MissionStatus.Victory)
        {
            PlaySound("Mission Complete");
        }
        else if (result == MissionStatus.Defeat)
        {
            PlaySound("Mission Failed");
        }
    }
}