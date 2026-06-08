using UnityEngine;
using System.Collections.Generic;
using SowurShield.Animals;

namespace SowurShield.Combat
{

/// <summary>
/// Stores the team composition assembled by the player.
/// Persists between farm scene and combat scene via DontDestroyOnLoad.
///
/// This is a MonoBehaviour singleton so Unity pins it in memory across
/// scene loads — plain C# static fields can be reset by domain reload.
/// </summary>
public class TeamAssemblerData : MonoBehaviour
{
    [System.Serializable]
    public class PositionedAnimal
    {
        // ScriptableObject reference - persists across scenes!
        public AnimalData animalData;

        // Runtime data extracted from Animal component
        public string customName;
        public float happiness;

        // Combat progression stats
        public float attackGrowth;
        public float defenseGrowth;
        public float speedGrowth;
        public float healthGrowth;
        public int level;
        public float experience;

        // Seasonal bonuses
        public float seasonalAttackMod;
        public float seasonalDefenseMod;
        public float seasonalSpeedMod;

        public Vector2Int gridPosition;
        public bool isFed;

        // Parameterless constructor for PlayerPrefs restore
        public PositionedAnimal() { }

        // Constructor to extract data from Animal component
        public PositionedAnimal(Animal animal, Vector2Int pos)
        {
            if (animal == null)
            {
                return;
            }

            // Extract the persistent data
            this.animalData = animal.AnimalData;
            this.customName = animal.GetDisplayName();
            this.happiness = animal.GetHappiness();
            this.gridPosition = pos;
            this.isFed = false;

            // Extract combat progression stats
            var stats = animal.GetCombatStats();
            if (stats != null)
            {
                this.attackGrowth = stats.attackGrowth;
                this.defenseGrowth = stats.defenseGrowth;
                this.speedGrowth = stats.speedGrowth;
                this.healthGrowth = stats.healthGrowth;
                this.level = stats.level;
                this.experience = stats.experience;

                // Extract seasonal bonuses
                this.seasonalAttackMod = stats.seasonalAttackMod;
                this.seasonalDefenseMod = stats.seasonalDefenseMod;
                this.seasonalSpeedMod = stats.seasonalSpeedMod;
            }
            else
            {
                // Default values if no combat stats
                this.attackGrowth = 1f;
                this.defenseGrowth = 1f;
                this.speedGrowth = 1f;
                this.healthGrowth = 1f;
                this.level = 1;
                this.experience = 0f;
                this.seasonalAttackMod = 1f;
                this.seasonalDefenseMod = 1f;
                this.seasonalSpeedMod = 1f;
            }
        }

        /// <summary>
        /// Get display name (custom name or animal name)
        /// </summary>
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(customName))
            {
                return customName;
            }
            return animalData != null ? animalData.animalName : "Unknown";
        }
    }

    [Header("Team Composition")]
    public List<PositionedAnimal> team = new List<PositionedAnimal>();

    [Header("Combat Zone Info")]
    public string zoneName = "Unknown Zone";
    public int zoneDifficulty = 1;

    /// <summary>
    /// Name of the selected stage — persists across scene loads (static fields don't in builds).
    /// Set by StageButton before loading CombatScene.
    /// </summary>
    public string selectedStageName = "";

    // ── MonoBehaviour singleton with DontDestroyOnLoad ────────────────────────
    private static TeamAssemblerData instance;
    public static TeamAssemblerData Instance
    {
        get
        {
            if (instance == null)
            {
                // FindObjectsByType searches ALL scenes including DontDestroyOnLoad
                var all = FindObjectsByType<TeamAssemblerData>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var t in all)
                {
                    instance = t;
                    break;
                }

                if (instance == null)
                {
                    // Create a persistent root GameObject
                    var go = new GameObject("TeamAssemblerData");
                    instance = go.AddComponent<TeamAssemblerData>();
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
        DontDestroyOnLoad(gameObject);
    }

    // ── PlayerPrefs persistence (survives domain reload in builds) ────────────

    private const string PrefsKeyStage = "Combat_SelectedStage";
    private const string PrefsKeyTeamCount = "Combat_TeamCount";
    private const string PrefsKeyTeamPrefix = "Combat_Team_";

    /// <summary>
    /// Save team and stage to PlayerPrefs before scene load.
    /// </summary>
    public void SaveToPrefs()
    {
        PlayerPrefs.SetString(PrefsKeyStage, selectedStageName);
        PlayerPrefs.SetInt(PrefsKeyTeamCount, team.Count);
        for (int i = 0; i < team.Count; i++)
        {
            var p = team[i];
            PlayerPrefs.SetString(PrefsKeyTeamPrefix + i + "_animal", p.animalData?.animalName ?? "");
            PlayerPrefs.SetString(PrefsKeyTeamPrefix + i + "_name",   p.customName ?? "");
            PlayerPrefs.SetFloat (PrefsKeyTeamPrefix + i + "_happiness", p.happiness);
            PlayerPrefs.SetInt   (PrefsKeyTeamPrefix + i + "_gx",  p.gridPosition.x);
            PlayerPrefs.SetInt   (PrefsKeyTeamPrefix + i + "_gy",  p.gridPosition.y);
            PlayerPrefs.SetFloat (PrefsKeyTeamPrefix + i + "_atkG", p.attackGrowth);
            PlayerPrefs.SetFloat (PrefsKeyTeamPrefix + i + "_defG", p.defenseGrowth);
            PlayerPrefs.SetFloat (PrefsKeyTeamPrefix + i + "_spdG", p.speedGrowth);
            PlayerPrefs.SetFloat (PrefsKeyTeamPrefix + i + "_hpG",  p.healthGrowth);
            PlayerPrefs.SetInt   (PrefsKeyTeamPrefix + i + "_lvl",  p.level);
            PlayerPrefs.SetFloat (PrefsKeyTeamPrefix + i + "_exp",  p.experience);
            PlayerPrefs.SetFloat (PrefsKeyTeamPrefix + i + "_atkS", p.seasonalAttackMod);
            PlayerPrefs.SetFloat (PrefsKeyTeamPrefix + i + "_defS", p.seasonalDefenseMod);
            PlayerPrefs.SetFloat (PrefsKeyTeamPrefix + i + "_spdS", p.seasonalSpeedMod);
        }
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Restore team and stage from PlayerPrefs (called in CombatScene if team is empty).
    /// </summary>
    public void LoadFromPrefs()
    {
        if (!PlayerPrefs.HasKey(PrefsKeyTeamCount)) return;

        selectedStageName = PlayerPrefs.GetString(PrefsKeyStage, "");
        int count = PlayerPrefs.GetInt(PrefsKeyTeamCount, 0);
        team.Clear();

        for (int i = 0; i < count; i++)
        {
            string animalName = PlayerPrefs.GetString(PrefsKeyTeamPrefix + i + "_animal", "");
            AnimalData data = Resources.Load<AnimalData>($"Animals/{animalName}");
            if (data == null)
            {
                Debug.LogWarning($"[TeamAssemblerData] LoadFromPrefs: could not load AnimalData 'Animals/{animalName}'");
                continue;
            }

            var p = new PositionedAnimal();
            p.animalData        = data;
            p.customName        = PlayerPrefs.GetString(PrefsKeyTeamPrefix + i + "_name", "");
            p.happiness         = PlayerPrefs.GetFloat (PrefsKeyTeamPrefix + i + "_happiness", 50f);
            p.gridPosition      = new Vector2Int(PlayerPrefs.GetInt(PrefsKeyTeamPrefix + i + "_gx", 6),
                                                 PlayerPrefs.GetInt(PrefsKeyTeamPrefix + i + "_gy", 2));
            p.attackGrowth      = PlayerPrefs.GetFloat(PrefsKeyTeamPrefix + i + "_atkG", 1f);
            p.defenseGrowth     = PlayerPrefs.GetFloat(PrefsKeyTeamPrefix + i + "_defG", 1f);
            p.speedGrowth       = PlayerPrefs.GetFloat(PrefsKeyTeamPrefix + i + "_spdG", 1f);
            p.healthGrowth      = PlayerPrefs.GetFloat(PrefsKeyTeamPrefix + i + "_hpG",  1f);
            p.level             = PlayerPrefs.GetInt  (PrefsKeyTeamPrefix + i + "_lvl",  1);
            p.experience        = PlayerPrefs.GetFloat(PrefsKeyTeamPrefix + i + "_exp",  0f);
            p.seasonalAttackMod = PlayerPrefs.GetFloat(PrefsKeyTeamPrefix + i + "_atkS", 1f);
            p.seasonalDefenseMod= PlayerPrefs.GetFloat(PrefsKeyTeamPrefix + i + "_defS", 1f);
            p.seasonalSpeedMod  = PlayerPrefs.GetFloat(PrefsKeyTeamPrefix + i + "_spdS", 1f);
            p.isFed             = true;
            team.Add(p);
        }

    }

    /// <summary>
    /// Add an animal to the team at a specific position
    /// </summary>
    public bool AddAnimal(Animal animal, Vector2Int position)
    {
        // Check if position is already occupied
        if (IsPositionOccupied(position))
        {
            return false;
        }

        // Check if animal is already in team
        if (IsAnimalInTeam(animal))
        {
            return false;
        }

        team.Add(new PositionedAnimal(animal, position));
        return true;
    }

    /// <summary>
    /// Remove an animal from the team (by Animal reference)
    /// </summary>
    public bool RemoveAnimal(Animal animal)
    {
        if (animal == null || animal.AnimalData == null) return false;

        PositionedAnimal positioned = team.Find(pa => pa.animalData == animal.AnimalData);
        if (positioned != null)
        {
            team.Remove(positioned);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Remove animal at specific position
    /// </summary>
    public bool RemoveAtPosition(Vector2Int position)
    {
        PositionedAnimal positioned = team.Find(pa => pa.gridPosition == position);
        if (positioned != null)
        {
            team.Remove(positioned);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Move an animal to a new position
    /// </summary>
    public bool MoveAnimal(Animal animal, Vector2Int newPosition)
    {
        if (animal == null || animal.AnimalData == null) return false;

        PositionedAnimal positioned = team.Find(pa => pa.animalData == animal.AnimalData);
        if (positioned != null)
        {
            // Check if new position is occupied by different animal
            PositionedAnimal occupant = team.Find(pa => pa.gridPosition == newPosition);
            if (occupant != null && occupant.animalData != animal.AnimalData)
            {
                // Swap positions
                Vector2Int oldPosition = positioned.gridPosition;
                positioned.gridPosition = newPosition;
                occupant.gridPosition = oldPosition;
            }
            else
            {
                // Just move
                positioned.gridPosition = newPosition;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Mark an animal as fed
    /// </summary>
    public void MarkAsFed(Animal animal)
    {
        if (animal == null || animal.AnimalData == null) return;

        PositionedAnimal positioned = team.Find(pa => pa.animalData == animal.AnimalData);
        if (positioned != null)
        {
            positioned.isFed = true;
        }
    }

    /// <summary>
    /// Check if all animals in team are fed
    /// </summary>
    public bool AreAllAnimalsFed()
    {
        return team.TrueForAll(pa => pa.isFed);
    }

    /// <summary>
    /// Get total food requirements for all unfed animals
    /// </summary>
    public Dictionary<string, int> GetTotalFoodRequirements()
    {
        Dictionary<string, int> requirements = new Dictionary<string, int>();

        foreach (PositionedAnimal pa in team)
        {
            if (!pa.isFed && pa.animalData != null)
            {
                foreach (FoodRequirement foodReq in pa.animalData.dailyFoodRequirements)
                {
                    if (requirements.ContainsKey(foodReq.itemName))
                    {
                        requirements[foodReq.itemName] += foodReq.quantityPerDay;
                    }
                    else
                    {
                        requirements[foodReq.itemName] = foodReq.quantityPerDay;
                    }
                }
            }
        }

        return requirements;
    }

    /// <summary>
    /// Check if position is occupied
    /// </summary>
    public bool IsPositionOccupied(Vector2Int position)
    {
        return team.Exists(pa => pa.gridPosition == position);
    }

    /// <summary>
    /// Check if animal is in team (by AnimalData reference)
    /// </summary>
    public bool IsAnimalInTeam(Animal animal)
    {
        if (animal == null || animal.AnimalData == null) return false;
        return team.Exists(pa => pa.animalData == animal.AnimalData);
    }

    /// <summary>
    /// Get positioned animal data at specific position
    /// </summary>
    public PositionedAnimal GetPositionedAnimalAtPosition(Vector2Int position)
    {
        return team.Find(pa => pa.gridPosition == position);
    }

    /// <summary>
    /// Clear all animals from team
    /// </summary>
    public void ClearTeam()
    {
        team.Clear();
    }

    /// <summary>
    /// Get team size
    /// </summary>
    public int GetTeamSize()
    {
        return team.Count;
    }

    /// <summary>
    /// Validate team (at least 1 animal, all fed)
    /// </summary>
    public bool IsTeamValid()
    {
        return team.Count > 0 && AreAllAnimalsFed();
    }
}

} // namespace SowurShield.Combat
