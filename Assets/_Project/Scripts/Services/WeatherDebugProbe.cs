using UnityEngine;

namespace WeatherApp
{
    public class WeatherDebugProbe : MonoBehaviour
    {
        [SerializeField] private WeatherService service;
        [SerializeField] private double latitude = -1.9441;
        [SerializeField] private double longitude = 30.0619;
        [SerializeField, Tooltip("-1 = live call at the coordinates above; 0 or more = run that test sample")]
        private int sampleIndex = -1;

        private void Awake()
        {
            if (service == null) service = FindFirstObjectByType<WeatherService>();
        }

        private void OnEnable()
        {
            if (service == null) return;
            service.RequestStarted += LogStarted;
            service.WeatherUpdated += LogSnapshot;
            service.RequestFailed += LogFailure;
        }

        private void OnDisable()
        {
            if (service == null) return;
            service.RequestStarted -= LogStarted;
            service.WeatherUpdated -= LogSnapshot;
            service.RequestFailed -= LogFailure;
        }

        private void Start() => Run();

        [ContextMenu("Run probe now")]
        private void Run()
        {
            if (service == null) return;
            if (sampleIndex >= 0) service.RequestSample(sampleIndex);
            else service.RequestCoordinates(latitude, longitude);
        }

        private static void LogStarted(WeatherRequest r) => Debug.Log($"[Probe] Request started ({r.Source})");

        private static void LogFailure(WeatherRequest r, string message) => Debug.LogWarning($"[Probe] Failed: {message}");

        private static void LogSnapshot(WeatherSnapshot s)
        {
            Debug.Log($"[Probe] {s.LocationLabel} | {s.Description} | {s.Condition} | {s.DayPhase} | " +
                      $"{s.TemperatureC} °C | {s.HumidityPercent}% | {s.WindSpeedMs} m/s | local {s.LocalTimeLabel}");
        }
    }
}
