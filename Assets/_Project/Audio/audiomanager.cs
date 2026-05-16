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

    public void PlaySFX(int index)
    {
        if (index < 0 || index >= soundClips.Length)
        {
            Debug.LogWarning($"AudioManager: Index {index} existiert nicht!");
            return;
        }
        sfxSource.PlayOneShot(soundClips[index], sfxVolume);
    }

    // --- SFX per Name ---

    public void PlaySFX(string clipName)
    {
        AudioClip clip = System.Array.Find(soundClips, c => c.name == clipName);
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: Clip '{clipName}' nicht gefunden!");
            return;
        }
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // --- SFX direkt ---

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip, sfxVolume);
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