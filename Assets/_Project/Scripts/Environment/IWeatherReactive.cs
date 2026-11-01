namespace WeatherApp
{
    public interface IWeatherReactive
    {
        void ApplyWeather(WeatherSnapshot snapshot, float transitionSeconds);
    }
}
