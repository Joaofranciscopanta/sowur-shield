using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SowurShield.Animals;
using SowurShield.Core;

namespace SowurShield.Combat
{

/// <summary>
/// A saved team layout: who fought, where they stood, and in which combat mode.
///
/// The point is to stop re-picking the same animals one by one before every battle.
/// Repeating a stage previously meant finding each animal again in a 20-entry list that
/// showed 9 at a time — the friction that hurts most, because it happens on every retry.
/// </summary>
[System.Serializable]
public class TeamProfile
{
    public string profileName = "";

    /// <summary>Animal ids (see TeamAssemblerData.GetAnimalId) with their grid slots.</summary>
    public List<Entry> entries = new List<Entry>();

    public CombatMode combatMode = CombatMode.ActivePause;

    [System.Serializable]
    public class Entry
    {
        public string animalId;
        public int x;
        public int y;

        public Vector2Int Position => new Vector2Int(x, y);
    }

    public int Count => entries.Count;
}

/// <summary>
/// Stores the player's saved team profiles and applies them back onto the assembler.
///
/// Persisted through ISaveable into the save slot rather than PlayerPrefs: profiles are
/// player content, and PlayerPrefs is shared across every save slot, so a second save file
/// would inherit the first one's teams.
/// </summary>
public class TeamProfileManager : MonoBehaviour, ISaveable
{
    /// <summary>How many profiles the player can keep. Three covers the common cases.</summary>
    public const int MaxProfiles = 3;

    private const string SaveKeyCount = "team_profile_count";
    private const string SaveKeyPrefix = "team_profile_";

    private List<TeamProfile> profiles = new List<TeamProfile>();

    private static TeamProfileManager instance;
    public static TeamProfileManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<TeamProfileManager>(FindObjectsInactive.Include);

                if (instance == null)
                {
                    var go = new GameObject("TeamProfileManager");
                    instance = go.AddComponent<TeamProfileManager>();

                    // DontDestroyOnLoad throws outside play mode.
                    if (Application.isPlaying)
                        DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        if (Application.isPlaying)
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Registering only in Awake loses the data: SaveManager.Start() calls LoadGame(),
        // and an object that registered before that never receives it.
        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterSaveable(this);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public IReadOnlyList<TeamProfile> Profiles => profiles;

    public bool CanAddProfile => profiles.Count < MaxProfiles;

    /// <summary>
    /// Save the currently assembled team as a profile. Overwrites a profile of the same
    /// name rather than creating a duplicate.
    /// </summary>
    public TeamProfile SaveCurrentTeam(string profileName)
    {
        var team = TeamAssemblerData.Instance.team;
        if (team == null || team.Count == 0) return null;

        var profile = new TeamProfile
        {
            profileName = string.IsNullOrWhiteSpace(profileName)
                ? $"Team {profiles.Count + 1}"
                : profileName.Trim(),
            combatMode = TeamAssemblerData.Instance.combatMode
        };

        foreach (var member in team)
        {
            if (member == null || string.IsNullOrEmpty(member.animalId)) continue;
            profile.entries.Add(new TeamProfile.Entry
            {
                animalId = member.animalId,
                x = member.gridPosition.x,
                y = member.gridPosition.y
            });
        }

        if (profile.entries.Count == 0) return null;

        int existing = profiles.FindIndex(p => p.profileName == profile.profileName);
        if (existing >= 0)
            profiles[existing] = profile;
        else if (CanAddProfile)
            profiles.Add(profile);
        else
            return null;

        return profile;
    }

    public void DeleteProfile(TeamProfile profile)
    {
        if (profile != null) profiles.Remove(profile);
    }

    /// <summary>What happened when a profile was applied, so the UI can report it.</summary>
    public struct ApplyResult
    {
        public int placed;
        public int missing;
        public List<string> missingNames;
    }

    /// <summary>
    /// Re-apply a saved profile to the current team.
    ///
    /// Animals that no longer exist (sold, or a save loaded on a different farm) are
    /// reported as missing rather than discarding the profile — a team of five should not
    /// be lost because one animal is gone.
    /// </summary>
    public ApplyResult Apply(TeamProfile profile, IReadOnlyList<Animal> availableAnimals)
    {
        var result = new ApplyResult { missingNames = new List<string>() };
        if (profile == null) return result;

        var byId = new Dictionary<string, Animal>();
        if (availableAnimals != null)
        {
            foreach (var animal in availableAnimals)
            {
                if (animal == null) continue;
                byId[TeamAssemblerData.GetAnimalId(animal)] = animal;
            }
        }

        TeamAssemblerData.Instance.ClearTeam();
        TeamAssemblerData.Instance.combatMode = profile.combatMode;

        foreach (var entry in profile.entries)
        {
            if (byId.TryGetValue(entry.animalId, out Animal animal) && animal != null)
            {
                if (TeamAssemblerData.Instance.AddAnimal(animal, entry.Position))
                {
                    // Carry over feeding already done on the farm today, exactly as a
                    // manual placement would.
                    if (!animal.NeedsFeeding)
                        TeamAssemblerData.Instance.MarkAsFed(animal);
                    result.placed++;
                }
            }
            else
            {
                result.missing++;
                result.missingNames.Add(entry.animalId);
            }
        }

        return result;
    }

    // ── Persistence ───────────────────────────────────────────────────────────
    // Stored as one string per profile ("name|id:x,y|id:x,y|...|mode:N") in worldStrings,
    // which the save format already supports, rather than adding a new typed collection.

    public void SaveData(GameData gameData)
    {
        if (gameData?.worldData == null) return;

        gameData.worldData.worldCounters[SaveKeyCount] = profiles.Count;

        for (int i = 0; i < profiles.Count; i++)
            gameData.worldData.worldStrings[SaveKeyPrefix + i] = Serialize(profiles[i]);
    }

    public void LoadData(GameData gameData)
    {
        if (gameData?.worldData == null) return;

        // A save with no profile data must not wipe profiles made this session.
        // SaveManager.RegisterSaveable replays LoadData onto anything that registers after
        // the initial load, so clearing unconditionally deleted a profile the player had
        // just saved — measured: saved 1, read back 0.
        if (!gameData.worldData.worldCounters.ContainsKey(SaveKeyCount)) return;

        profiles.Clear();
        int count = gameData.worldData.worldCounters[SaveKeyCount];

        for (int i = 0; i < count; i++)
        {
            string key = SaveKeyPrefix + i;
            if (!gameData.worldData.worldStrings.ContainsKey(key)) continue;

            var profile = Deserialize(gameData.worldData.worldStrings[key]);
            if (profile != null) profiles.Add(profile);
        }
    }

    private static string Serialize(TeamProfile profile)
    {
        var parts = new List<string> { profile.profileName.Replace('|', ' ') };
        foreach (var e in profile.entries)
            parts.Add($"{e.animalId}:{e.x},{e.y}");
        parts.Add($"mode:{(int)profile.combatMode}");
        return string.Join("|", parts);
    }

    private static TeamProfile Deserialize(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;

        string[] parts = raw.Split('|');
        if (parts.Length == 0) return null;

        var profile = new TeamProfile { profileName = parts[0] };

        for (int i = 1; i < parts.Length; i++)
        {
            string part = parts[i];

            if (part.StartsWith("mode:"))
            {
                if (int.TryParse(part.Substring(5), out int mode))
                    profile.combatMode = (CombatMode)mode;
                continue;
            }

            int colon = part.LastIndexOf(':');
            if (colon <= 0) continue;

            string id = part.Substring(0, colon);
            string[] coords = part.Substring(colon + 1).Split(',');
            if (coords.Length != 2) continue;

            if (int.TryParse(coords[0], out int x) && int.TryParse(coords[1], out int y))
                profile.entries.Add(new TeamProfile.Entry { animalId = id, x = x, y = y });
        }

        return profile.entries.Count > 0 ? profile : null;
    }
}

} // namespace SowurShield.Combat
