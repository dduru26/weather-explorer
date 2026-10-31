using System;
using UnityEngine;

namespace WeatherApp
{
    [Serializable]
    public class LocationData
    {
        [Tooltip("Label shown in the dropdown, e.g. \"Tokyo, Japan\"")]
        public string displayName;

        [Tooltip("Latitude, -90 to 90")]
        public double latitude;

        [Tooltip("Longitude, -180 to 180")]
        public double longitude;

        public LocationData() { }

        public LocationData(string displayName, double latitude, double longitude)
        {
            this.displayName = displayName;
            this.latitude = latitude;
            this.longitude = longitude;
        }
    }
}
