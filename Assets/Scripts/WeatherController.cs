using UnityEngine;

namespace SowurShield.Core
{
    /// <summary>
    /// Rolls and applies weather effects at the start of each new day.
    /// Place one instance of this MonoBehaviour in SampleScene — it is not a singleton.
    ///
    /// Rain  → auto-waters every SoilBlockInteractable in the scene.
    /// Drought → accelerates water depletion on every CropGrowthManager in the scene.
    /// </summary>
    public class WeatherController : MonoBehaviour
    {
        public enum WeatherType
        {
            Clear,
            Rain,
            Drought
        }

        [SerializeField] [Range(0f, 1f)] private float rainChance = 0.20f;
        [SerializeField] [Range(0f, 1f)] private float droughtChance = 0.15f;

        public WeatherType CurrentWeather { get; private set; }

        private void Start()
        {
            CurrentWeather = WeatherType.Clear;

            if (GameTimeController.instance != null)
            {
                GameTimeController.instance.OnDayChanged += OnDayChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameTimeController.instance != null)
            {
                GameTimeController.instance.OnDayChanged -= OnDayChanged;
            }
        }

        private void OnDayChanged()
        {
            CurrentWeather = RollWeather();
            ApplyWeatherEffects(CurrentWeather);
        }

        private WeatherType RollWeather()
        {
            float roll = Random.value;

            if (roll < rainChance)
                return WeatherType.Rain;

            if (roll < rainChance + droughtChance)
                return WeatherType.Drought;

            return WeatherType.Clear;
        }

        private void ApplyWeatherEffects(WeatherType weather)
        {
            if (weather == WeatherType.Rain)
            {
                SoilBlockInteractable[] soilBlocks =
                    FindObjectsByType<SoilBlockInteractable>(FindObjectsSortMode.None);

                foreach (SoilBlockInteractable soil in soilBlocks)
                {
                    if (soil != null)
                        soil.AutoWater();
                }
            }
            else if (weather == WeatherType.Drought)
            {
                CropGrowthManager[] crops =
                    FindObjectsByType<CropGrowthManager>(FindObjectsSortMode.None);

                foreach (CropGrowthManager crop in crops)
                {
                    if (crop != null)
                        crop.SimulateDroughtDay();
                }
            }
        }

        /// <summary>Returns a human-readable label for the current weather.</summary>
        public string GetWeatherDisplayString()
        {
            return CurrentWeather switch
            {
                WeatherType.Rain    => "Rain",
                WeatherType.Drought => "Drought",
                _                   => "Clear"
            };
        }

        /// <summary>
        /// Previews what weather would be rolled next without changing any state.
        /// Useful for UI forecasting.
        /// </summary>
        public WeatherType RollWeatherForNextDay()
        {
            return RollWeather();
        }
    }
}
