using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private AudioClip gameplayMusic; // Fallback single farm track
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Seasonal Farm Tracks (index 0=Spring 1=Summer 2=Fall 3=Winter)")]
    [SerializeField] private AudioClip[] seasonalFarmTracks = new AudioClip[4];

    [Header("Scene Tracks")]
    [SerializeField] private AudioClip combatMusic;
    [SerializeField] private AudioClip menuMusic;

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

        StopMenuMusic();

        if (playOnStart)
            PlayFarmMusic();

        // Subscribe to season changes for live crossfading
        if (GameTimeController.instance != null)
            GameTimeController.instance.OnSeasonChanged += OnSeasonChanged;

        // Switch tracks on every subsequent scene load. This used to be driven by
        // SceneTransitionManager.OnSceneLoaded calling OnStartGame/OnEnterCombat, but
        // SceneTransitionManager is never instantiated anywhere in the project — every
        // call site falls back to a plain SceneManager.LoadScene — so those callbacks
        // never fired and combat music never played (confirmed: farm music kept
        // playing straight through a battle). Listening directly here matches the
        // pattern already used by AnimalPurchaseLoader/GameSceneReloadHandler.
        SceneManager.sceneLoaded += OnAnySceneLoaded;
    }

    private void OnDestroy()
    {
        if (GameTimeController.instance != null)
            GameTimeController.instance.OnSeasonChanged -= OnSeasonChanged;

        SceneManager.sceneLoaded -= OnAnySceneLoaded;
    }

    private void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // MainMenu is intentionally not handled here — MainMenuManager already plays its
        // own menu music via a separate AudioSource and explicitly calls
        // GameMusicManager.StopMusic() to avoid overlap.
        switch (scene.name)
        {
            case "SampleScene":
                OnStartGame();
                break;
            case "CombatScene":
                OnEnterCombat();
                break;
        }
    }

    private void OnSeasonChanged(string newSeason)
    {
        // Only crossfade if we're playing farm music (not combat/menu)
        if (musicSource == null || combatMusic == musicSource.clip || menuMusic == musicSource.clip)
            return;
        PlayFarmMusic();
    }

    /// <summary>
    /// Play the seasonal farm track, falling back to gameplayMusic if none assigned.
    /// </summary>
    public void PlayFarmMusic()
    {
        AudioClip track = GetCurrentSeasonTrack();
        if (track != null)
            PlayMusic(track, fadeInDuration);
    }

    private AudioClip GetCurrentSeasonTrack()
    {
        if (seasonalFarmTracks != null && seasonalFarmTracks.Length == 4)
        {
            int idx = GameTimeController.instance != null
                ? GameTimeController.instance.GetCurrentSeasonIndex()
                : 0;
            idx = Mathf.Clamp(idx, 0, 3);
            if (seasonalFarmTracks[idx] != null)
                return seasonalFarmTracks[idx];
        }
        return gameplayMusic; // Fallback
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

    private System.Collections.IEnumerator PlayMusicWhenLoaded(AudioClip clip, float fadeTime)
    {
        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        while (clip != null && clip.loadState == AudioDataLoadState.Loading)
        {
            yield return null;
        }

        if (clip != null && clip.loadState == AudioDataLoadState.Loaded)
        {
            PlayMusic(clip, fadeTime);
        }
    }

    /// <summary>
    /// Play a music clip with optional fade in
    /// </summary>
    public void PlayMusic(AudioClip clip, float fadeTime = 0f)
    {
        if (clip == null) return;

        // On WebGL, AudioClip decoding is async — calling Play() before the
        // clip finishes loading throws an unhandled engine exception. Clips
        // also sit in Unloaded (not Loading) until LoadAudioData() is called,
        // since "Preload Audio Data" is off on these assets.
        if (clip.loadState == AudioDataLoadState.Unloaded || clip.loadState == AudioDataLoadState.Loading)
        {
            StartCoroutine(PlayMusicWhenLoaded(clip, fadeTime));
            return;
        }

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
            if (source != musicSource && source.isPlaying && source.clip != null
                && source.clip.loadState == AudioDataLoadState.Loaded)
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
    /// Called when transitioning to the farm scene.
    /// </summary>
    public void OnStartGame()
    {
        PlayFarmMusic();
    }

    /// <summary>
    /// Called when entering the combat scene.
    /// </summary>
    public void OnEnterCombat()
    {
        if (combatMusic != null)
            PlayMusic(combatMusic, fadeInDuration);
    }

    /// <summary>
    /// Called when returning from combat to the farm scene.
    /// </summary>
    public void OnExitCombat()
    {
        PlayFarmMusic();
    }

    /// <summary>
    /// Play the menu music track.
    /// </summary>
    public void OnEnterMainMenu()
    {
        if (menuMusic != null)
            PlayMusic(menuMusic, fadeInDuration);
        else
            StopMusic(fadeOutDuration);
    }
}

} // namespace SowurShield.Core
