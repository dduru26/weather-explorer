using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeatherApp
{
    public class CoordinateInputUI : MonoBehaviour
    {
        [SerializeField] private WeatherService service;
        [SerializeField] private TMP_InputField latitudeInput;
        [SerializeField] private TMP_InputField longitudeInput;
        [SerializeField] private Button getWeatherButton;
        [SerializeField, Tooltip("Small red text under the fields")]
        private TMP_Text validationText;

        private void Awake()
        {
            if (service == null) service = FindFirstObjectByType<WeatherService>();
            if (getWeatherButton != null) getWeatherButton.onClick.AddListener(Submit);

            if (latitudeInput != null) latitudeInput.onSubmit.AddListener(_ => Submit());
            if (longitudeInput != null) longitudeInput.onSubmit.AddListener(_ => Submit());

            ShowValidation(null);
        }

        public void Submit()
        {
            string latText = latitudeInput != null ? latitudeInput.text : "";
            string lonText = longitudeInput != null ? longitudeInput.text : "";

            if (!CoordinateValidator.TryValidate(latText, lonText, out double lat, out double lon, out string error))
            {
                ShowValidation(error);
                return;
            }

            ShowValidation(null);
            if (service != null) service.RequestCoordinates(lat, lon);
        }

        private void ShowValidation(string message)
        {
            if (validationText == null) return;
            validationText.text = message ?? "";
            validationText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }
}
