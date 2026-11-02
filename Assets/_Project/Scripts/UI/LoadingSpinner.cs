using UnityEngine;

namespace WeatherApp
{
    [RequireComponent(typeof(RectTransform))]
    public class LoadingSpinner : MonoBehaviour
    {
        [SerializeField, Tooltip("Degrees per second (negative = clockwise)")]
        private float degreesPerSecond = -270f;

        private RectTransform rect;

        private void Awake() => rect = GetComponent<RectTransform>();

        private void Update()
        {
            rect.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
