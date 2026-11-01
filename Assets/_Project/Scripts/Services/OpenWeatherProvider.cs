using System;
using System.Collections;
using System.Globalization;
using UnityEngine.Networking;

namespace WeatherApp
{
    public class OpenWeatherProvider : IWeatherProvider
    {
        private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";

        private readonly string apiKey;
        private readonly int timeoutSeconds;

        public OpenWeatherProvider(string apiKey, int timeoutSeconds)
        {
            this.apiKey = apiKey;
            this.timeoutSeconds = timeoutSeconds;
        }

        public IEnumerator Fetch(WeatherRequest request, Action<string> onSuccess, Action<string> onFailure)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onFailure(WeatherErrorMessages.NoApiKey);
                yield break;
            }

            string url = string.Format(CultureInfo.InvariantCulture,
                "{0}?lat={1:0.######}&lon={2:0.######}&appid={3}&units=metric",
                BaseUrl, request.Latitude, request.Longitude, UnityWebRequest.EscapeURL(apiKey));

            using (UnityWebRequest web = UnityWebRequest.Get(url))
            {
                web.timeout = timeoutSeconds;
                yield return web.SendWebRequest();

                switch (web.result)
                {
                    case UnityWebRequest.Result.Success:
                        onSuccess(web.downloadHandler.text);
                        break;

                    case UnityWebRequest.Result.ProtocolError:
                        onFailure(WeatherErrorMessages.FromHttp(web.responseCode, web.downloadHandler.text));
                        break;

                    default:
                        onFailure(WeatherErrorMessages.ConnectionFailed);
                        break;
                }
            }
        }
    }
}
