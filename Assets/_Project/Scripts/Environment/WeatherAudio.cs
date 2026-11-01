using UnityEngine;

namespace WeatherApp
{
    public class WeatherAudio : MonoBehaviour, IWeatherReactive
    {
        [SerializeField] private AudioSource rainLoop;
        [SerializeField] private AudioSource windLoop;
        [SerializeField, Range(0f, 1f)] private float maxRainVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] private float maxWindVolume = 0.4f;
        [SerializeField, Tooltip("Wind speed (m/s) that gives full wind volume")]
        private float windForFullVolume = 12f;

        private float rainTarget, windTarget;
        private float fadeSeconds = 2f;

        private void Start()
        {
            Prepare(rainLoop);
            Prepare(windLoop);
        }

        public void ApplyWeather(WeatherSnapshot snapshot, float transitionSeconds)
        {
            fadeSeconds = Mathf.Max(0.01f, transitionSeconds);
            rainTarget = snapshot.Condition == WeatherCondition.Rain ? maxRainVolume : 0f;
            float wind = snapshot.WindSpeedMs ?? 0f;
            windTarget = Mathf.Clamp01(wind / windForFullVolume) * maxWindVolume;
        }

        private void Update()
        {
            Fade(rainLoop, rainTarget);
            Fade(windLoop, windTarget);
        }

        private void Fade(AudioSource source, float target)
        {
            if (source == null) return;
            source.volume = Mathf.MoveTowards(source.volume, target, Time.deltaTime / fadeSeconds);
        }

        private static void Prepare(AudioSource source)
        {
            if (source == null) return;
            source.loop = true;
            source.volume = 0f;
            if (!source.isPlaying) source.Play();
        }
    }
}
