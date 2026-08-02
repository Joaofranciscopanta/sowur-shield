using System.Collections.Generic;
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
/// VARIATIONS:
///   A key may have several recordings, named with a numeric suffix:
///     Audio/SFX/sfx_till_soil1, sfx_till_soil2, sfx_till_soil3
///   Play("sfx_till_soil") picks one at random each call and avoids repeating
///   the previous pick, so a repeated action does not sound copy-pasted.
///   A key with a single unsuffixed clip still works unchanged.
///
/// SETUP IN UNITY:
///   Add to a persistent GameObject; assign poolSize (default 5).
///   Optionally pre-assign named clips in the inspector entries list.
///   Inspector entries sharing one key are treated as variations of it.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Pool")]
    [SerializeField] private int poolSize = 5;

    [Header("Named Clips (assign in Inspector)")]
    [Tooltip("Several entries may share one key — they become random variations.")]
    [SerializeField] private NamedClip[] namedClips;

    private AudioSource[] _pool;
    private int _poolIndex;

    // Resolved clip sets per key, and the index played last time for that key.
    private readonly Dictionary<string, AudioClip[]> _variantCache =
        new Dictionary<string, AudioClip[]>(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _lastVariant =
        new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

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

    private void OnDestroy()
    {
        // Leaving a stale Instance behind makes the next scene's manager
        // destroy itself in Awake, silencing all SFX from then on.
        if (Instance == this) Instance = null;
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

        AudioClip[] variants = GetVariants(key);
        if (variants == null || variants.Length == 0)
        {
            Debug.LogWarning($"[SFXManager] Clip not found: '{key}'");
            return;
        }

        PlayClip(PickVariant(key, variants));
    }

    /// <summary>
    /// All clips registered under a key, in inspector-then-Resources order.
    /// Cached after the first lookup, including the empty result.
    /// </summary>
    private AudioClip[] GetVariants(string key)
    {
        if (_variantCache.TryGetValue(key, out var cached))
            return cached;

        var found = new List<AudioClip>();

        // 1. Inspector entries — every entry matching the key is a variation.
        if (namedClips != null)
        {
            foreach (var entry in namedClips)
            {
                if (entry != null && entry.clip != null &&
                    string.Equals(entry.key, key, System.StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(entry.clip);
                }
            }
        }

        // 2/3. Resources, under the key itself and under its file-name form.
        //      Call sites use PascalCase ("CombatHit") while the audio files are
        //      snake_case with an sfx_ prefix ("sfx_combat_hit"), so try both.
        if (found.Count == 0)
        {
            TryLoadFrom(key, found);

            string fileForm = ToFileName(key);
            if (found.Count == 0 && fileForm != key)
                TryLoadFrom(fileForm, found);
        }

        AudioClip[] result = found.ToArray();
        _variantCache[key] = result;
        return result;
    }

    /// <summary>
    /// Loads "Audio/SFX/{name}" plus any numbered variations "{name}1", "{name}2", ...
    /// Probed one by one rather than LoadAll'd, so we never pull in the whole folder.
    /// </summary>
    private static void TryLoadFrom(string name, List<AudioClip> into)
    {
        AudioClip exact = Resources.Load<AudioClip>($"Audio/SFX/{name}");
        if (exact != null) into.Add(exact);

        for (int i = 1; ; i++)
        {
            AudioClip numbered = Resources.Load<AudioClip>($"Audio/SFX/{name}{i}");
            if (numbered == null) break;
            into.Add(numbered);
        }
    }

    /// <summary>
    /// "CombatHit" -> "sfx_combat_hit", matching how the audio files are named.
    /// </summary>
    private static string ToFileName(string key)
    {
        var sb = new System.Text.StringBuilder("sfx_", key.Length + 8);
        for (int i = 0; i < key.Length; i++)
        {
            char c = key[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Random variation, never the same one twice in a row (when there are 2+).
    /// Immediate repetition is what makes a repeated sound read as synthetic.
    /// </summary>
    private AudioClip PickVariant(string key, AudioClip[] variants)
    {
        if (variants.Length == 1) return variants[0];

        int previous = _lastVariant.TryGetValue(key, out int p) ? p : -1;
        int index = Random.Range(0, variants.Length);
        if (index == previous)
            index = (index + 1) % variants.Length;

        _lastVariant[key] = index;
        return variants[index];
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
