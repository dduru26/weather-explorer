using System;
using System.Globalization;

namespace WeatherApp
{
    public sealed class WeatherSnapshot
    {
        public string LocationLabel { get; private set; }
        public string CoordinatesLabel { get; private set; }
        public string Description { get; private set; }
        public WeatherCondition Condition { get; private set; }
        public int? ConditionCode { get; private set; }
        public DayPhase DayPhase { get; private set; }
        public float? TemperatureC { get; private set; }
        public int? HumidityPercent { get; private set; }
        public float? WindSpeedMs { get; private set; }
        public int? CloudCoverPercent { get; private set; }
        public string LocalTimeLabel { get; private set; }
        public bool IsTestData { get; private set; }
        public string SourceLabel { get; private set; }

        public static WeatherSnapshot NoData(bool isTest) => new WeatherSnapshot
        {
            LocationLabel = null,
            Condition = WeatherCondition.Unknown,
            DayPhase = DayPhase.Unknown,
            IsTestData = isTest
        };

        private WeatherSnapshot() { }

        public static WeatherSnapshot From(WeatherResponse r, WeatherRequest request)
        {
            var s = new WeatherSnapshot
            {
                IsTestData = request.IsTest,
                SourceLabel = request.IsTest ? request.Label : null
            };

            double lat = r.coord != null && !float.IsNaN(r.coord.lat) ? r.coord.lat : request.Latitude;
            double lon = r.coord != null && !float.IsNaN(r.coord.lon) ? r.coord.lon : request.Longitude;
            s.CoordinatesLabel = double.IsNaN(lat) || double.IsNaN(lon)
                ? null
                : string.Format(CultureInfo.InvariantCulture, "{0:0.####}°, {1:0.####}°", lat, lon);

            string country = r.sys != null ? r.sys.country : null;
            if (!string.IsNullOrWhiteSpace(r.name))
                s.LocationLabel = string.IsNullOrWhiteSpace(country) ? r.name : $"{r.name}, {country}";
            else
                s.LocationLabel = s.CoordinatesLabel != null ? $"Coordinates {s.CoordinatesLabel}" : null;

            WeatherInfo w = r.PrimaryWeather;
            if (w != null && w.id >= 0)
            {
                s.ConditionCode = w.id;
                s.Condition = WeatherConditionMapper.FromCode(w.id);
                s.Description = Capitalise(w.description);
            }
            else
            {
                s.Condition = WeatherCondition.Unknown;
            }

            if (r.main != null)
            {
                s.TemperatureC = float.IsNaN(r.main.temp) ? (float?)null : r.main.temp;
                s.HumidityPercent = r.main.humidity < 0 ? (int?)null : r.main.humidity;
            }
            if (r.wind != null)
                s.WindSpeedMs = float.IsNaN(r.wind.speed) ? (float?)null : r.wind.speed;
            if (r.clouds != null)
                s.CloudCoverPercent = r.clouds.all < 0 ? (int?)null : r.clouds.all;

            s.DayPhase = ResolveDayPhase(r, w);

            if (r.dt > 0)
            {
                DateTime local = DateTimeOffset.FromUnixTimeSeconds(r.dt + r.timezone).UtcDateTime;
                s.LocalTimeLabel = local.ToString("HH:mm", CultureInfo.InvariantCulture);
            }

            return s;
        }

        private static DayPhase ResolveDayPhase(WeatherResponse r, WeatherInfo w)
        {
            if (w != null && !string.IsNullOrEmpty(w.icon))
            {
                char last = w.icon[w.icon.Length - 1];
                if (last == 'd') return DayPhase.Day;
                if (last == 'n') return DayPhase.Night;
            }

            if (r.dt > 0 && r.sys != null && r.sys.sunrise > 0 && r.sys.sunset > 0)
                return r.dt >= r.sys.sunrise && r.dt < r.sys.sunset ? DayPhase.Day : DayPhase.Night;

            return DayPhase.Unknown;
        }

        private static string Capitalise(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}
