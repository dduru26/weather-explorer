using System;
using UnityEngine;

namespace WeatherApp
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleWeatherEffect : MonoBehaviour, IWeatherReactive
    {
        [Header("When to play")]
        [SerializeField, Tooltip("Conditions in which this effect is visible")]
        private WeatherCondition[] activeIn = { WeatherCondition.Rain };

        [Header("Amount")]
        [SerializeField, Tooltip("Particles per second at full strength")]
        private float baseEmissionRate = 600f;
        [SerializeField, Tooltip("Scale emission by the API's cloud cover % (use for clouds)")]
        private bool scaleWithCloudCover = false;
        [SerializeField, Range(0f, 1f), Tooltip("Emission multiplier at 0% cloud cover")]
        private float minCloudScale = 0.3f;

        [Header("Motion")]
        [SerializeField, Tooltip("Vertical speed in m/s (negative = falling). Rain ~ -15, snow ~ -1.5, clouds 0")]
        private float fallSpeed = -15f;
        [SerializeField, Tooltip("Sideways m/s added per 1 m/s of wind")]
        private float windInfluence = 0.6f;
        [SerializeField, Tooltip("Cap on sideways speed")]
        private float maxSideways = 12f;

        [Header("Tint (multiplies the particle material colour)")]
        [SerializeField] private Color dayTint = Color.white;
        [SerializeField] private Color nightTint = new Color(0.35f, 0.38f, 0.5f, 1f);

        private ParticleSystem ps;
        private Material material;
        private float currentRate, targetRate;
        private float currentWind, targetWind;
        private Color currentTint, targetTint;
        private float fadeSeconds = 2f;

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            var psRenderer = GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null) material = psRenderer.material;

            currentTint = targetTint = dayTint;
            ApplyEmission(0f);
            ApplyVelocity(0f);
            ApplyTint(currentTint);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void ApplyWeather(WeatherSnapshot snapshot, float transitionSeconds)
        {
            fadeSeconds = Mathf.Max(0.01f, transitionSeconds);
            bool active = Array.IndexOf(activeIn, snapshot.Condition) >= 0;

            float scale = 1f;
            if (scaleWithCloudCover && snapshot.CloudCoverPercent is int cover)
                scale = Mathf.Lerp(minCloudScale, 1f, cover / 100f);

            targetRate = active ? baseEmissionRate * scale : 0f;

            float wind = snapshot.WindSpeedMs ?? 0f;
            targetWind = Mathf.Clamp(wind * windInfluence, -maxSideways, maxSideways);

            targetTint = snapshot.DayPhase == DayPhase.Night ? nightTint : dayTint;
            if (!active) targetTint.a = 0f;

            if (active && !ps.isEmitting) ps.Play();
        }

        private void Update()
        {
            float step = Time.deltaTime / fadeSeconds;

            currentRate = Mathf.MoveTowards(currentRate, targetRate, baseEmissionRate * step);
            currentWind = Mathf.MoveTowards(currentWind, targetWind, maxSideways * step);
            currentTint = Color.Lerp(currentTint, targetTint, step * 3f);

            ApplyEmission(currentRate);
            ApplyVelocity(currentWind);
            ApplyTint(currentTint);

            if (targetRate <= 0f && currentRate <= 0.01f && currentTint.a <= 0.02f && ps.isPlaying)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void ApplyEmission(float rate)
        {
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;
        }

        private void ApplyVelocity(float sideways)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(sideways);
            velocity.y = new ParticleSystem.MinMaxCurve(fallSpeed);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
        }

        private void ApplyTint(Color tint)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            else material.color = tint;
        }
    }
}
