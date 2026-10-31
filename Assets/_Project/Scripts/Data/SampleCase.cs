using System;
using UnityEngine;

namespace WeatherApp
{
    [Serializable]
    public class SampleCase
    {
        [Tooltip("Shown in the Test Mode dropdown and on the TEST MODE banner")]
        public string label = "New sample";

        [Tooltip("A .json file containing an OpenWeather response (or error body)")]
        public TextAsset json;

        [Tooltip("Tick to treat this sample as a failed HTTP request (e.g. a 401 error body)")]
        public bool simulateFailure;

        [Tooltip("HTTP status to pretend we received when Simulate Failure is ticked")]
        public long simulatedStatusCode = 401;
    }
}
