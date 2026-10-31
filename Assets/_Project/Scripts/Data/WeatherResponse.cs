using System;

namespace WeatherApp
{
    [Serializable]
    public class WeatherResponse
    {
        public Coord coord = new Coord();
        public WeatherInfo[] weather = Array.Empty<WeatherInfo>();
        public MainData main = new MainData();
        public WindData wind = new WindData();
        public CloudData clouds = new CloudData();
        public SysData sys = new SysData();
        public long dt = -1;
        public int timezone = 0;
        public string name = "";

        public WeatherInfo PrimaryWeather =>
            weather != null && weather.Length > 0 ? weather[0] : null;
    }

    [Serializable]
    public class Coord
    {
        public float lon = float.NaN;
        public float lat = float.NaN;
    }

    [Serializable]
    public class WeatherInfo
    {
        public int id = -1;
        public string main = "";
        public string description = "";
        public string icon = "";
    }

    [Serializable]
    public class MainData
    {
        public float temp = float.NaN;
        public float feels_like = float.NaN;
        public float temp_min = float.NaN;
        public float temp_max = float.NaN;
        public int pressure = -1;
        public int humidity = -1;
    }

    [Serializable]
    public class WindData
    {
        public float speed = float.NaN;
        public int deg = -1;
        public float gust = float.NaN;
    }

    [Serializable]
    public class CloudData
    {
        public int all = -1;
    }

    [Serializable]
    public class SysData
    {
        public string country = "";
        public long sunrise = -1;
        public long sunset = -1;
    }

    [Serializable]
    public class ApiErrorResponse
    {
        public string message = "";
    }
}
