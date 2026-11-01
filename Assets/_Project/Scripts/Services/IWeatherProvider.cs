using System;
using System.Collections;

namespace WeatherApp
{
    public interface IWeatherProvider
    {
        IEnumerator Fetch(WeatherRequest request, Action<string> onSuccess, Action<string> onFailure);
    }
}
