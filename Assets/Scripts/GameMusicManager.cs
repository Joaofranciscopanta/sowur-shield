using UnityEngine;

namespace SowurShield.Core
{

/// <summary>
/// Manages background music during gameplay
/// Singleton pattern with DontDestroyOnLoad to persist between scenes
/// Integrates with Master, Music, and SFX volume controls
/// </summary>
public class GameMusicManager : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;

    // Singleton
    public static GameMusicManager Instance { get; private set; }

    // State
    private float targetVolume;
    private bool isFading = false;
    private float fadeTimer = 0f;
    private float fadeDuration = 0f;
    private float fadeStartVolume = 0f;
    private float fadeTargetVolume = 0f;

    private void Awake()
    {
        // Singleton setup with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Setup audio source
            if (musicSource == null)
                musicSource = GetComponent<AudioSource>();

            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();

            // Configure audio source
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        else
        {
            // Destroy duplicate instances
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // This only runs once when GameMusicManager is first created
        // For subsequent scene loads, we rely on SceneTransitionManager callbacks

        // Stop any menu music that might be playing
        StopMenuMusic();

        // Start gameplay music if enabled
        if (playOnStart && gameplayMusic != null)
        {
            PlayMusic(gameplayMusic, fadeInDuration);
        }
    }

    private void Update()
    {
        // Handle fade transitions
        if (isFading)
        {
            fadeTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(fadeTimer / fadeDuration);

            // Smooth fade curve
            float volume = Mathf.Lerp(fadeStartVolume, fadeTargetVolume, t);
            musicSource.volume = volume;

            // End fade
            if (t >= 1f)
            {
                isFading = false;

                // Stop music if fading out completely
                if (fadeTargetVolume <= 0f && musicSource.isPlaying)
                {
                    musicSource.Stop();
                }
            }
        }
    }

    // ============================================================================
    // PUBLIC API
    // ============================================================================

    /// <summary>
    /// Play a music clip with optional fade in
    /// </summary>
    public void PlayMusic(AudioClip clip, float fadeTime = 0f)
    {
        if (clip == null) return;


        // Cancel any ongoing fade operation
        if (isFading)
        {
            isFading = false;
        }

        // Stop current music if different clip
        if (musicSource.clip != clip)
        {
            musicSource.Stop();
            musicSource.clip = clip;
        }

        // Calculate target volume
        UpdateTargetVolume();

        // Always ensure music is playing
        if (!musicSource.isPlaying)
        {
            // Not playing - start it
            if (fadeTime > 0f)
            {
                // Fade in
                musicSource.volume = 0f;
                musicSource.Play();
                FadeToVolume(targetVolume, fadeTime);
            }
            else
            {
                // Instant start
                musicSource.volume = targetVolume;
                musicSource.Play();
            }
        }
        else if (musicSource.clip == clip && musicSource.volume > 0.01f && !isFading)
        {
            // Already playing same clip at audible volume - just adjust volume if needed
            if (fadeTime > 0f)
            {
                FadeToVolume(targetVolume, fadeTime);
            }
            else
            {
                musicSource.volume = targetVolume;
            }
        }
        else
        {
            // Playing but volume is 0 or very low (from fade out) - restart it
            musicSource.Stop();
            musicSource.clip = clip;
            if (fadeTime > 0f)
            {
                musicSource.volume = 0f;
                musicSource.Play();
                FadeToVolume(targetVolume, fadeTime);
            }
            else
            {
                musicSource.volume = targetVolume;
                musicSource.Play();
            }
        }
    }

    /// <summary>
    /// Stop music with optional fade out
    /// </summary>
    public void StopMusic(float fadeTime = 0f)
    {
        if (!musicSource.isPlaying) return;

        if (fadeTime > 0f)
        {
            // Fade out
            FadeToVolume(0f, fadeTime);
        }
        else
        {
            // Instant stop
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Pause music
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    /// <summary>
    /// Resume paused music
    /// </summary>
    public void ResumeMusic()
    {
        if (!musicSource.isPlaying)
        {
            musicSource.UnPause();
        }
    }

    /// <summary>
    /// Update volume based on player settings (Master Volume * Music Volume)
    /// </summary>
    public void UpdateVolume()
    {
        UpdateTargetVolume();

        if (!isFading)
        {
            musicSource.volume = targetVolume;
        }
    }

    /// <summary>
    /// Change the gameplay music clip
    /// </summary>
    public void SetGameplayMusic(AudioClip clip)
    {
        gameplayMusic = clip;
    }

    // ============================================================================
    // PRIVATE METHODS
    // ============================================================================

    private void UpdateTargetVolume()
    {
        // Get volume settings from PlayerPrefs
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicSettingVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        // Calculate final volume: base volume * master * music setting
        targetVolume = musicVolume * masterVolume * musicSettingVolume;
    }

    private void FadeToVolume(float target, float duration)
    {
        fadeStartVolume = musicSource.volume;
        fadeTargetVolume = target;
        fadeDuration = duration;
        fadeTimer = 0f;
        isFading = true;
    }

    /// <summary>
    /// Stop any menu music that might be playing
    /// </summary>
    private void StopMenuMusic()
    {
        // Find and stop menu music source
        if (MainMenuManager.Instance != null)
        {
            // Menu manager exists, let it handle cleanup
            return;
        }

        // Find any AudioSources playing menu music
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource source in allAudioSources)
        {
            // Stop any AudioSource that's not this one and is playing
            if (source != musicSource && source.isPlaying && source.clip != null)
            {
                // Check if it's likely menu music (not sound effects)
                if (source.loop && source.clip.length > 10f)
                {
                    source.Stop();
                }
            }
        }
    }

    // ============================================================================
    // SCENE TRANSITION INTEGRATION
    // ============================================================================

    /// <summary>
    /// Called when returning to main menu
    /// </summary>
    public void OnReturnToMainMenu()
    {
        // Fade out game music
        StopMusic(fadeOutDuration);
    }

    /// <summary>
    /// Called when starting game from menu
    /// </summary>
    public void OnStartGame()
    {

        // Play gameplay music
        if (gameplayMusic != null)
        {
            PlayMusic(gameplayMusic, fadeInDuration);
        }
        else
        {
        }
    }
}

} // namespace SowurShield.Core
