using UnityEngine;
using System;
using UnityEngine.UI;

public class GameTimeController : MonoBehaviour, ISaveable
{
    [Header("Time Settings")]
    public int startingDay = 1;
    [Tooltip("Initial time of day (0-1, where 0.25 = 6:00)")]
    public float defaultWakeUpTime = 0.25f;
    [Tooltip("Time of day when sunset occurs (0-1)")]
    public float sunsetTime = 0.75f;
    [Tooltip("Time of day when sunrise occurs (0-1)")]
    public float sunriseTime = 0.25f;
    [Tooltip("Time of day when player should sleep (0-1)")]
    public float bedTime = 0.958333f;

    [Header("Time Flow Settings")]
    public bool timeFlowEnabled = true;
    [Tooltip("How many game minutes pass per real second")]
    public float minutesPerRealSecond = 10f;
    [Tooltip("Interval (in game minutes) to update UI")]
    public int uiUpdateInterval = 15;

    private int _currentDay;
    private float _dayProgress = 0.25f;
    private bool _isPaused = false;
    public float _lastUIUpdateProgress;
    public float _minutesPerDay = 24f * 60f;

    public int currentDay { get { return _currentDay; } }
    public float dayProgress { get { return _dayProgress; } }
    public float lastUIUpdateProgress { get { return _lastUIUpdateProgress; } }
    public bool isPaused {
        get { return _isPaused; }
        set {
            _isPaused = value;
        }
    }

    public event Action OnDayChanged;
    public event Action OnTimeChanged;
    public event Action OnRealTimeChanged;
    public event Action OnBedTime;

    // Singleton
    public static GameTimeController instance;

    private void Awake()
    {
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

        _currentDay = startingDay;
        _dayProgress = defaultWakeUpTime;
        _lastUIUpdateProgress = _dayProgress;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }
    }

    private void Start()
    {
        int timeListeners = OnTimeChanged?.GetInvocationList()?.Length ?? 0;
        int dayListeners = OnDayChanged?.GetInvocationList()?.Length ?? 0;

        if (OnTimeChanged != null)
            OnTimeChanged.Invoke();

        if (OnDayChanged != null)
            OnDayChanged.Invoke();
    }

    private void Update()
    {
        if (timeFlowEnabled && !_isPaused)
        {
            float timeToAdd = (minutesPerRealSecond / _minutesPerDay) * Time.deltaTime;

            if (_dayProgress < bedTime && _dayProgress + timeToAdd >= bedTime)
            {
                UpdateTimeProgress(bedTime);

                if (OnBedTime != null)
                {
                    OnBedTime.Invoke();
                }
            }
            else if (_dayProgress < bedTime)
            {
                UpdateTimeProgress(_dayProgress + timeToAdd);
            }
        }
    }

    private void UpdateTimeProgress(float newProgress)
    {
        if (Mathf.Approximately(_dayProgress, newProgress))
            return;

        float oldProgress = _dayProgress;

        bool dayChange = false;

        if (newProgress >= 1f)
        {
            int daysToAdd = Mathf.FloorToInt(newProgress);
            newProgress -= daysToAdd;
            _currentDay += daysToAdd;
            dayChange = true;
        }
        else if (newProgress < 0f)
        {
            int daysToSubtract = Mathf.FloorToInt(-newProgress) + 1;
            newProgress += daysToSubtract;
            _currentDay -= daysToSubtract;
            dayChange = true;
        }

        _dayProgress = newProgress;

        if (OnRealTimeChanged != null)
            OnRealTimeChanged.Invoke();

        CheckUIUpdate(oldProgress);

        if (dayChange && OnDayChanged != null)
        {
            OnDayChanged.Invoke();

            _lastUIUpdateProgress = _dayProgress;

            if (OnTimeChanged != null)
                OnTimeChanged.Invoke();
        }
    }

    private void CheckUIUpdate(float oldProgress)
    {
        int currentMinutes = Mathf.FloorToInt(_dayProgress * _minutesPerDay);
        int lastUpdateMinutes = Mathf.FloorToInt(_lastUIUpdateProgress * _minutesPerDay);

        if (Mathf.FloorToInt(currentMinutes / uiUpdateInterval) != Mathf.FloorToInt(lastUpdateMinutes / uiUpdateInterval))
        {
            int roundedMinutes = Mathf.FloorToInt(currentMinutes / uiUpdateInterval) * uiUpdateInterval;

            _lastUIUpdateProgress = roundedMinutes / _minutesPerDay;

            if (OnTimeChanged != null)
            {
                OnTimeChanged.Invoke();
            }
        }
    }

    public void AdvanceDay(int days = 1)
    {
        int oldDay = _currentDay;
        _currentDay += days;

        _dayProgress = defaultWakeUpTime;
        _lastUIUpdateProgress = _dayProgress;

        if (OnRealTimeChanged != null)
            OnRealTimeChanged.Invoke();

        if (OnTimeChanged != null)
        {
            int listeners = OnTimeChanged.GetInvocationList().Length;
            OnTimeChanged.Invoke();
        }

        if (oldDay != _currentDay && OnDayChanged != null)
        {
            int listeners = OnDayChanged.GetInvocationList().Length;
            OnDayChanged.Invoke();
        }
    }

    public void SetTimeOfDay(float progress)
    {
        float oldProgress = _dayProgress;
        _dayProgress = Mathf.Clamp01(progress);
        _lastUIUpdateProgress = GetRoundedUITime(_dayProgress);

        if (OnRealTimeChanged != null)
            OnRealTimeChanged.Invoke();

        if (OnTimeChanged != null)
            OnTimeChanged.Invoke();
    }

    private float GetRoundedUITime(float progress)
    {
        int totalMinutes = Mathf.FloorToInt(progress * _minutesPerDay);
        int roundedMinutes = Mathf.FloorToInt(totalMinutes / uiUpdateInterval) * uiUpdateInterval;
        return roundedMinutes / _minutesPerDay;
    }

    public void AddMinutes(int minutes)
    {
        float timeToAdd = minutes / _minutesPerDay;
        UpdateTimeProgress(_dayProgress + timeToAdd);
    }

    public void AddHours(int hours)
    {
        float timeToAdd = hours / 24f;
        UpdateTimeProgress(_dayProgress + timeToAdd);
    }

    public float TimeToProgress(int hour, int minute)
    {
        return (hour * 60 + minute) / _minutesPerDay;
    }

    public void ProgressToTime(float progress, out int hour, out int minute)
    {
        int totalMinutes = Mathf.FloorToInt(progress * _minutesPerDay);
        hour = totalMinutes / 60;
        minute = totalMinutes % 60;
    }

    public string GetTimeAsString(bool useUITime = false)
    {
        float progressToUse = useUITime ? _lastUIUpdateProgress : _dayProgress;

        int hour, minute;
        ProgressToTime(progressToUse, out hour, out minute);
        return $"{hour:D2}:{minute:D2}";
    }

    public string GetDayAsString()
    {
        return $"Day {_currentDay}";
    }

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

    public void DebugSetTime(float time)
    {
        SetTimeOfDay(Mathf.Clamp01(time));
    }

    public void SetTime(int hour, int minute)
    {
        float progress = TimeToProgress(hour, minute);
        SetTimeOfDay(progress);
    }

    public void DebugAdjustTime(float hoursToAdd)
    {
        float timeToAdd = hoursToAdd / 24f;
        UpdateTimeProgress(_dayProgress + timeToAdd);
    }

    public void ToggleTimeFlow()
    {
        isPaused = !isPaused;
    }

    public void EnableTimeFlow(bool enable)
    {
        timeFlowEnabled = enable;
    }

    public void ForceUIUpdate()
    {
        _lastUIUpdateProgress = GetRoundedUITime(_dayProgress);

        if (OnTimeChanged != null)
        {
            OnTimeChanged.Invoke();
        }
    }

    #region ISaveable Implementation

    public void SaveData(GameData gameData)
    {
        gameData.timeData.currentDay = _currentDay;
        gameData.timeData.dayProgress = _dayProgress;
        gameData.timeData.timeFlowEnabled = timeFlowEnabled;
        gameData.timeData.minutesPerRealSecond = minutesPerRealSecond;

        gameData.timeData.season = "Spring";
        gameData.timeData.year = 1;
    }

    public void LoadData(GameData gameData)
    {
        _currentDay = gameData.timeData.currentDay;
        _dayProgress = gameData.timeData.dayProgress;
        timeFlowEnabled = gameData.timeData.timeFlowEnabled;
        minutesPerRealSecond = gameData.timeData.minutesPerRealSecond;

        _lastUIUpdateProgress = GetRoundedUITime(_dayProgress);

        if (OnRealTimeChanged != null)
            OnRealTimeChanged.Invoke();

        if (OnTimeChanged != null)
            OnTimeChanged.Invoke();

        if (OnDayChanged != null)
            OnDayChanged.Invoke();
    }

    public void ResetToStartOfDay()
    {
        _dayProgress = defaultWakeUpTime;
        _lastUIUpdateProgress = GetRoundedUITime(_dayProgress);

        if (OnRealTimeChanged != null)
            OnRealTimeChanged.Invoke();

        if (OnTimeChanged != null)
            OnTimeChanged.Invoke();
    }

    public void ResetForNewGame()
    {
        _currentDay = startingDay;
        _dayProgress = defaultWakeUpTime;
        _lastUIUpdateProgress = GetRoundedUITime(_dayProgress);
        _isPaused = false;
        timeFlowEnabled = true;

        if (OnRealTimeChanged != null)
            OnRealTimeChanged.Invoke();

        if (OnTimeChanged != null)
            OnTimeChanged.Invoke();

        if (OnDayChanged != null)
            OnDayChanged.Invoke();
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
        }
    }

    #endregion
}
