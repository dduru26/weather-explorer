namespace WeatherApp
{
    public enum RequestSource
    {
        Preset,
        CustomCoordinates,
        TestSample
    }

    public sealed class WeatherRequest
    {
        public RequestSource Source { get; }
        public double Latitude { get; }
        public double Longitude { get; }
        public string Label { get; }
        public int SampleIndex { get; }

        public bool IsTest => Source == RequestSource.TestSample;

        private WeatherRequest(RequestSource source, double lat, double lon, string label, int sampleIndex)
        {
            Source = source;
            Latitude = lat;
            Longitude = lon;
            Label = label;
            SampleIndex = sampleIndex;
        }

        public static WeatherRequest ForPreset(LocationData location) =>
            new WeatherRequest(RequestSource.Preset, location.latitude, location.longitude, location.displayName, -1);

        public static WeatherRequest ForCoordinates(double lat, double lon) =>
            new WeatherRequest(RequestSource.CustomCoordinates, lat, lon, null, -1);

        public static WeatherRequest ForSample(int index, string label) =>
            new WeatherRequest(RequestSource.TestSample, double.NaN, double.NaN, label, index);
    }
}
