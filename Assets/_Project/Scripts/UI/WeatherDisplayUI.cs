using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeatherApp
{
    public class WeatherDisplayUI : MonoBehaviour
    {
        private const string Missing = "N/A";

        [SerializeField] private WeatherService service;

        [Header("Weather card values")]
        [SerializeField] private TMP_Text locationText;
        [SerializeField] private TMP_Text coordinatesText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text temperatureText;
        [SerializeField] private TMP_Text humidityText;
        [SerializeField] private TMP_Text windText;
        [SerializeField] private TMP_Text dayNightText;

        [Header("States")]
        [SerializeField, Tooltip("Spinner + 'Fetching weather...' (child object)")]
        private GameObject loadingGroup;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField, Tooltip("Red error box (child object)")]
        private GameObject errorGroup;
        [SerializeField] private TMP_Text errorText;

        [Header("Test mode")]
        [SerializeField, Tooltip("Yellow TEST MODE banner (child object)")]
        private GameObject testModeBanner;
        [SerializeField] private TMP_Text testModeBannerText;

        [Header("Controls")]
        [SerializeField, Tooltip("CanvasGroup on the controls panel; disabled while loading")]
        private CanvasGroup controlsGroup;
        [SerializeField] private Button refreshButton;

        private void Awake()
        {
            if (service == null) service = FindFirstObjectByType<WeatherService>();
        }

        private void OnEnable()
        {
            if (service == null) return;
            service.RequestStarted += HandleRequestStarted;
            service.WeatherUpdated += HandleWeatherUpdated;
            service.RequestFailed += HandleRequestFailed;
        }

        private void OnDisable()
        {
            if (service == null) return;
            service.RequestStarted -= HandleRequestStarted;
            service.WeatherUpdated -= HandleWeatherUpdated;
            service.RequestFailed -= HandleRequestFailed;
        }

        private void Start()
        {
            SetActive(loadingGroup, false);
            SetActive(errorGroup, false);
            SetActive(testModeBanner, false);
            ShowValues(null, "Choose a location to begin");
            if (refreshButton != null) refreshButton.interactable = false;
        }

        private void HandleRequestStarted(WeatherRequest request)
        {
            SetActive(errorGroup, false);
            SetActive(loadingGroup, true);
            if (loadingText != null)
                loadingText.text = request.IsTest ? "Loading test sample..." : "Fetching live weather...";
            SetControlsInteractable(false);
            ShowTestBanner(request.IsTest, request.Label);
        }

        private void HandleWeatherUpdated(WeatherSnapshot snapshot)
        {
            SetActive(loadingGroup, false);
            SetActive(errorGroup, false);
            SetControlsInteractable(true);
            ShowTestBanner(snapshot.IsTestData, snapshot.SourceLabel);
            ShowValues(snapshot, null);
        }

        private void HandleRequestFailed(WeatherRequest request, string message)
        {
            SetActive(loadingGroup, false);
            SetActive(errorGroup, true);
            if (errorText != null) errorText.text = message;
            SetControlsInteractable(true);
            ShowTestBanner(request.IsTest, request.Label);
            ShowValues(null, "No weather data");
        }

        private void ShowValues(WeatherSnapshot s, string emptyTitle)
        {
            var c = CultureInfo.InvariantCulture;

            SetText(locationText, s?.LocationLabel ?? emptyTitle ?? Missing);
            SetText(coordinatesText, s?.CoordinatesLabel ?? "");
            SetText(descriptionText, s?.Description ?? Missing);
            SetText(temperatureText, s?.TemperatureC is float t ? t.ToString("0.#", c) + " °C" : Missing);
            SetText(humidityText, s?.HumidityPercent is int h ? h + "%" : Missing);
            SetText(windText, s?.WindSpeedMs is float w ? w.ToString("0.#", c) + " m/s" : Missing);
            SetText(dayNightText, s == null ? Missing : DescribeDayPhase(s));
        }

        private static string DescribeDayPhase(WeatherSnapshot s)
        {
            string phase = s.DayPhase == DayPhase.Day ? "Daytime"
                         : s.DayPhase == DayPhase.Night ? "Nighttime"
                         : "Unknown";
            return s.LocalTimeLabel != null ? $"{phase}  (local time {s.LocalTimeLabel})" : phase;
        }

        private void ShowTestBanner(bool isTest, string label)
        {
            SetActive(testModeBanner, isTest);
            if (isTest && testModeBannerText != null)
                testModeBannerText.text = $"TEST MODE - sample data: {label}";
        }

        private void SetControlsInteractable(bool value)
        {
            if (controlsGroup != null) controlsGroup.interactable = value;
            if (refreshButton != null) refreshButton.interactable = value && service != null && service.HasLastRequest;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null) target.text = value;
        }

        private static void SetActive(GameObject go, bool value)
        {
            if (go != null) go.SetActive(value);
        }
    }
}
