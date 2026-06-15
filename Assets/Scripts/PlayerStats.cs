using UnityEngine;
using System.Collections.Generic;

namespace SowurShield.Core
{
public class PlayerStats : MonoBehaviour, ISaveable
{
    [Header("Health & Energy")]
    public int maxHealth = 100;
    public int maxEnergy = 100;

    [Header("Current Values")]
    public int currentHealth = 100;
    public int currentEnergy = 100;

    [Header("Currency & Progression")]
    public int money = 100;
    public int playerLevel = 1;
    public float experience = 0f;
    public float experienceToNextLevel = 100f;

    [Header("UI References")]
    public UnityEngine.UI.Slider healthSlider;
    public UnityEngine.UI.Slider energySlider;
    public UnityEngine.UI.Text moneyText; // Optional: direct money text reference

    // Events
    public System.Action<int, int> OnHealthChanged; // current, max
    public System.Action<int, int> OnEnergyChanged; // current, max
    public System.Action<int> OnMoneyChanged;
    public System.Action<int> OnLevelChanged;
    public System.Action<float, float> OnExperienceChanged; // current, needed for next level

    // Properties for save system compatibility
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public int Money => money;
    public int PlayerLevel => playerLevel;
    public float Experience => experience;
    public float ExperienceToNextLevel => experienceToNextLevel;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;

        // Register with SaveManager early in Awake to ensure we get LoadData calls
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }
        else
        {
            // Try again in Start if SaveManager isn't ready yet
            StartCoroutine(DelayedRegistration());
        }
    }

    private void Start()
    {
        var teamData = SowurShield.Combat.TeamAssemblerData.Instance;
        if (teamData.pendingGoldReward != 0 || teamData.pendingXpReward != 0f)
        {
            AddMoney(teamData.pendingGoldReward);
            AddExperience(teamData.pendingXpReward);
            teamData.pendingGoldReward = 0;
            teamData.pendingXpReward = 0f;
        }

        UpdateUI();
    }

    private System.Collections.IEnumerator DelayedRegistration()
    {
        yield return null; // Wait one frame

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }
        else
        {
        }
    }

    public void RestoreHealth(int amount)
    {
        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (currentHealth != oldHealth)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            UpdateUI();
        }
    }

    public void RestoreEnergy(int amount)
    {
        int oldEnergy = currentEnergy;
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);

        if (currentEnergy != oldEnergy)
        {
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            UpdateUI();
        }
    }

    public void UseEnergy(int amount)
    {
        int oldEnergy = currentEnergy;
        currentEnergy = Mathf.Max(0, currentEnergy - amount);

        if (currentEnergy != oldEnergy)
        {
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            UpdateUI();
        }
    }

    public bool HasEnergy(int amount)
    {
        return currentEnergy >= amount;
    }

    // ============================================================================
    // MONEY MANAGEMENT
    // ============================================================================

    public void AddMoney(int amount)
    {
        money += amount;
        OnMoneyChanged?.Invoke(money);
        UpdateMoneyUI();
    }

    public bool SpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            OnMoneyChanged?.Invoke(money);
            UpdateMoneyUI();
            return true;
        }
        return false;
    }

    public bool HasMoney(int amount)
    {
        return money >= amount;
    }

    // ============================================================================
    // EXPERIENCE & LEVELING
    // ============================================================================

    public void AddExperience(float amount)
    {
        experience += amount;

        // Check for level up
        while (experience >= experienceToNextLevel)
        {
            LevelUp();
        }

        OnExperienceChanged?.Invoke(experience, experienceToNextLevel);
    }

    private void LevelUp()
    {
        experience -= experienceToNextLevel;
        playerLevel++;

        // Increase stats on level up
        maxHealth += 10;
        maxEnergy += 5;
        currentHealth = maxHealth; // Full heal on level up
        currentEnergy = maxEnergy; // Full energy on level up

        // Calculate next level requirement (increases by 20% each level)
        experienceToNextLevel = Mathf.Floor(experienceToNextLevel * 1.2f);

        OnLevelChanged?.Invoke(playerLevel);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

        UpdateUI();
    }

    // ============================================================================
    // SAVE SYSTEM METHODS
    // ============================================================================

    public void SetHealth(float health)
    {
        int oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(Mathf.RoundToInt(health), 0, maxHealth);

        if (currentHealth != oldHealth)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            UpdateUI();
        }
    }

    public void SetMaxHealth(float maxHp)
    {
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHp));
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateUI();
    }

    public void SetEnergy(float energy)
    {
        int oldEnergy = currentEnergy;
        currentEnergy = Mathf.Clamp(Mathf.RoundToInt(energy), 0, maxEnergy);

        if (currentEnergy != oldEnergy)
        {
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            UpdateUI();
        }
    }

    public void SetMaxEnergy(float maxEn)
    {
        maxEnergy = Mathf.Max(1, Mathf.RoundToInt(maxEn));
        currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        UpdateUI();
    }

    public void SetMoney(int newMoney)
    {
        money = Mathf.Max(0, newMoney);
        OnMoneyChanged?.Invoke(money);
        UpdateMoneyUI();
    }

    public void SetPlayerLevel(int level)
    {
        playerLevel = Mathf.Max(1, level);
        OnLevelChanged?.Invoke(playerLevel);
    }

    public void SetExperience(float exp)
    {
        experience = Mathf.Max(0, exp);
        OnExperienceChanged?.Invoke(experience, experienceToNextLevel);
    }

    public void SetExperienceToNextLevel(float expNeeded)
    {
        experienceToNextLevel = Mathf.Max(1, expNeeded);
        OnExperienceChanged?.Invoke(experience, experienceToNextLevel);
    }

    private void UpdateUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (energySlider != null)
        {
            energySlider.maxValue = maxEnergy;
            energySlider.value = currentEnergy;
        }

        UpdateMoneyUI();
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"Money: ${money}";
        }
    }

    // ============================================================================
    // ISAVEABLE IMPLEMENTATION
    // ============================================================================

    public void SaveData(GameData gameData)
    {
        gameData.playerData.health = (float)currentHealth;
        gameData.playerData.energy = (float)currentEnergy;
        gameData.playerData.money = money;
        gameData.playerData.playerLevel = playerLevel;
        gameData.playerData.experience = experience;
        gameData.playerData.experienceToNextLevel = experienceToNextLevel;
        gameData.playerData.maxHealth = (float)maxHealth;
        gameData.playerData.maxEnergy = (float)maxEnergy;

    }

    public void LoadData(GameData gameData)
    {
        currentHealth = Mathf.RoundToInt(gameData.playerData.health);
        currentEnergy = Mathf.RoundToInt(gameData.playerData.energy);
        money = gameData.playerData.money;
        playerLevel = gameData.playerData.playerLevel;
        experience = gameData.playerData.experience;
        experienceToNextLevel = gameData.playerData.experienceToNextLevel;
        maxHealth = Mathf.RoundToInt(gameData.playerData.maxHealth);
        maxEnergy = Mathf.RoundToInt(gameData.playerData.maxEnergy);

        // Trigger events and update UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        OnMoneyChanged?.Invoke(money);
        OnLevelChanged?.Invoke(playerLevel);
        OnExperienceChanged?.Invoke(experience, experienceToNextLevel);

        UpdateUI();

    }
}
} // namespace SowurShield.Core
