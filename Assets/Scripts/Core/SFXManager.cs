using UnityEngine;

namespace SowurShield.Core
{

/// <summary>
/// Centralized SFX playback — pooled AudioSources, volume from PlayerPrefs.
/// Clips are assigned in the Inspector or loaded from Resources/Audio/SFX/.
///
/// Usage (anywhere):
///   SFXManager.Play(clip);
///   SFXManager.Play("HarvestCrop");   // loads Resources/Audio/SFX/HarvestCrop
///
/// SETUP IN UNITY:
///   Add to a persistent GameObject; assign poolSize (default 5).
///   Optionally pre-assign named clips in the inspector entries list.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Pool")]
    [SerializeField] private int poolSize = 5;

    [Header("Named Clips (assign in Inspector)")]
    [SerializeField] private NamedClip[] namedClips;

    private AudioSource[] _pool;
    private int _poolIndex;

    [System.Serializable]
    public class NamedClip
    {
        public string key;
        public AudioClip clip;
    }

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildPool();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void BuildPool()
    {
        _pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"SFX_Source_{i}");
            go.transform.SetParent(transform);
            _pool[i] = go.AddComponent<AudioSource>();
            _pool[i].playOnAwake = false;
            _pool[i].loop = false;
        }
    }

    // =========================================================================
    // Static shortcuts
    // =========================================================================

    public static void Play(AudioClip clip)    => Instance?.PlayClip(clip);
    public static void Play(string clipKey)    => Instance?.PlayByKey(clipKey);

    // =========================================================================
    // Public instance API
    // =========================================================================

    public void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource source = GetNextSource();
        source.clip   = clip;
        source.volume = GetVolume();
        source.Play();
    }

    public void PlayByKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        // 1. Check named clips in inspector
        if (namedClips != null)
        {
            foreach (var entry in namedClips)
            {
                if (string.Equals(entry.key, key, System.StringComparison.OrdinalIgnoreCase) &&
                    entry.clip != null)
                {
                    PlayClip(entry.clip);
                    return;
                }
            }
        }

        // 2. Try Resources/Audio/SFX/{key}
        AudioClip loaded = Resources.Load<AudioClip>($"Audio/SFX/{key}");
        if (loaded != null)
        {
            PlayClip(loaded);
            return;
        }

        Debug.LogWarning($"[SFXManager] Clip not found: '{key}'");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private AudioSource GetNextSource()
    {
        // Round-robin through pool
        AudioSource source = _pool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % _pool.Length;

        // If the chosen source is still playing, find a free one
        if (source.isPlaying)
        {
            foreach (var s in _pool)
            {
                if (!s.isPlaying)
                    return s;
            }
            // All busy — interrupt the oldest (round-robin source)
        }

        return source;
    }

    private float GetVolume()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float sfx    = PlayerPrefs.GetFloat("SFXVolume", 1f);
        return master * sfx;
    }
}

} // namespace SowurShield.Core
