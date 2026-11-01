using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WeatherApp
{
    public class SampleWeatherProvider : IWeatherProvider
    {
        private readonly IReadOnlyList<SampleCase> samples;
        private readonly float simulatedDelaySeconds;

        public SampleWeatherProvider(IReadOnlyList<SampleCase> samples, float simulatedDelaySeconds)
        {
            this.samples = samples;
            this.simulatedDelaySeconds = simulatedDelaySeconds;
        }

        public IEnumerator Fetch(WeatherRequest request, Action<string> onSuccess, Action<string> onFailure)
        {
            if (simulatedDelaySeconds > 0f)
                yield return new WaitForSeconds(simulatedDelaySeconds);

            int i = request.SampleIndex;
            if (samples == null || i < 0 || i >= samples.Count)
            {
                onFailure("That test sample doesn't exist.");
                yield break;
            }

            SampleCase sample = samples[i];
            if (sample.json == null)
            {
                onFailure($"Test sample \"{sample.label}\" has no JSON file assigned.");
                yield break;
            }

            if (sample.simulateFailure)
                onFailure(WeatherErrorMessages.FromHttp(sample.simulatedStatusCode, sample.json.text));
            else
                onSuccess(sample.json.text);
        }
    }
}
