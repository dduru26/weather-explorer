namespace WeatherApp
{
    public enum WeatherCondition
    {
        Unknown,
        Clear,
        Cloudy,
        Rain,
        Snow
    }

    public enum DayPhase
    {
        Unknown,
        Day,
        Night
    }

    public static class WeatherConditionMapper
    {
        public static WeatherCondition FromCode(int code)
        {
            if (code >= 200 && code < 300) return WeatherCondition.Rain;
            if (code >= 300 && code < 400) return WeatherCondition.Rain;
            if (code >= 500 && code < 600) return WeatherCondition.Rain;
            if (code >= 600 && code < 700) return WeatherCondition.Snow;
            if (code == 800) return WeatherCondition.Clear;
            if (code > 800 && code < 900) return WeatherCondition.Cloudy;
            return WeatherCondition.Unknown;
        }
    }
}
