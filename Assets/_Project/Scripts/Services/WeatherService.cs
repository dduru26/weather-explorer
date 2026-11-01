using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WeatherApp
{
    [DefaultExecutionOrder(-100)]
    public class WeatherService : MonoBehaviour
    {
        [Header("Live API")]
        [SerializeField, Tooltip("Seconds before a live request gives up")]
        private int timeoutSeconds = 10;

        [Header("Test mode")]
        [SerializeField, Tooltip("Fake delay for samples so the loading indicator shows")]
        private float sampleDelaySeconds = 0.6f;

        [SerializeField, Tooltip("Sample JSON scenarios listed in the Test Mode dropdown")]
        private List<SampleCase> sampleCases = new List<SampleCase>();

        public event Action<WeatherRequest> RequestStarted;
        public event Action<WeatherSnapshot> WeatherUpdated;
        public event Action<WeatherRequest, string> RequestFailed;

        public IReadOnlyList<SampleCase> SampleCases => sampleCases;
        public bool IsBusy { get; private set; }
        public bool HasLastRequest => lastRequest != null;

        private IWeatherProvider liveProvider;
        private IWeatherProvider sampleProvider;
        private WeatherRequest lastRequest;
        private int latestRequestId;

        private void Awake()
        {
            liveProvider = new OpenWeatherProvider(ApiKeyLoader.Load(), timeoutSeconds);
            sampleProvider = new SampleWeatherProvider(sampleCases, sampleDelaySeconds);
        }

        public void RequestPreset(LocationData location) => Begin(WeatherRequest.ForPreset(location));

        public void RequestCoordinates(double latitude, double longitude) =>
            Begin(WeatherRequest.ForCoordinates(latitude, longitude));

        public void RequestSample(int index)
        {
            string label = index >= 0 && index < sampleCases.Count ? sampleCases[index].label : $"Sample {index}";
            Begin(WeatherRequest.ForSample(index, label));
        }

        public void Refresh()
        {
            if (lastRequest != null) Begin(lastRequest);
        }

        private void Begin(WeatherRequest request)
        {
            lastRequest = request;
            int id = ++latestRequestId;
            IsBusy = true;
            RequestStarted?.Invoke(request);
            StartCoroutine(Execute(request, id));
        }

        private IEnumerator Execute(WeatherRequest request, int id)
        {
            IWeatherProvider provider = request.IsTest ? sampleProvider : liveProvider;

            string json = null, error = null;
            yield return provider.Fetch(request, result => json = result, message => error = message);

            if (id != latestRequestId) yield break;
            IsBusy = false;

            if (error != null) { Fail(request, error); yield break; }

            if (!JsonDeserializer.TryDeserialize(json, out WeatherResponse response, out string parseError))
            {
                Fail(request, parseError);
                yield break;
            }

            if (!HasUsableData(response))
            {
                Fail(request, WeatherErrorMessages.NoUsableData);
                yield break;
            }

            WeatherUpdated?.Invoke(WeatherSnapshot.From(response, request));
        }

        private static bool HasUsableData(WeatherResponse r) =>
            r.PrimaryWeather != null || (r.main != null && !float.IsNaN(r.main.temp));

        private void Fail(WeatherRequest request, string message)
        {
            Debug.LogWarning($"[WeatherApp] Request failed: {message}");
            RequestFailed?.Invoke(request, message);
        }
    }
}
