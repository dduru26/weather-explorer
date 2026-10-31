using UnityEngine;

namespace WeatherApp
{
    public static class ApiKeyLoader
    {
        public const string ResourcePath = "Config/openweather_key";
        private const string Placeholder = "PASTE_YOUR_KEY_HERE";

        public static string Load()
        {
            TextAsset file = Resources.Load<TextAsset>(ResourcePath);
            if (file == null)
            {
                Debug.LogWarning($"[WeatherApp] No API key file at Resources/{ResourcePath}.txt — see README.");
                return null;
            }

            string key = file.text.Trim();
            if (string.IsNullOrEmpty(key) || key == Placeholder)
            {
                Debug.LogWarning("[WeatherApp] API key file is empty or still the placeholder.");
                return null;
            }
            return key;
        }
    }
}
