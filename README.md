# Weather Explorer

A Unity 6 (URP) WebGL app that fetches current weather for any location from the OpenWeather API, shows it on a Canvas UI, and makes a small stylised 3D town react to it: clear or cloudy skies, rain, snow, fog, and day or night lighting based on the selected location's time.

**Live WebGL build:** https://donaldduru.itch.io/weather-responsive-environment

## Features

- Live data from the OpenWeather **Current Weather Data** API (`units=metric`) via `UnityWebRequest`
- 7 preset locations across UTC-9 to UTC+9 (Kigali, Tokyo, Dubai, London, Reykjavik, New York, Anchorage)
- Custom latitude/longitude entry with validation (empty, non-numeric, out of range)
- Weather card: location and country (or coordinates), description, °C, humidity %, wind m/s, daytime/nighttime with the location's local time
- Loading spinner, clear error messages, and "N/A" for values missing from the response
- Environment states: Clear, Cloudy, Rain, Snow, plus a hazy default for unsupported codes (fog, haze, dust); each works by day and by night
- Day/night taken from the API (icon `d`/`n`, fallback: observation time vs sunrise/sunset), never the computer's clock
- **Test mode** with 12 sample JSON responses, clearly labelled by a yellow banner
- Extras: Refresh button, smooth lighting/sky transitions, wind-driven rain and snow, cloud density from cloud cover, snowy ground, night street lamps, optional rain/wind audio

## Requirements

- Unity 6 LTS with **Web Build Support**
- Universal Render Pipeline (project template "Universal 3D")
- TextMeshPro Essential Resources (Window → TextMeshPro → Import TMP Essential Resources)
- A free OpenWeather API key

## Configure your API key (required)

The key is **not** in this repository. To run the project:

1. Create a free account at https://openweathermap.org and copy your key from **My API keys**.
2. In `Assets/_Project/Resources/Config/`, copy `openweather_key.example.txt` and rename the copy to `openweather_key.txt`.
3. Replace the placeholder text with your key (one line, nothing else) and save as plain text.
4. Press Play. If you see "The API key was rejected (401)", the key may still be activating; new keys can take a couple of hours.

`openweather_key.txt` is listed in `.gitignore`, so it is never committed.

> Note: a WebGL build runs in the browser, so the key is included in the built files. That's acceptable for a free coursework key; a production app would route requests through its own server.

## How to use

| Control | What it does |
| --- | --- |
| Preset location dropdown | Fetches live weather for that city |
| Latitude / Longitude + **Get weather** | Validates, then fetches live weather for those coordinates (Enter also submits) |
| **Refresh** | Repeats the last request |
| Test mode dropdown | Replays a sample JSON response through the same pipeline; a yellow TEST MODE banner names the sample |

## Test mode samples

| Sample | Demonstrates |
| --- | --- |
| Clear - Day / Clear - Night | Clear sky, both times of day |
| Cloudy - Day / Cloudy - Night | Cloud effects |
| Rain - Day / Rain - Night, windy | Rainfall, wind tilt |
| Snow - Day / Snow - Night, windy | Snowfall; snow at night is the combined case |
| Fog - unsupported code 741 | Default appearance for unsupported conditions |
| Missing fields (ocean point) | Missing name, weather array, humidity and wind → coordinates and N/A |
| Unavailable data (empty response) | Response with no usable data → error message |
| API failure (401 invalid key) | Failed request handling |

Sample files live in `Assets/_Project/SampleWeather/` and are registered on the `WeatherService` object.

## Project structure

```
Assets/_Project/
├─ Scripts/
│  ├─ Data/         WeatherResponse (JSON models), WeatherCondition, WeatherSnapshot,
│  │                WeatherRequest, LocationData, SampleCase
│  ├─ Core/         JsonDeserializer (generic), CoordinateValidator
│  ├─ Services/     IWeatherProvider, OpenWeatherProvider, SampleWeatherProvider,
│  │                WeatherService, WeatherErrorMessages, ApiKeyLoader, WeatherDebugProbe
│  ├─ UI/           WeatherDisplayUI, LocationSelectorUI, CoordinateInputUI,
│  │                TestModeUI, LoadingSpinner
│  └─ Environment/  EnvironmentController, ParticleWeatherEffect, WeatherAudio, IWeatherReactive
├─ Resources/Config/ openweather_key.example.txt (your openweather_key.txt goes here)
├─ SampleWeather/   12 sample JSON responses
└─ Scenes/          WeatherScene
```

## Architecture

- **WeatherService** is the single entry point for requests. It raises three C# events: `RequestStarted`, `WeatherUpdated` and `RequestFailed`.
- **IWeatherProvider** (interface) has two implementations: `OpenWeatherProvider` (live, `UnityWebRequest`) and `SampleWeatherProvider` (test mode). Both return raw JSON, so sample data goes through the same deserialization and display path as live data.
- **JsonDeserializer.TryDeserialize\<T\>()** (generic) wraps `JsonUtility` with error handling and parses both weather responses and API error bodies.
- **WeatherSnapshot** turns the raw models into display-ready, nullable values. The UI and the environment receive the same snapshot, so they always agree.
- **WeatherDisplayUI** and **EnvironmentController** subscribe to the events independently. The environment forwards each snapshot to every `IWeatherReactive` child (rain, snow, clouds, audio).
- Condition codes map to visuals: 2xx/3xx/5xx → Rain, 6xx → Snow, 800 → Clear, 801–804 → Cloudy, anything else → default haze.

## Building for WebGL

1. File → Build Profiles → Web → Switch Platform.
2. Player Settings → Publishing Settings: Compression Format **Gzip**, tick **Decompression Fallback**.
3. Build into `Builds/WebGL` (ignored by Git) and host the folder's contents on itch.io or GitHub Pages. Opening `index.html` directly from disk does not work; use Build And Run or a web host.

## Troubleshooting

| Problem | Fix |
| --- | --- |
| "No API key found" | Create `Assets/_Project/Resources/Config/openweather_key.txt` containing only your key |
| "The API key was rejected (401)" | New keys need time to activate; wait and retry |
| Build stuck loading on a web host | Enable Decompression Fallback and rebuild |
| UI clicks do nothing | Make sure the scene has an EventSystem using the Input System UI module |
