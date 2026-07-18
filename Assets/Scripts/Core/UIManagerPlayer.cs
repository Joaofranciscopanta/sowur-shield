using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

namespace SowurShield.Core
{

public class UIManagerPlayer : MonoBehaviour
{
    [Header("Referências UI")]
    public Slider staminaSlider;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI dayText;  // Novo: texto para mostrar o dia

    [Header("Configurações")]
    public bool showAmPm = false;    // Se deve mostrar tempo em formato AM/PM
    public bool showSeasons = true;  // Se deve mostrar estações

    [Header("Localization")]
    [SerializeField] private LocalizedString moneyLabelLocalized; // table "UI_Common", key "ui_common.money_label"
    [SerializeField] private LocalizedString timeLabelLocalized; // table "UI_Common", key "ui_common.time_label"
    [SerializeField] private LocalizedString seasonDayLabelLocalized; // table "UI_Common", key "ui_common.season_day_label"
    [SerializeField] private LocalizedString dayLabelLocalized; // table "UI_Common", key "ui_common.day_label"

    // Referência para o sistema de tempo
    private GameTimeController timeController;
    
    // Referência para os stats do jogador
    private PlayerStats playerStats;

    private void Start()
    {
        // Encontra o controlador de tempo
        timeController = GameTimeController.instance;
        if (timeController == null)
            timeController = FindFirstObjectByType<GameTimeController>();

        if (timeController != null)
        {
            // Register for time events (fired every 15 in-game minutes)
            timeController.OnTimeChanged += HandleTimeChanged;
            timeController.OnDayChanged += HandleDayChanged;

            // Initialize immediately
            UpdateTimeDisplay();
            UpdateDayDisplay();
        }

        // Conecta com PlayerStats
        ConnectToPlayerStats();
        
        // Inicializa a UI com dados reais
        UpdateAllUI();

        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void HandleLanguageChanged(Locale locale)
    {
        UpdateAllUI();
        UpdateTimeDisplay();
        UpdateDayDisplay();
    }

    // Handlers específicos para os eventos
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
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;

        // Remove os callbacks ao destruir para evitar memory leaks
        if (timeController != null)
        {
            timeController.OnTimeChanged -= HandleTimeChanged;
            timeController.OnDayChanged -= HandleDayChanged;
        }
        
        // Remove callbacks do PlayerStats
        if (playerStats != null)
        {
            playerStats.OnEnergyChanged -= UpdateStaminaUI;
            playerStats.OnMoneyChanged -= UpdateMoneyUI;
        }
    }

    private void ConnectToPlayerStats()
    {
        // Encontra PlayerStats na cena
        playerStats = FindFirstObjectByType<PlayerStats>();
        
        if (playerStats != null)
        {
            // Subscribe to stat change events
            playerStats.OnEnergyChanged += UpdateStaminaUI;
            playerStats.OnMoneyChanged += UpdateMoneyUI;

            // Initialize with current values
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
        {
            moneyLabelLocalized.Arguments = new object[] { money };
            moneyText.text = moneyLabelLocalized.SafeGetLocalizedString();
        }
    }
    
    // Método público para compatibilidade (caso outros scripts chamem)
    public void UpdateStaminaUI(int stamina)
    {
        if (staminaSlider != null)
            staminaSlider.value = stamina;
    }
    
    // Atualiza toda a UI
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

    // Método que usa o tempo do jogo
    public void UpdateTimeDisplay()
    {
        if (timeController == null || timeText == null)
            return;

        // Usa o método GetUITimeAsString para obter o horário para UI (arredondado para 15 minutos)
        string timeString;

        // Converte o progresso do dia para horas e minutos
        int hour, minute;
        // Pega o tempo arredondado para UI
        timeController.ProgressToTime(timeController.lastUIUpdateProgress, out hour, out minute);

        // Formata o horário conforme configuração (12h ou 24h)
        if (showAmPm)
        {
            // Formato 12h com AM/PM
            string amPm = hour >= 12 ? "PM" : "AM";
            int displayHours = hour % 12;
            if (displayHours == 0) displayHours = 12; // 12 AM/PM em vez de 0

            timeString = $"{displayHours}:{minute:D2} {amPm}";
        }
        else
        {
            // Formato 24h
            timeString = $"{hour:D2}:{minute:D2}";
        }

        // Atualiza o texto de hora
        timeLabelLocalized.Arguments = new object[] { timeString };
        timeText.text = timeLabelLocalized.SafeGetLocalizedString();
    }

    // Método para atualizar o display do dia
    public void UpdateDayDisplay()
    {
        if (timeController == null || dayText == null)
            return;

        int currentDay = timeController.currentDay;

        string seasonName = GetCurrentSeason(currentDay);
        int dayOfSeason = GetDayOfSeason(currentDay);

        if (showSeasons && !string.IsNullOrEmpty(seasonName))
        {
            seasonDayLabelLocalized.Arguments = new object[] { seasonName, dayOfSeason };
            dayText.text = seasonDayLabelLocalized.SafeGetLocalizedString();
        }
        else
        {
            dayLabelLocalized.Arguments = new object[] { currentDay };
            dayText.text = dayLabelLocalized.SafeGetLocalizedString();
        }

    }

    // Métodos auxiliares para lidar com estações (opcional)
    private string GetCurrentSeason(int day)
    {
        // Exemplo: cada estação tem 28 dias
        int daysPerSeason = 28;
        int seasonIndex = ((day - 1) / daysPerSeason) % 4;

        string key = seasonIndex switch
        {
            0 => "ui_common.season.spring",
            1 => "ui_common.season.summer",
            2 => "ui_common.season.fall",
            3 => "ui_common.season.winter",
            _ => null
        };
        if (key == null) return "";

        return new LocalizedString("UI_Common", key).SafeGetLocalizedString();
    }

    private int GetDayOfSeason(int totalDay)
    {
        // Retorna o dia dentro da estação atual (1-28)
        int daysPerSeason = 28;
        return ((totalDay - 1) % daysPerSeason) + 1;
    }

    // Método para forçar a atualização da UI
    public void ForceUpdateUI()
    {
        if (timeController == null)
            timeController = FindFirstObjectByType<GameTimeController>();

        if (timeController != null)
        {
            timeController.ForceUIUpdate();  // Força uma atualização do tempo na UI
            UpdateDayDisplay();
        }
    }

}

} // namespace SowurShield.Core
