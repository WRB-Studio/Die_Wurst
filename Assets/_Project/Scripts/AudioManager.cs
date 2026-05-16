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

    private Dictionary<string, AudioSource> _activeLoops = new(); // Laufende Loops

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
    }

    private void Start() {
        if (startMusic != null)
            PlayMusic(startMusic);
    }

    // --- Musik ---

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource.isPlaying && musicSource.clip == clip) return;
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();
    public void PauseMusic() => musicSource.Pause();
    public void ResumeMusic() => musicSource.UnPause();

    // --- SFX per Index ---

    public void PlaySFX(int index, bool loop = false)
    {
        if (index < 0 || index >= soundClips.Length)
        {
            Debug.LogWarning($"AudioManager: Index {index} existiert nicht!");
            return;
        }
        PlaySFXClip(soundClips[index], loop);
    }

    // --- SFX per Name ---

    public void PlaySFX(string clipName, bool loop = false)
    {
        AudioClip clip = System.Array.Find(soundClips, c => c.name == clipName);
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: Clip '{clipName}' nicht gefunden!");
            return;
        }

        PlaySFXClip(clip, loop);
    }

    // --- SFX direkt ---

    public void PlaySFX(AudioClip clip, bool loop = false)
    {
        PlaySFXClip(clip, loop);
    }

    // --- Loop stoppen per Name --- 
    public void StopLoop(string clipName)
    {
        if (_activeLoops.TryGetValue(clipName, out AudioSource source))
        {
            source.Stop();
            Destroy(source);
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
            source.Stop();
            Destroy(source);
        }
        _activeLoops.Clear();
    }
 
    // --- SFX direkt stoppen (einmalige Sounds) ---
 
    public void StopSFX() => sfxSource.Stop();
 
    // --- Interner Helper ---
 
    private void PlaySFXClip(AudioClip clip, bool loop)
    {
        if (loop)
        {
            // Läuft dieser Clip bereits? Dann nicht nochmal starten
            if (_activeLoops.ContainsKey(clip.name))
            {
                Debug.LogWarning($"AudioManager: Loop '{clip.name}' läuft bereits!");
                return;
            }
 
            // Neue AudioSource dynamisch erstellen
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.clip = clip;
            newSource.loop = true;
            newSource.volume = sfxVolume;
            newSource.Play();
 
            _activeLoops[clip.name] = newSource;
        }
        else
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }


    // --- Lautstärke ---

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        sfxSource.volume = sfxVolume;
    }
}