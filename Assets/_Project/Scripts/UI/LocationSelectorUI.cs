using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace WeatherApp
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class LocationSelectorUI : MonoBehaviour
    {
        [SerializeField] private WeatherService service;
        [SerializeField] private string placeholderLabel = "Choose a location...";

        [SerializeField, Tooltip("Varied time zones (UTC-9 to UTC+9) and climates")]
        private List<LocationData> presets = new List<LocationData>
        {
            new LocationData("Kigali, Rwanda",      -1.9441,   30.0619),
            new LocationData("Tokyo, Japan",        35.6762,  139.6503),
            new LocationData("Dubai, UAE",          25.2048,   55.2708),
            new LocationData("London, UK",          51.5072,   -0.1276),
            new LocationData("Reykjavik, Iceland",  64.1466,  -21.9426),
            new LocationData("New York, USA",       40.7128,  -74.0060),
            new LocationData("Anchorage, USA",      61.2181, -149.9003),
        };

        private TMP_Dropdown dropdown;

        private void Awake()
        {
            dropdown = GetComponent<TMP_Dropdown>();
            if (service == null) service = FindFirstObjectByType<WeatherService>();

            Populate();
            dropdown.onValueChanged.AddListener(HandleSelection);
        }

        private void OnEnable()
        {
            if (service != null) service.RequestStarted += HandleAnyRequestStarted;
        }

        private void OnDisable()
        {
            if (service != null) service.RequestStarted -= HandleAnyRequestStarted;
        }

        private void Populate()
        {
            var c = CultureInfo.InvariantCulture;
            var options = new List<string> { placeholderLabel };
            foreach (LocationData p in presets)
                options.Add(string.Format(c, "{0}  ({1:0.00}, {2:0.00})", p.displayName, p.latitude, p.longitude));

            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(0);
        }

        private void HandleSelection(int index)
        {
            if (index <= 0 || service == null) return;
            service.RequestPreset(presets[index - 1]);
        }

        private void HandleAnyRequestStarted(WeatherRequest request)
        {
            if (request.Source != RequestSource.Preset)
                dropdown.SetValueWithoutNotify(0);
        }
    }
}
