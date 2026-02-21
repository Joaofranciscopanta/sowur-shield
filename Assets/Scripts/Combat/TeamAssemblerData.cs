using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Stores the team composition assembled by the player.
/// Persists between farm scene and combat scene.
/// </summary>
[System.Serializable]
public class TeamAssemblerData
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

    // Singleton instance (persists between scenes)
    private static TeamAssemblerData instance;
    public static TeamAssemblerData Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new TeamAssemblerData();
            }
            return instance;
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
