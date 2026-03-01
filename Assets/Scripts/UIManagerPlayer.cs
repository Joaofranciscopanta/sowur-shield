using UnityEngine;
using UnityEngine.UI;
using TMPro;

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


            // Registra para eventos com handlers específicos
            timeController.OnTimeChanged += HandleTimeChanged;  // Este evento agora é disparado a cada 15 minutos
            timeController.OnDayChanged += HandleDayChanged;

            // Inicializa imediatamente
            UpdateTimeDisplay();
            UpdateDayDisplay();
        }
        else
        {

        }

        // Conecta com PlayerStats
        ConnectToPlayerStats();
        
        // Inicializa a UI com dados reais
        UpdateAllUI();
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

            
            // Registra para eventos de mudança nos stats
            playerStats.OnEnergyChanged += UpdateStaminaUI;
            playerStats.OnMoneyChanged += UpdateMoneyUI;
            
            // Atualiza UI imediatamente com valores atuais
            UpdateStaminaUI(playerStats.currentEnergy, playerStats.maxEnergy);
            UpdateMoneyUI(playerStats.money);
        }
        else
        {

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
        timeText.text = $"Time: {timeString}";
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
            dayText.text = $"{seasonName} {dayOfSeason}";
        }
        else
        {
            dayText.text = $"Day {currentDay}";
        }

    }

    // Métodos auxiliares para lidar com estações (opcional)
    private string GetCurrentSeason(int day)
    {
        // Exemplo: cada estação tem 28 dias
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

    private void Update()
    {

    }
}

} // namespace SowurShield.Core
