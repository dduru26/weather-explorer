using System;
using UnityEngine;

namespace WeatherApp
{
    public static class JsonDeserializer
    {
        public static bool TryDeserialize<T>(string json, out T result, out string error) where T : class, new()
        {
            result = null;
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "The response was empty.";
                return false;
            }

            string trimmed = json.TrimStart();
            if (!trimmed.StartsWith("{"))
            {
                error = "The response was not a JSON object.";
                return false;
            }

            try
            {
                result = JsonUtility.FromJson<T>(json);
                if (result == null)
                {
                    error = "The response could not be read.";
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                error = $"The response could not be read ({e.Message}).";
                return false;
            }
        }
    }
}
