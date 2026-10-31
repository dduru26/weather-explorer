using System.Globalization;

namespace WeatherApp
{
    public static class CoordinateValidator
    {
        public const double MinLatitude = -90, MaxLatitude = 90;
        public const double MinLongitude = -180, MaxLongitude = 180;

        public static bool TryValidate(string latText, string lonText,
                                       out double latitude, out double longitude, out string error)
        {
            latitude = longitude = 0;
            error = null;

            bool latEmpty = string.IsNullOrWhiteSpace(latText);
            bool lonEmpty = string.IsNullOrWhiteSpace(lonText);

            if (latEmpty && lonEmpty) { error = "Enter a latitude and a longitude."; return false; }
            if (latEmpty) { error = "Latitude is empty. Enter a number between -90 and 90."; return false; }
            if (lonEmpty) { error = "Longitude is empty. Enter a number between -180 and 180."; return false; }

            if (!TryParse(latText, out latitude))
            {
                error = $"Latitude \"{latText.Trim()}\" isn't a number. Use digits and a dot, e.g. -1.9441.";
                return false;
            }
            if (!TryParse(lonText, out longitude))
            {
                error = $"Longitude \"{lonText.Trim()}\" isn't a number. Use digits and a dot, e.g. 30.0619.";
                return false;
            }

            if (latitude < MinLatitude || latitude > MaxLatitude)
            {
                error = $"Latitude must be between -90 and 90 (you entered {latitude.ToString(CultureInfo.InvariantCulture)}).";
                return false;
            }
            if (longitude < MinLongitude || longitude > MaxLongitude)
            {
                error = $"Longitude must be between -180 and 180 (you entered {longitude.ToString(CultureInfo.InvariantCulture)}).";
                return false;
            }

            return true;
        }

        private static bool TryParse(string text, out double value)
        {
            bool ok = double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            return ok && !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
