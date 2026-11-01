using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace WeatherApp
{
    [Serializable]
    public class TimeOfDayProfile
    {
        public Color sunColor = Color.white;
        public float sunIntensity = 1.2f;
        public Vector3 sunRotation = new Vector3(50f, -30f, 0f);
        public Color ambientColor = new Color(0.55f, 0.6f, 0.7f);
        public Color skyTint = new Color(0.5f, 0.5f, 0.5f);
        public float skyExposure = 1.3f;
        public float atmosphereThickness = 1f;
        public Color fogColor = new Color(0.75f, 0.8f, 0.85f);
    }

    [Serializable]
    public class WeatherLook
    {
        [Range(0f, 1f), Tooltip("Multiplies sun, ambient and sky brightness")]
        public float lightMultiplier = 1f;
        [Range(0f, 1f), Tooltip("Blends the sky tint toward Overcast Tint")]
        public float overcast = 0f;
        [Tooltip("Exponential-squared fog density (0 = none)")]
        public float fogDensity = 0f;
        [Tooltip("Sun/moon disc size in the procedural sky (0 hides it)")]
        public float sunSize = 0.04f;
        [Range(0f, 1f), Tooltip("How white the ground turns (snow)")]
        public float snowCover = 0f;
    }

    public class EnvironmentController : MonoBehaviour
    {
        [SerializeField] private WeatherService service;

        [Header("Scene references")]
        [SerializeField] private Light sun;
        [SerializeField, Tooltip("A Skybox/Procedural material (a runtime copy is used)")]
        private Material skyboxTemplate;
        [SerializeField, Tooltip("Optional: ground whose colour turns white in snow")]
        private Renderer groundRenderer;
        [SerializeField, Tooltip("Optional: lamps/lights switched on only at night")]
        private GameObject[] nightOnlyObjects = Array.Empty<GameObject>();

        [Header("Blending")]
        [SerializeField, Tooltip("Seconds to blend between states (extra credit)")]
        private float transitionSeconds = 2.5f;

        [Header("Time of day")]
        [SerializeField] private TimeOfDayProfile day = new TimeOfDayProfile();
        [SerializeField] private TimeOfDayProfile night = new TimeOfDayProfile
        {
            sunColor = new Color(0.55f, 0.65f, 1f),
            sunIntensity = 0.25f,
            sunRotation = new Vector3(35f, 150f, 0f),
            ambientColor = new Color(0.08f, 0.1f, 0.18f),
            skyTint = new Color(0.15f, 0.18f, 0.35f),
            skyExposure = 0.15f,
            atmosphereThickness = 0.5f,
            fogColor = new Color(0.08f, 0.09f, 0.13f)
        };

        [Header("Weather looks")]
        [SerializeField] private Color overcastTint = new Color(0.3f, 0.3f, 0.32f);
        [SerializeField] private WeatherLook clear = new WeatherLook();
        [SerializeField] private WeatherLook cloudy = new WeatherLook { lightMultiplier = 0.65f, overcast = 0.6f, fogDensity = 0.004f, sunSize = 0f };
        [SerializeField] private WeatherLook rain = new WeatherLook { lightMultiplier = 0.45f, overcast = 0.85f, fogDensity = 0.012f, sunSize = 0f };
        [SerializeField] private WeatherLook snow = new WeatherLook { lightMultiplier = 0.6f, overcast = 0.9f, fogDensity = 0.018f, sunSize = 0f, snowCover = 1f };
        [SerializeField, Tooltip("Default for unsupported codes (fog, haze...) and missing data")]
        private WeatherLook unknown = new WeatherLook { lightMultiplier = 0.7f, overcast = 0.7f, fogDensity = 0.025f, sunSize = 0f };

        [Header("Ground colours")]
        [SerializeField] private Color groundColor = new Color(0.36f, 0.55f, 0.3f);
        [SerializeField] private Color snowColor = new Color(0.92f, 0.94f, 0.98f);

        private IWeatherReactive[] reactives;
        private Material skyInstance;
        private Material groundMaterial;
        private Coroutine blend;
        private WeatherSnapshot lastSnapshot;

        private struct EnvState
        {
            public Color sunColor, ambient, skyTint, fogColor, ground;
            public float sunIntensity, exposure, atmosphere, sunSize, fogDensity;
            public Quaternion sunRotation;

            public static EnvState Lerp(EnvState a, EnvState b, float t) => new EnvState
            {
                sunColor = Color.Lerp(a.sunColor, b.sunColor, t),
                ambient = Color.Lerp(a.ambient, b.ambient, t),
                skyTint = Color.Lerp(a.skyTint, b.skyTint, t),
                fogColor = Color.Lerp(a.fogColor, b.fogColor, t),
                ground = Color.Lerp(a.ground, b.ground, t),
                sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t),
                exposure = Mathf.Lerp(a.exposure, b.exposure, t),
                atmosphere = Mathf.Lerp(a.atmosphere, b.atmosphere, t),
                sunSize = Mathf.Lerp(a.sunSize, b.sunSize, t),
                fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t),
                sunRotation = Quaternion.Slerp(a.sunRotation, b.sunRotation, t)
            };
        }

        private EnvState current;

        private void Awake()
        {
            if (service == null) service = FindFirstObjectByType<WeatherService>();
            if (sun == null) sun = RenderSettings.sun;

            reactives = GetComponentsInChildren<IWeatherReactive>(true);

            if (skyboxTemplate != null)
            {
                skyInstance = new Material(skyboxTemplate);
                RenderSettings.skybox = skyInstance;
            }
            if (groundRenderer != null) groundMaterial = groundRenderer.material;

            RenderSettings.sun = sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.ambientMode = AmbientMode.Flat;
        }

        private void OnEnable()
        {
            if (service == null) return;
            service.WeatherUpdated += HandleWeatherUpdated;
            service.RequestFailed += HandleRequestFailed;
        }

        private void OnDisable()
        {
            if (service == null) return;
            service.WeatherUpdated -= HandleWeatherUpdated;
            service.RequestFailed -= HandleRequestFailed;
        }

        private void Start()
        {
            Show(WeatherSnapshot.NoData(false), instant: true);
        }

        private void HandleWeatherUpdated(WeatherSnapshot snapshot) => Show(snapshot, instant: false);

        private void HandleRequestFailed(WeatherRequest request, string message) =>
            Show(WeatherSnapshot.NoData(request.IsTest), instant: false);

        [ContextMenu("Re-apply last weather (after tweaking values in Play mode)")]
        private void ReapplyLast()
        {
            if (lastSnapshot != null) Show(lastSnapshot, instant: true);
        }

        private void Show(WeatherSnapshot snapshot, bool instant)
        {
            lastSnapshot = snapshot;
            float seconds = instant ? 0.01f : transitionSeconds;

            foreach (IWeatherReactive r in reactives)
                r.ApplyWeather(snapshot, seconds);

            bool isNight = snapshot.DayPhase == DayPhase.Night;
            foreach (GameObject go in nightOnlyObjects)
                if (go != null) go.SetActive(isNight);

            EnvState target = BuildTarget(snapshot);

            if (blend != null) StopCoroutine(blend);
            if (instant) { current = target; ApplyState(current); }
            else blend = StartCoroutine(BlendTo(target, seconds));
        }

        private EnvState BuildTarget(WeatherSnapshot s)
        {
            TimeOfDayProfile tod = s.DayPhase == DayPhase.Night ? night : day;
            WeatherLook look = LookFor(s.Condition);
            float m = look.lightMultiplier;

            return new EnvState
            {
                sunColor = tod.sunColor,
                sunIntensity = tod.sunIntensity * m,
                sunRotation = Quaternion.Euler(tod.sunRotation),
                ambient = tod.ambientColor * m,
                skyTint = Color.Lerp(tod.skyTint, overcastTint, look.overcast),
                exposure = tod.skyExposure * m,
                atmosphere = tod.atmosphereThickness,
                sunSize = look.sunSize,
                fogColor = tod.fogColor * Mathf.Lerp(1f, 0.85f, look.overcast),
                fogDensity = look.fogDensity,
                ground = Color.Lerp(groundColor, snowColor, look.snowCover) * (s.DayPhase == DayPhase.Night ? 0.6f : 1f)
            };
        }

        private WeatherLook LookFor(WeatherCondition condition)
        {
            switch (condition)
            {
                case WeatherCondition.Clear: return clear;
                case WeatherCondition.Cloudy: return cloudy;
                case WeatherCondition.Rain: return rain;
                case WeatherCondition.Snow: return snow;
                default: return unknown;
            }
        }

        private IEnumerator BlendTo(EnvState target, float seconds)
        {
            EnvState from = current;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / seconds;
                current = EnvState.Lerp(from, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                ApplyState(current);
                yield return null;
            }
            blend = null;
        }

        private void ApplyState(EnvState s)
        {
            if (sun != null)
            {
                sun.color = s.sunColor;
                sun.intensity = s.sunIntensity;
                sun.transform.rotation = s.sunRotation;
            }

            RenderSettings.ambientLight = s.ambient;
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(s.ambient);
            RenderSettings.ambientProbe = probe;

            RenderSettings.fogColor = s.fogColor;
            RenderSettings.fogDensity = s.fogDensity;

            if (skyInstance != null)
            {
                SetIfPresent(skyInstance, "_SkyTint", s.skyTint);
                SetIfPresent(skyInstance, "_Exposure", s.exposure);
                SetIfPresent(skyInstance, "_AtmosphereThickness", s.atmosphere);
                SetIfPresent(skyInstance, "_SunSize", s.sunSize);
            }

            if (groundMaterial != null)
            {
                if (groundMaterial.HasProperty("_BaseColor")) groundMaterial.SetColor("_BaseColor", s.ground);
                else groundMaterial.color = s.ground;
            }
        }

        private static void SetIfPresent(Material m, string property, Color value)
        {
            if (m.HasProperty(property)) m.SetColor(property, value);
        }

        private static void SetIfPresent(Material m, string property, float value)
        {
            if (m.HasProperty(property)) m.SetFloat(property, value);
        }
    }
}
