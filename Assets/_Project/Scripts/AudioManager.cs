using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music")]
    [SerializeField] private AudioClip startMusic; // <- zieh hier deinen Clip rein

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Sounds")]
    public AudioClip[] soundClips;  // Hier ziehst du deine 3 Clips rein

    private Dictionary<string, AudioSource> _activeLoops = new Dictionary<string, AudioSource>(); // Laufende Loops

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();

        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
    }

    private void Start() {
        if (startMusic != null)
            PlayMusic(startMusic);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // --- Musik ---

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        EnsureAudioSources();

        if (musicSource.isPlaying && musicSource.clip == clip) return;
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        EnsureAudioSources();
        musicSource.Stop();
    }

    public void PauseMusic()
    {
        EnsureAudioSources();
        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        EnsureAudioSources();
        musicSource.UnPause();
    }

    // --- SFX per Index ---

    public void PlaySFX(int index, bool loop = false, float volume = 1f)
    {
        if (soundClips == null)
        {
            return;
        }

        if (index < 0 || index >= soundClips.Length)
        {
            Debug.LogWarning($"AudioManager: Index {index} existiert nicht!");
            return;
        }
        PlaySFXClip(soundClips[index], loop, volume);
    }

    // --- SFX per Name ---

    public void PlaySFX(string clipName, bool loop = false, float volume = 1f)
    {
        if (soundClips == null)
        {
            return;
        }

        AudioClip clip = System.Array.Find(soundClips, c => c.name == clipName);
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: Clip '{clipName}' nicht gefunden!");
            return;
        }

        PlaySFXClip(clip, loop, volume);
    }

    // --- SFX direkt abspielen, aber nur wenn er nicht schon läuft (für einmalige Sounds) ---
    
    public void PlaySFXBlocking(string clipName, float volume = 1f)
    {
        EnsureAudioSources();

        AudioClip clip = System.Array.Find(soundClips, c => c.name == clipName);
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: Clip '{clipName}' nicht gefunden!");
            return;
        }

        // Prüfen ob genau dieser Clip gerade läuft
        if (sfxSource.isPlaying && sfxSource.clip == clip) return;

        sfxSource.clip = clip;
        sfxSource.volume = sfxVolume * volume;
        sfxSource.Play();
    }

    // --- SFX direkt ---

    public void PlaySFX(AudioClip clip, bool loop = false, float volume = 1f)
    {
        PlaySFXClip(clip, loop, volume);
    }

    // --- Loop stoppen per Name --- 
    public void StopLoop(string clipName)
    {
        if (_activeLoops.TryGetValue(clipName, out AudioSource source))
        {
            if (source != null)
            {
                source.Stop();
                Destroy(source);
            }

            _activeLoops.Remove(clipName);
        }
        else
        {
            Debug.LogWarning($"AudioManager: Kein aktiver Loop '{clipName}' gefunden!");
        }
    }

    // --- Alle Loops stoppen ---
    public void StopAllLoops()
    {
        foreach (var source in _activeLoops.Values)
        {
            if (source == null)
            {
                continue;
            }

            source.Stop();
            Destroy(source);
        }
        _activeLoops.Clear();
    }
 
    // --- SFX direkt stoppen (einmalige Sounds) ---
 
    public void StopSFX()
    {
        EnsureAudioSources();
        sfxSource.Stop();
    }
 
    // --- Interner Helper ---
    private void PlaySFXClip(AudioClip clip, bool loop, float volume) // 1f = maxLautstärke
    {
        EnsureAudioSources();

        float finalVolume = sfxVolume * volume; // globale Lautstärke * lokale Lautstärke

        if (loop)
        {
            if (_activeLoops.ContainsKey(clip.name))
            {
                Debug.LogWarning($"AudioManager: Loop '{clip.name}' läuft bereits!");
                return;
            }
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.clip = clip;
            newSource.loop = true;
            newSource.volume = finalVolume;
            newSource.Play();
            _activeLoops[clip.name] = newSource;
        }
        else
        {
            sfxSource.PlayOneShot(clip, finalVolume);
        }
}


    // --- Lautstärke ---

    public void SetMusicVolume(float value)
    {
        EnsureAudioSources();
        musicVolume = Mathf.Clamp01(value);
        musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float value)
    {
        EnsureAudioSources();
        sfxVolume = Mathf.Clamp01(value);
        sfxSource.volume = sfxVolume;
    }
}
