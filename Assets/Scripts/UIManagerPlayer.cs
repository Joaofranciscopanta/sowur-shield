using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManagerPlayer : MonoBehaviour
{
    [Header("UI References")]
    public Slider staminaSlider;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI dayText;

    [Header("Configuration")]
    public bool showAmPm = false;
    public bool showSeasons = true;

    private GameTimeController timeController;
    private PlayerStats playerStats;

    private void Start()
    {
        timeController = GameTimeController.instance;
        if (timeController == null)
            timeController = FindFirstObjectByType<GameTimeController>();

        if (timeController != null)
        {
            timeController.OnTimeChanged += HandleTimeChanged;
            timeController.OnDayChanged += HandleDayChanged;

            UpdateTimeDisplay();
            UpdateDayDisplay();
        }

        ConnectToPlayerStats();
        UpdateAllUI();
    }

    private void HandleTimeChanged()
    {
        UpdateTimeDisplay();
    }

    private void HandleDayChanged()
    {
        UpdateDayDisplay();
    }

    private void OnDestroy()
    {
        if (timeController != null)
        {
            timeController.OnTimeChanged -= HandleTimeChanged;
            timeController.OnDayChanged -= HandleDayChanged;
        }

        if (playerStats != null)
        {
            playerStats.OnEnergyChanged -= UpdateStaminaUI;
            playerStats.OnMoneyChanged -= UpdateMoneyUI;
        }
    }

    private void ConnectToPlayerStats()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.OnEnergyChanged += UpdateStaminaUI;
            playerStats.OnMoneyChanged += UpdateMoneyUI;

            UpdateStaminaUI(playerStats.currentEnergy, playerStats.maxEnergy);
            UpdateMoneyUI(playerStats.money);
        }
    }
    
    private void UpdateStaminaUI(int currentEnergy, int maxEnergy)
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxEnergy;
            staminaSlider.value = currentEnergy;
        }
    }

    private void UpdateMoneyUI(int money)
    {
        if (moneyText != null)
            moneyText.text = $"Money: ${money}";
    }

    public void UpdateStaminaUI(int stamina)
    {
        if (staminaSlider != null)
            staminaSlider.value = stamina;
    }

    private void UpdateAllUI()
    {
        if (playerStats != null)
        {
            UpdateStaminaUI(playerStats.currentEnergy, playerStats.maxEnergy);
            UpdateMoneyUI(playerStats.money);
        }
        UpdateTimeDisplay();
        UpdateDayDisplay();
    }

    public void UpdateTimeDisplay()
    {
        if (timeController == null || timeText == null)
            return;

        string timeString;
        int hour, minute;
        timeController.ProgressToTime(timeController.lastUIUpdateProgress, out hour, out minute);

        if (showAmPm)
        {
            string amPm = hour >= 12 ? "PM" : "AM";
            int displayHours = hour % 12;
            if (displayHours == 0) displayHours = 12;

            timeString = $"{displayHours}:{minute:D2} {amPm}";
        }
        else
        {
            timeString = $"{hour:D2}:{minute:D2}";
        }

        timeText.text = $"Time: {timeString}";
    }

    public void UpdateDayDisplay()
    {
        if (timeController == null || dayText == null)
            return;

        int currentDay = timeController.currentDay;
        string seasonName = GetCurrentSeason(currentDay);
        int dayOfSeason = GetDayOfSeason(currentDay);

        if (showSeasons && !string.IsNullOrEmpty(seasonName))
        {
            dayText.text = $"{seasonName} {dayOfSeason}";
        }
        else
        {
            dayText.text = $"Day {currentDay}";
        }
    }

    private string GetCurrentSeason(int day)
    {
        int daysPerSeason = 28;
        int seasonIndex = ((day - 1) / daysPerSeason) % 4;

        switch (seasonIndex)
        {
            case 0: return "Spring";
            case 1: return "Summer";
            case 2: return "Fall";
            case 3: return "Winter";
            default: return "";
        }
    }

    private int GetDayOfSeason(int totalDay)
    {
        int daysPerSeason = 28;
        return ((totalDay - 1) % daysPerSeason) + 1;
    }

    public void ForceUpdateUI()
    {
        if (timeController == null)
            timeController = FindFirstObjectByType<GameTimeController>();

        if (timeController != null)
        {
            timeController.ForceUIUpdate();
            UpdateDayDisplay();
        }
    }
}
