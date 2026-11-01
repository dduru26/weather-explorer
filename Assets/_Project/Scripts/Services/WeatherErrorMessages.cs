namespace WeatherApp
{
    public static class WeatherErrorMessages
    {
        public const string NoApiKey =
            "No API key found. Add your OpenWeather key to Resources/Config/openweather_key.txt (see README).";

        public const string ConnectionFailed =
            "Couldn't reach the weather service. Check your internet connection and try again.";

        public const string NoUsableData =
            "The weather service returned no usable data for this location. Try again or pick another location.";

        public static string FromHttp(long statusCode, string body)
        {
            string detail = null;
            if (JsonDeserializer.TryDeserialize(body, out ApiErrorResponse parsed, out _)
                && !string.IsNullOrWhiteSpace(parsed.message))
            {
                detail = parsed.message;
            }

            switch (statusCode)
            {
                case 0:
                    return ConnectionFailed;
                case 400:
                    return $"The weather service couldn't use those coordinates (400){Suffix(detail)}";
                case 401:
                    return "The API key was rejected (401). Check openweather_key.txt. New keys can take a couple of hours to activate.";
                case 404:
                    return "No weather data was found for this location (404).";
                case 429:
                    return "Too many requests (429). Wait a minute, then try again.";
                default:
                    if (statusCode >= 500)
                        return $"The weather service is having problems ({statusCode}). Try again shortly.";
                    return $"The weather request failed ({statusCode}){Suffix(detail)}";
            }
        }

        private static string Suffix(string detail) =>
            string.IsNullOrWhiteSpace(detail) ? "." : $": {detail}.";
    }
}
