using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music")]
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameOverMusic;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip knifeSfx;
    [SerializeField] private AudioClip jumpSfx;
    [SerializeField] private AudioClip yaySfx;
    [SerializeField] private AudioClip coinCountSfx;
    [SerializeField] private AudioClip hitOofSfx;
    [SerializeField] private AudioClip hitOuchSfx;
    [SerializeField] private AudioClip grinderSfx;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private readonly Dictionary<string, AudioSource> activeLoops = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    private void Start()
    {
        PlayGameplayMusic();
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null)
        {
            return;
        }

        if (musicSource.isPlaying && musicSource.clip == clip)
        {
            return;
        }

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (musicSource != null)
        {
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }

    public void RestartMusic()
    {
        if (musicSource == null || musicSource.clip == null)
        {
            return;
        }

        musicSource.Stop();
        musicSource.time = 0f;
        musicSource.Play();
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }

    public void PlayMainMenuMusic()
    {
        PlayMusic(mainMenuMusic);
    }

    public void PlayGameOverMusic()
    {
        PlayMusic(gameOverMusic);
    }

    public void PlayKnifeSfx(float volume = 1f)
    {
        PlayOneShotSfx(knifeSfx, volume);
    }

    public void PlayJumpSfx(float volume = 1f)
    {
        PlayOneShotSfx(jumpSfx, volume);
    }

    public void PlayYaySfx(float volume = 1f)
    {
        PlayOneShotSfx(yaySfx, volume);
    }

    public void PlayCoinCountSfx(float volume = 1f)
    {
        PlayOneShotSfx(coinCountSfx, volume);
    }

    public void PlayHitOofSfx(float volume = 1f)
    {
        PlayOneShotSfx(hitOofSfx, volume);
    }

    public void PlayHitOuchSfx(float volume = 1f)
    {
        PlayOneShotSfx(hitOuchSfx, volume);
    }

    public void PlayGrinderSfx(float volume = 1f)
    {
        PlayOneShotSfx(grinderSfx, volume);
    }

    public void PlayGrinderSfxBlocking(float volume = 1f)
    {
        PlayBlockingSfx(grinderSfx, volume);
    }

    public void StopSFX()
    {
        if (sfxSource != null)
        {
            sfxSource.Stop();
        }
    }

    private void PlayOneShotSfx(AudioClip clip, float volume)
    {
        PlaySfxClip(clip, false, volume);
    }

    private void PlayBlockingSfx(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        if (sfxSource.isPlaying && sfxSource.clip == clip)
        {
            return;
        }

        sfxSource.clip = clip;
        sfxSource.volume = sfxVolume * volume;
        sfxSource.loop = false;
        sfxSource.Play();
    }

    private void PlaySfxClip(AudioClip clip, bool loop, float volume)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        float finalVolume = sfxVolume * volume;

        if (loop)
        {
            if (activeLoops.ContainsKey(clip.name))
            {
                return;
            }

            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.clip = clip;
            newSource.loop = true;
            newSource.volume = finalVolume;
            newSource.Play();
            activeLoops[clip.name] = newSource;
            return;
        }

        sfxSource.PlayOneShot(clip, finalVolume);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);

        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }
}
