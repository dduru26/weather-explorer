using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WeatherApp
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class TestModeUI : MonoBehaviour
    {
        [SerializeField] private WeatherService service;
        [SerializeField] private string placeholderLabel = "Test mode: choose a sample...";

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
            var options = new List<string> { placeholderLabel };
            if (service != null)
                foreach (SampleCase sample in service.SampleCases)
                    options.Add(sample.label);

            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(0);
        }

        private void HandleSelection(int index)
        {
            if (index <= 0 || service == null) return;
            service.RequestSample(index - 1);
        }

        private void HandleAnyRequestStarted(WeatherRequest request)
        {
            if (!request.IsTest) dropdown.SetValueWithoutNotify(0);
        }
    }
}
