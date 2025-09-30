using UnityEngine;
using System;
using UnityEngine.UI;

public class GameTimeController : MonoBehaviour, ISaveable
{
    [Header("Configurações de Tempo")]
    public int startingDay = 1;
    [Tooltip("Hora do dia inicial (0-1, onde 0.25 = 6:00)")]
    public float defaultWakeUpTime = 0.25f; // 6:00
    [Tooltip("Hora do dia em que anoitece (0-1)")]
    public float sunsetTime = 0.75f; // 18:00
    [Tooltip("Hora do dia em que amanhece (0-1)")]
    public float sunriseTime = 0.25f; // 6:00
    [Tooltip("Hora do dia em que o jogador deve dormir (0-1)")]
    public float bedTime = 0.958333f; // 23:00 (23/24 = 0.958333)

    [Header("Configurações de Fluxo de Tempo")]
    public bool timeFlowEnabled = true;  // Se o tempo deve fluir automaticamente
    [Tooltip("Quantos minutos do jogo passam em um segundo real")]
    public float minutesPerRealSecond = 10f;  // 10 minutos do jogo a cada 1 segundo real
    [Tooltip("Intervalo (em minutos do jogo) para atualizar a UI")]
    public int uiUpdateInterval = 15;  // Atualiza a UI a cada 15 minutos do jogo

    // Variáveis de tempo
    private int _currentDay;
    private float _dayProgress = 0.25f; // Começa às 6:00 (defaultWakeUpTime)
    private bool _isPaused = false;
    public float _lastUIUpdateProgress;  // Último valor de progresso que disparou atualização de UI
    public float _minutesPerDay = 24f * 60f;  // Total de minutos em um dia

    // Propriedades públicas para acesso externo
    public int currentDay { get { return _currentDay; } }
    public float dayProgress { get { return _dayProgress; } }
    public float lastUIUpdateProgress { get { return _lastUIUpdateProgress; } } // Tornada pública para acesso do UIManager
    public bool isPaused {
        get { return _isPaused; }
        set {
            _isPaused = value;
        }
    }

    // Eventos
    public event Action OnDayChanged;
    public event Action OnTimeChanged;  // Disparado a cada 15 minutos
    public event Action OnRealTimeChanged;  // Disparado a cada mudança real de tempo
    public event Action OnBedTime;  // Evento para quando chegar na hora de dormir

    // Singleton
    public static GameTimeController instance;

    private void Awake()
    {
        // Setup do singleton
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Inicialização
        _currentDay = startingDay;
        _dayProgress = defaultWakeUpTime; // Começa no horário de acordar (6:00)
        _lastUIUpdateProgress = _dayProgress;

        // Register with SaveManager (before SaveManager.Start() runs)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }
    }

    private void Start()
    {

        // Disparar eventos iniciais para garantir que todos os ouvintes recebam a notificação inicial

        // Verificar quantos listeners existem
        int timeListeners = OnTimeChanged?.GetInvocationList()?.Length ?? 0;
        int dayListeners = OnDayChanged?.GetInvocationList()?.Length ?? 0;

        if (OnTimeChanged != null)
            OnTimeChanged.Invoke();

        if (OnDayChanged != null)
            OnDayChanged.Invoke();
    }

    private void Update()
    {
        // Avança o tempo se estiver habilitado e não pausado
        if (timeFlowEnabled && !_isPaused)
        {
            // Calcula quanto do dia deve passar neste frame
            float timeToAdd = (minutesPerRealSecond / _minutesPerDay) * Time.deltaTime;

            // Verifica se vai ultrapassar a hora de dormir
            if (_dayProgress < bedTime && _dayProgress + timeToAdd >= bedTime)
            {
                // Atualiza para exatamente a hora de dormir
                UpdateTimeProgress(bedTime);

                // Notifica que chegou a hora de dormir
                if (OnBedTime != null)
                {
                    OnBedTime.Invoke();
                }
            }
            else if (_dayProgress < bedTime)
            {
                // Atualiza o progresso do dia normalmente
                UpdateTimeProgress(_dayProgress + timeToAdd);
            }
        }
    }

    // Método central para atualizar o progresso do tempo
    private void UpdateTimeProgress(float newProgress)
    {
        // Verifica se o valor é diferente para evitar notificações desnecessárias
        if (Mathf.Approximately(_dayProgress, newProgress))
            return;

        float oldProgress = _dayProgress;

        // Verifica se vai passar para o próximo dia
        bool dayChange = false;

        // Se passar para o próximo dia
        if (newProgress >= 1f)
        {
            int daysToAdd = Mathf.FloorToInt(newProgress);
            newProgress -= daysToAdd;
            _currentDay += daysToAdd;
            dayChange = true;
        }
        // Se retroceder para o dia anterior
        else if (newProgress < 0f)
        {
            int daysToSubtract = Mathf.FloorToInt(-newProgress) + 1;
            newProgress += daysToSubtract;
            _currentDay -= daysToSubtract;
            dayChange = true;
        }

        // Atualiza o momento do dia
        _dayProgress = newProgress;

        // Dispara evento de mudança real de tempo (sempre)
        if (OnRealTimeChanged != null)
            OnRealTimeChanged.Invoke();

        // Verifica se deve atualizar a UI (a cada 15 minutos)
        CheckUIUpdate(oldProgress);

        // Notifica mudança de dia se necessário
        if (dayChange && OnDayChanged != null)
        {
            OnDayChanged.Invoke();

            // Reseta o lastUIUpdateProgress para o início do dia
            _lastUIUpdateProgress = _dayProgress;

            // Garantir que a UI seja atualizada no início de um novo dia
            if (OnTimeChanged != null)
                OnTimeChanged.Invoke();
        }
    }

    // Verifica se é hora de atualizar a UI
    private void CheckUIUpdate(float oldProgress)
    {
        // Converte o progresso para minutos
        int currentMinutes = Mathf.FloorToInt(_dayProgress * _minutesPerDay);
        int lastUpdateMinutes = Mathf.FloorToInt(_lastUIUpdateProgress * _minutesPerDay);

        // Se cruzou um limite de 15 minutos ou se passou para o dia seguinte
        if (Mathf.FloorToInt(currentMinutes / uiUpdateInterval) != Mathf.FloorToInt(lastUpdateMinutes / uiUpdateInterval))
        {
            // Arredonda para o intervalo de 15 minutos
            int roundedMinutes = Mathf.FloorToInt(currentMinutes / uiUpdateInterval) * uiUpdateInterval;

            // Atualiza o último momento em que a UI foi atualizada
            _lastUIUpdateProgress = roundedMinutes / _minutesPerDay;

            // Dispara evento de atualização da UI
            if (OnTimeChanged != null)
            {
                OnTimeChanged.Invoke();
            }
        }
    }

    // Método chamado ao dormir - avança o dia
    public void AdvanceDay(int days = 1)
    {
        // Atualiza o dia
        int oldDay = _currentDay;
        _currentDay += days;

        // Define o momento do dia para horário de acordar
        _dayProgress = defaultWakeUpTime;
        _lastUIUpdateProgress = _dayProgress; // Reseta o último momento de atualização da UI

        // Dispara eventos com verificação de nulo
        if (OnRealTimeChanged != null)
            OnRealTimeChanged.Invoke();

        if (OnTimeChanged != null)
            OnTimeChanged.Invoke();

        if (oldDay != _currentDay && OnDayChanged != null)
            OnDayChanged.Invoke();
    }


    // Para definir um horário específico
    public void SetTimeOfDay(float progress)
    {
        float oldProgress = _dayProgress;
        _dayProgress = Mathf.Clamp01(progress);
        _lastUIUpdateProgress = GetRoundedUITime(_dayProgress);

        // Dispara eventos
        if (OnRealTimeChanged != null)
            OnRealTimeChanged.Invoke();

        if (OnTimeChanged != null)
            OnTimeChanged.Invoke();
    }

    // Obtém o valor de progresso arredondado para o intervalo de UI
    private float GetRoundedUITime(float progress)
    {
        int totalMinutes = Mathf.FloorToInt(progress * _minutesPerDay);
        int roundedMinutes = Mathf.FloorToInt(totalMinutes / uiUpdateInterval) * uiUpdateInterval;
        return roundedMinutes / _minutesPerDay;
    }

    // Adiciona minutos ao tempo atual
    public void AddMinutes(int minutes)
    {
        float timeToAdd = minutes / _minutesPerDay;
        UpdateTimeProgress(_dayProgress + timeToAdd);
    }

    // Adiciona horas ao tempo atual
    public void AddHours(int hours)
    {
        float timeToAdd = hours / 24f;
        UpdateTimeProgress(_dayProgress + timeToAdd);
    }

    // Converte hora e minutos para progresso do dia (0-1)
    public float TimeToProgress(int hour, int minute)
    {
        return (hour * 60 + minute) / _minutesPerDay;
    }

    // Converte progresso do dia para hora e minutos
    public void ProgressToTime(float progress, out int hour, out int minute)
    {
        int totalMinutes = Mathf.FloorToInt(progress * _minutesPerDay);
        hour = totalMinutes / 60;
        minute = totalMinutes % 60;
    }

    public string GetTimeAsString(bool useUITime = false)
    {
        // Usa o progresso real ou o progresso para UI
        float progressToUse = useUITime ? _lastUIUpdateProgress : _dayProgress;

        int hour, minute;
        ProgressToTime(progressToUse, out hour, out minute);
        return $"{hour:D2}:{minute:D2}";
    }

    public string GetDayAsString()
    {
        return $"Dia {_currentDay}";
    }

    // Retorna o tempo atual arredondado para o intervalo da UI
    public string GetUITimeAsString()
    {
        return GetTimeAsString(true);
    }

    public bool IsTimeBetween(float startTime, float endTime)
    {
        if (startTime <= endTime)
            return _dayProgress >= startTime && _dayProgress <= endTime;
        else
            return _dayProgress >= startTime || _dayProgress <= endTime;
    }

    public bool IsMorning() => IsTimeBetween(sunriseTime, 0.5f);
    public bool IsAfternoon() => IsTimeBetween(0.5f, sunsetTime);
    public bool IsEvening() => IsTimeBetween(sunsetTime, 0.9f);
    public bool IsNight() => IsTimeBetween(0.9f, sunriseTime);

    // Método para ajuste manual do tempo (debug)
    public void DebugSetTime(float time)
    {
        SetTimeOfDay(Mathf.Clamp01(time));
    }

    // Método para definir um horário específico por hora e minuto
    public void SetTime(int hour, int minute)
    {
        float progress = TimeToProgress(hour, minute);
        SetTimeOfDay(progress);
    }

    // Método adicional para ajustar o tempo (usado apenas para debug/testes)
    public void DebugAdjustTime(float hoursToAdd)
    {
        float timeToAdd = hoursToAdd / 24f;
        UpdateTimeProgress(_dayProgress + timeToAdd);
    }

    // Método para pausar/retomar o fluxo de tempo
    public void ToggleTimeFlow()
    {
        isPaused = !isPaused;
    }

    // Método para habilitar/desabilitar completamente o fluxo de tempo
    public void EnableTimeFlow(bool enable)
    {
        timeFlowEnabled = enable;
    }

    // Força uma atualização da UI imediatamente
    public void ForceUIUpdate()
    {
        _lastUIUpdateProgress = GetRoundedUITime(_dayProgress);

        if (OnTimeChanged != null)
        {
            OnTimeChanged.Invoke();
        }
    }

    // ============================================================================
    // ISAVEABLE IMPLEMENTATION
    // ============================================================================

    public void SaveData(GameData gameData)
    {
        gameData.timeData.currentDay = _currentDay;
        gameData.timeData.dayProgress = _dayProgress;
        gameData.timeData.timeFlowEnabled = timeFlowEnabled;
        gameData.timeData.minutesPerRealSecond = minutesPerRealSecond;

        // For now, keep default season and year - can be expanded later
        gameData.timeData.season = "Spring";
        gameData.timeData.year = 1;
    }

    public void LoadData(GameData gameData)
    {
        _currentDay = gameData.timeData.currentDay;
        _dayProgress = gameData.timeData.dayProgress;
        timeFlowEnabled = gameData.timeData.timeFlowEnabled;
        minutesPerRealSecond = gameData.timeData.minutesPerRealSecond;

        // Update UI progress tracking
        _lastUIUpdateProgress = GetRoundedUITime(_dayProgress);

        // Trigger events to update UI
        if (OnRealTimeChanged != null)
            OnRealTimeChanged.Invoke();

        if (OnTimeChanged != null)
            OnTimeChanged.Invoke();

        if (OnDayChanged != null)
            OnDayChanged.Invoke();
    }

    /// <summary>
    /// Reset day progress to wake-up time (start of day) - used by Continue button
    /// </summary>
    public void ResetToStartOfDay()
    {
        _dayProgress = defaultWakeUpTime; // Reset to 6:00 AM
        _lastUIUpdateProgress = GetRoundedUITime(_dayProgress);

        // Trigger events to update UI
        if (OnRealTimeChanged != null)
            OnRealTimeChanged.Invoke();

        if (OnTimeChanged != null)
            OnTimeChanged.Invoke();
    }

    /// <summary>
    /// Complete reset for new games - resets day, time, and all settings to initial state
    /// </summary>
    public void ResetForNewGame()
    {
        _currentDay = startingDay; // Reset to Day 1
        _dayProgress = defaultWakeUpTime; // Reset to 6:00 AM
        _lastUIUpdateProgress = GetRoundedUITime(_dayProgress);
        _isPaused = false;
        timeFlowEnabled = true;

        // Trigger events to update UI
        if (OnRealTimeChanged != null)
            OnRealTimeChanged.Invoke();

        if (OnTimeChanged != null)
            OnTimeChanged.Invoke();

        if (OnDayChanged != null)
            OnDayChanged.Invoke();
    }

    private void OnDestroy()
    {
        // Unregister from SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
        }
    }

}
