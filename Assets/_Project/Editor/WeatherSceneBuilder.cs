using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace WeatherApp.EditorTools
{
    public static class WeatherSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string MaterialsDir = Root + "/Materials";
        private const string ScenesDir = Root + "/Scenes";
        private const string ScenePath = ScenesDir + "/WeatherScene.unity";
        private const string SamplesDir = Root + "/SampleWeather";
        private const string KeyDir = Root + "/Resources/Config";
        private const string KeyPath = KeyDir + "/openweather_key.txt";
        private const string BuildDir = "Builds/WebGL";

        private static readonly Color PanelColor = new Color32(16, 24, 40, 215);
        private static readonly Color MutedText = new Color32(154, 164, 178, 255);
        private static readonly Color Accent = new Color32(59, 130, 246, 255);
        private static readonly Color ErrorRed = new Color32(255, 122, 112, 255);

        private static TMP_DefaultControls.Resources ui;

        [MenuItem("Weather Explorer/1. Build Scene", priority = 1)]
        public static void BuildScene()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog("TextMeshPro needed",
                    "Import TextMeshPro first:\nWindow → TextMeshPro → Import TMP Essential Resources.\nThen run Build Scene again.", "OK");
                return;
            }
            if (Shader.Find("Universal Render Pipeline/Lit") == null)
            {
                EditorUtility.DisplayDialog("URP needed",
                    "This project isn't using the Universal Render Pipeline. Create it from the 'Universal 3D' template.", "OK");
                return;
            }
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Rebuild WeatherScene?",
                    "WeatherScene already exists. Rebuild it from scratch? Manual changes in that scene will be lost.",
                    "Rebuild", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolders();
            ui = LoadUiResources();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Material ground = LitMaterial("Ground", new Color32(92, 140, 77, 255));
            Material house = LitMaterial("House", new Color32(232, 220, 196, 255));
            Material roof = LitMaterial("Roof", new Color32(156, 74, 58, 255));
            Material leaves = LitMaterial("Leaves", new Color32(47, 107, 58, 255));
            Material trunk = LitMaterial("Trunk", new Color32(107, 74, 47, 255));
            Material pole = LitMaterial("Pole", new Color32(60, 64, 72, 255));
            Material sky = SkyMaterial();
            Texture2D soft = SoftParticleTexture();
            Material rainMat = ParticleMaterial("RainParticle", soft);
            Material snowMat = ParticleMaterial("SnowParticle", soft);
            Material cloudMat = ParticleMaterial("CloudParticle", soft);

            Light sun = SetUpCameraSunAndLighting(sky);

            GameObject groundGo = BuildTown(ground, house, roof, leaves, trunk, pole, out GameObject[] lampLights);

            WeatherService service = BuildService();

            BuildEnvironment(service, sun, sky, groundGo.GetComponent<Renderer>(), lampLights, rainMat, snowMat, cloudMat);

            BuildUi(service);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Debug.Log("[WeatherApp] WeatherScene built and saved to " + ScenePath + ". Press Play to test.");
            if (!File.Exists(KeyPath))
                Debug.LogWarning("[WeatherApp] No API key yet. Use Weather Explorer → Open API Key Folder, " +
                                 "then create openweather_key.txt containing only your key. Test mode works without it.");
        }

        [MenuItem("Weather Explorer/2. Apply WebGL Settings", priority = 2)]
        public static void ApplyWebGLSettings()
        {
            PlayerSettings.productName = "Weather Explorer";
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            AssetDatabase.SaveAssets();
            Debug.Log("[WeatherApp] WebGL settings applied: Gzip + Decompression Fallback, 1280×720, Run In Background.");
        }

        [MenuItem("Weather Explorer/3. Build WebGL", priority = 3)]
        public static void BuildWebGL()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("No scene", "Run Weather Explorer → 1. Build Scene first.", "OK");
                return;
            }
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog("Web Build Support missing",
                    "Install it: Unity Hub → Installs → gear → Add modules → Web Build Support.", "OK");
                return;
            }
            if (!File.Exists(KeyPath) &&
                !EditorUtility.DisplayDialog("No API key",
                    "openweather_key.txt is missing, so live weather won't work in this build (test mode will). Build anyway?",
                    "Build anyway", "Cancel"))
                return;

            ApplyWebGLSettings();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[WeatherApp] WebGL build succeeded ({summary.totalSize / (1024 * 1024)} MB) → {BuildDir}");
                EditorUtility.RevealInFinder(BuildDir + "/index.html");
            }
            else
            {
                Debug.LogError($"[WeatherApp] WebGL build {summary.result}. See the Console for details.");
            }
        }

        [MenuItem("Weather Explorer/Open API Key Folder", priority = 20)]
        public static void OpenKeyFolder()
        {
            EnsureFolders();
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(KeyDir + "/openweather_key.example.txt");
            Debug.Log(File.Exists(KeyPath)
                ? "[WeatherApp] openweather_key.txt found."
                : "[WeatherApp] Create openweather_key.txt in this folder containing only your OpenWeather key.");
        }

        private static void EnsureFolders()
        {
            foreach (string dir in new[] { MaterialsDir, ScenesDir, SamplesDir, KeyDir, Root + "/Audio" })
                Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        private static Material LitMaterial(string name, Color color)
        {
            Material m = LoadOrCreateMaterial(name, Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material SkyMaterial()
        {
            Material m = LoadOrCreateMaterial("SkyProcedural", Shader.Find("Skybox/Procedural"));
            SetFloat(m, "_SunSize", 0.04f);
            SetFloat(m, "_AtmosphereThickness", 1f);
            SetFloat(m, "_Exposure", 1.3f);
            if (m.HasProperty("_SkyTint")) m.SetColor("_SkyTint", new Color(0.5f, 0.5f, 0.5f));
            if (m.HasProperty("_GroundColor")) m.SetColor("_GroundColor", new Color(0.37f, 0.35f, 0.34f));
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material ParticleMaterial(string name, Texture2D texture)
        {
            Material m = LoadOrCreateMaterial(name, Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);

            SetFloat(m, "_Surface", 1f);
            SetFloat(m, "_Blend", 0f);
            SetFloat(m, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(m, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloat(m, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloat(m, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            SetFloat(m, "_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material LoadOrCreateMaterial(string name, Shader shader)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader)
            {
                m.shader = shader;
            }
            return m;
        }

        private static Texture2D SoftParticleTexture()
        {
            string path = MaterialsDir + "/SoftParticle.png";
            if (!File.Exists(path))
            {
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    a = a * a * (3f - 2f * a);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Light SetUpCameraSunAndLighting(Material sky)
        {
            GameObject cam = GameObject.Find("Main Camera");
            if (cam != null)
            {
                cam.transform.SetPositionAndRotation(new Vector3(0f, 6f, -20f), Quaternion.Euler(5f, 0f, 0f));
                var c = cam.GetComponent<Camera>();
                c.farClipPlane = 300f;
            }

            GameObject sunGo = GameObject.Find("Directional Light") ?? new GameObject("Directional Light", typeof(Light));
            sunGo.name = "Sun";
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light sun = sunGo.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.intensity = 1.2f;

            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.7f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.004f;
            RenderSettings.fogColor = new Color(0.75f, 0.8f, 0.85f);
            return sun;
        }

        private static GameObject BuildTown(Material ground, Material house, Material roof, Material leaves,
                                            Material trunk, Material pole, out GameObject[] lampLights)
        {
            GameObject groundGo = Primitive(PrimitiveType.Plane, "Ground", null, Vector3.zero, new Vector3(6f, 1f, 6f), ground);

            var town = new GameObject("Town").transform;

            Vector3[] housePositions =
            {
                new Vector3(-8f, 1.5f, 4f), new Vector3(-3f, 1.5f, 8f),
                new Vector3(4f, 1.5f, 6f), new Vector3(9f, 1.5f, 2f)
            };
            for (int i = 0; i < housePositions.Length; i++)
            {
                GameObject h = Primitive(PrimitiveType.Cube, $"House_{i + 1}", town, housePositions[i], Vector3.one * 3f, house);
                GameObject r = Primitive(PrimitiveType.Cube, "Roof", h.transform, Vector3.zero, new Vector3(1.1f, 0.2f, 1.1f), roof);
                r.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            }

            Vector3[] treePositions = { new Vector3(-12f, 0f, -2f), new Vector3(12f, 0f, 9f), new Vector3(0f, 0f, 14f) };
            for (int i = 0; i < treePositions.Length; i++)
            {
                var tree = new GameObject($"Tree_{i + 1}").transform;
                tree.SetParent(town, false);
                tree.localPosition = treePositions[i];
                Primitive(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 1f, 0f), new Vector3(0.4f, 1f, 0.4f), trunk, local: true);
                Primitive(PrimitiveType.Sphere, "Leaves", tree, new Vector3(0f, 2.8f, 0f), Vector3.one * 2.2f, leaves, local: true);
            }

            Vector3[] lampPositions = { new Vector3(-4f, 0f, -4f), new Vector3(5f, 0f, -3f) };
            lampLights = new GameObject[lampPositions.Length];
            for (int i = 0; i < lampPositions.Length; i++)
            {
                var post = new GameObject(i == 0 ? "LampPost_A" : "LampPost_B").transform;
                post.SetParent(town, false);
                post.localPosition = lampPositions[i];
                Primitive(PrimitiveType.Cylinder, "Pole", post, new Vector3(0f, 2f, 0f), new Vector3(0.15f, 2f, 0.15f), pole, local: true);

                var lightGo = new GameObject("LampLight", typeof(Light));
                lightGo.transform.SetParent(post, false);
                lightGo.transform.localPosition = new Vector3(0f, 4.2f, 0f);
                var l = lightGo.GetComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color32(255, 198, 128, 255);
                l.intensity = 4f;
                l.range = 10f;
                lampLights[i] = lightGo;
            }

            return groundGo;
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position,
                                            Vector3 scale, Material material, bool local = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            if (local || parent != null) go.transform.localPosition = position; else go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static readonly (string label, string file, bool fail)[] Samples =
        {
            ("Clear - Day (Dubai)", "clear_day", false),
            ("Clear - Night (Kigali)", "clear_night", false),
            ("Cloudy - Day (London)", "cloudy_day", false),
            ("Cloudy - Night (New York)", "cloudy_night", false),
            ("Rain - Day (Tokyo)", "rain_day", false),
            ("Rain - Night, windy (London)", "rain_night", false),
            ("Snow - Day (Anchorage)", "snow_day", false),
            ("Snow - Night, windy (Reykjavik)", "snow_night", false),
            ("Fog - unsupported code 741", "fog_unsupported", false),
            ("Missing fields (ocean point)", "missing_fields", false),
            ("Unavailable data (empty response)", "empty_response", false),
            ("API failure (401 invalid key)", "api_failure_401", true),
        };

        private static WeatherService BuildService()
        {
            var go = new GameObject("WeatherService");
            var service = go.AddComponent<WeatherService>();

            var so = new SerializedObject(service);
            SerializedProperty list = so.FindProperty("sampleCases");
            list.arraySize = Samples.Length;
            for (int i = 0; i < Samples.Length; i++)
            {
                SerializedProperty item = list.GetArrayElementAtIndex(i);
                var json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{SamplesDir}/{Samples[i].file}.json");
                if (json == null) Debug.LogWarning($"[WeatherApp] Sample file missing: {SamplesDir}/{Samples[i].file}.json");
                item.FindPropertyRelative("label").stringValue = Samples[i].label;
                item.FindPropertyRelative("json").objectReferenceValue = json;
                item.FindPropertyRelative("simulateFailure").boolValue = Samples[i].fail;
                item.FindPropertyRelative("simulatedStatusCode").longValue = 401;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var probe = go.AddComponent<WeatherDebugProbe>();
            probe.enabled = false;
            return service;
        }

        private struct FxConfig
        {
            public string name;
            public Vector3 position, boxScale;
            public float lifetime, sizeMin, sizeMax;
            public Color startColor;
            public int maxParticles;
            public bool stretched, noise, fadeInOut, sortByDistance;
            public WeatherCondition[] activeIn;
            public float baseRate, fallSpeed, windInfluence, maxSideways;
            public bool scaleWithCloudCover;
            public Color dayTint, nightTint;
        }

        private static void BuildEnvironment(WeatherService service, Light sun, Material sky, Renderer ground,
                                             GameObject[] lampLights, Material rainMat, Material snowMat, Material cloudMat)
        {
            var envGo = new GameObject("Environment");

            CreateFx(envGo.transform, rainMat, new FxConfig
            {
                name = "Rain", position = new Vector3(0f, 18f, -2f), boxScale = new Vector3(40f, 1f, 30f),
                lifetime = 1.6f, sizeMin = 0.06f, sizeMax = 0.06f, startColor = new Color32(191, 217, 255, 150),
                maxParticles = 6000, stretched = true,
                activeIn = new[] { WeatherCondition.Rain },
                baseRate = 1500f, fallSpeed = -15f, windInfluence = 0.6f, maxSideways = 10f,
                dayTint = Color.white, nightTint = new Color32(115, 128, 153, 255)
            });

            CreateFx(envGo.transform, snowMat, new FxConfig
            {
                name = "Snow", position = new Vector3(0f, 18f, -2f), boxScale = new Vector3(40f, 1f, 30f),
                lifetime = 12f, sizeMin = 0.08f, sizeMax = 0.18f, startColor = Color.white,
                maxParticles = 7000, noise = true,
                activeIn = new[] { WeatherCondition.Snow },
                baseRate = 500f, fallSpeed = -1.6f, windInfluence = 0.35f, maxSideways = 5f,
                dayTint = Color.white, nightTint = new Color32(140, 153, 179, 255)
            });

            CreateFx(envGo.transform, cloudMat, new FxConfig
            {
                name = "Clouds", position = new Vector3(0f, 16f, 30f), boxScale = new Vector3(100f, 4f, 60f),
                lifetime = 40f, sizeMin = 10f, sizeMax = 18f, startColor = new Color32(255, 255, 255, 200),
                maxParticles = 300, fadeInOut = true, sortByDistance = true,
                activeIn = new[] { WeatherCondition.Cloudy, WeatherCondition.Rain, WeatherCondition.Snow, WeatherCondition.Unknown },
                baseRate = 6f, scaleWithCloudCover = true, fallSpeed = 0f, windInfluence = 0.3f, maxSideways = 4f,
                dayTint = new Color32(235, 235, 242, 255), nightTint = new Color32(64, 69, 90, 255)
            });

            var audioGo = new GameObject("Audio");
            audioGo.transform.SetParent(envGo.transform, false);
            AudioSource rain = AddLoopSource(audioGo);
            AudioSource wind = AddLoopSource(audioGo);
            var weatherAudio = audioGo.AddComponent<WeatherAudio>();
            Wire(weatherAudio, ("rainLoop", rain), ("windLoop", wind));

            var controller = envGo.AddComponent<EnvironmentController>();
            Wire(controller, ("service", service), ("sun", sun), ("skyboxTemplate", sky), ("groundRenderer", ground));
            var so = new SerializedObject(controller);
            SerializedProperty lamps = so.FindProperty("nightOnlyObjects");
            lamps.arraySize = lampLights.Length;
            for (int i = 0; i < lampLights.Length; i++)
                lamps.GetArrayElementAtIndex(i).objectReferenceValue = lampLights[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioSource AddLoopSource(GameObject go)
        {
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.volume = 0f;
            source.spatialBlend = 0f;
            return source;
        }

        private static void CreateFx(Transform parent, Material material, FxConfig c)
        {
            var go = new GameObject(c.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = c.position;
            go.transform.localRotation = Quaternion.identity;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = c.lifetime;
            main.startSpeed = 0f;
            main.startSize = Mathf.Approximately(c.sizeMin, c.sizeMax)
                ? new ParticleSystem.MinMaxCurve(c.sizeMin)
                : new ParticleSystem.MinMaxCurve(c.sizeMin, c.sizeMax);
            main.startColor = c.startColor;
            main.maxParticles = c.maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = c.boxScale;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(c.fallSpeed);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);

            if (c.noise)
            {
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 0.6f;
                noise.frequency = 0.4f;
                noise.scrollSpeed = 0.3f;
            }

            if (c.fadeInOut)
            {
                var col = ps.colorOverLifetime;
                col.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[]
                    {
                        new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f),
                        new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f)
                    });
                col.color = new ParticleSystem.MinMaxGradient(gradient);
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = c.stretched ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (c.stretched)
            {
                renderer.velocityScale = 0.05f;
                renderer.lengthScale = 1f;
            }
            if (c.sortByDistance) renderer.sortMode = ParticleSystemSortMode.Distance;

            var effect = go.AddComponent<ParticleWeatherEffect>();
            var so = new SerializedObject(effect);
            SerializedProperty active = so.FindProperty("activeIn");
            active.arraySize = c.activeIn.Length;
            for (int i = 0; i < c.activeIn.Length; i++)
                active.GetArrayElementAtIndex(i).intValue = (int)c.activeIn[i];
            so.FindProperty("baseEmissionRate").floatValue = c.baseRate;
            so.FindProperty("scaleWithCloudCover").boolValue = c.scaleWithCloudCover;
            so.FindProperty("minCloudScale").floatValue = 0.3f;
            so.FindProperty("fallSpeed").floatValue = c.fallSpeed;
            so.FindProperty("windInfluence").floatValue = c.windInfluence;
            so.FindProperty("maxSideways").floatValue = c.maxSideways;
            so.FindProperty("dayTint").colorValue = c.dayTint;
            so.FindProperty("nightTint").colorValue = c.nightTint;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildUi(WeatherService service)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Transform canvas = canvasGo.transform;

            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif

            Image banner = Panel("TestModeBanner", canvas, new Color32(255, 213, 74, 255), rounded: false);
            Place(banner.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 56f));
            TextMeshProUGUI bannerText = Label("BannerText", banner.transform, "TEST MODE", 24f, Color.black,
                                               FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(bannerText.rectTransform, 0f);

            Image controls = Panel("ControlsPanel", canvas, PanelColor, rounded: true);
            RectTransform crt = controls.rectTransform;
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(0f, 1f);
            crt.pivot = new Vector2(0f, 0.5f);
            crt.offsetMin = new Vector2(24f, 24f);
            crt.offsetMax = new Vector2(24f + 460f, -96f);
            CanvasGroup controlsGroup = controls.gameObject.AddComponent<CanvasGroup>();
            VerticalLayout(controls.gameObject, 24, 12f);

            Fixed(Label("TitleText", controls.transform, "Weather Explorer", 36f, Color.white, FontStyles.Bold), 48f);
            Fixed(Label("PresetLabel", controls.transform, "Preset location", 20f, MutedText), 28f);
            TMP_Dropdown presetDropdown = Dropdown("PresetDropdown", controls.transform);
            Fixed(Label("CustomLabel", controls.transform, "Custom coordinates", 20f, MutedText), 28f);
            TMP_InputField latInput = Input("LatitudeInput", controls.transform, "Latitude (-90 to 90)");
            TMP_InputField lonInput = Input("LongitudeInput", controls.transform, "Longitude (-180 to 180)");
            Button getWeather = ButtonUi("GetWeatherButton", controls.transform, "Get weather", Accent);
            TextMeshProUGUI validation = Label("ValidationText", controls.transform, "", 18f, ErrorRed);
            Fixed(validation, 50f);
            Button refresh = ButtonUi("RefreshButton", controls.transform, "Refresh", new Color32(71, 85, 105, 255));
            Fixed(Label("TestLabel", controls.transform, "Test mode (sample JSON)", 20f, MutedText), 28f);
            TMP_Dropdown testDropdown = Dropdown("TestDropdown", controls.transform);

            Image card = Panel("WeatherCard", canvas, PanelColor, rounded: true);
            Place(card.rectTransform, Vector2.one, Vector2.one, Vector2.one, new Vector2(-24f, -96f), new Vector2(540f, 640f));
            VerticalLayout(card.gameObject, 28, 10f);

            TextMeshProUGUI locationText = Fixed(Label("LocationValue", card.transform, "Location", 40f, Color.white, FontStyles.Bold), 52f);
            locationText.enableAutoSizing = true;
            locationText.fontSizeMin = 22f;
            locationText.fontSizeMax = 40f;
            locationText.textWrappingMode = TextWrappingModes.NoWrap;
            TextMeshProUGUI coordsText = Fixed(Label("CoordinatesValue", card.transform, "", 20f, MutedText), 28f);
            TextMeshProUGUI descText = Fixed(Label("DescriptionValue", card.transform, "Description", 28f, Color.white), 38f);
            TextMeshProUGUI tempText = Fixed(Label("TemperatureValue", card.transform, "-- °C", 72f, Color.white, FontStyles.Bold), 86f);
            TextMeshProUGUI humidityText = Row("HumidityRow", card.transform, "Humidity");
            TextMeshProUGUI windText = Row("WindRow", card.transform, "Wind speed");
            TextMeshProUGUI timeText = Row("TimeRow", card.transform, "Time of day");

            Image loading = Panel("LoadingGroup", card.transform, new Color(0f, 0f, 0f, 0.67f), rounded: true);
            Stretch(loading.rectTransform, 0f);
            loading.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var spinnerRt = NewUi("Spinner", loading.transform);
            Place(spinnerRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(64f, 64f));
            var spinner = spinnerRt.gameObject.AddComponent<Image>();
            spinner.sprite = ui.knob;
            spinner.type = Image.Type.Filled;
            spinner.fillMethod = Image.FillMethod.Radial360;
            spinner.fillAmount = 0.75f;
            spinner.color = Accent;
            spinner.raycastTarget = false;
            spinnerRt.gameObject.AddComponent<LoadingSpinner>();
            TextMeshProUGUI loadingText = Label("LoadingText", loading.transform, "Fetching live weather...", 22f, Color.white,
                                                FontStyles.Normal, TextAlignmentOptions.Center);
            Place(loadingText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                  new Vector2(0f, -40f), new Vector2(440f, 40f));

            Image error = Panel("ErrorGroup", card.transform, new Color32(122, 31, 31, 235), rounded: true);
            error.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            RectTransform ert = error.rectTransform;
            ert.anchorMin = new Vector2(0f, 0f);
            ert.anchorMax = new Vector2(1f, 0f);
            ert.pivot = new Vector2(0.5f, 0f);
            ert.anchoredPosition = new Vector2(0f, 16f);
            ert.sizeDelta = new Vector2(-32f, 130f);
            TextMeshProUGUI errorText = Label("ErrorText", error.transform, "Error message", 20f, Color.white);
            Stretch(errorText.rectTransform, 16f);

            var display = canvasGo.AddComponent<WeatherDisplayUI>();
            Wire(display,
                ("service", service),
                ("locationText", locationText), ("coordinatesText", coordsText), ("descriptionText", descText),
                ("temperatureText", tempText), ("humidityText", humidityText), ("windText", windText),
                ("dayNightText", timeText),
                ("loadingGroup", loading.gameObject), ("loadingText", loadingText),
                ("errorGroup", error.gameObject), ("errorText", errorText),
                ("testModeBanner", banner.gameObject), ("testModeBannerText", bannerText),
                ("controlsGroup", controlsGroup), ("refreshButton", refresh));

            Wire(presetDropdown.gameObject.AddComponent<LocationSelectorUI>(), ("service", service));
            Wire(testDropdown.gameObject.AddComponent<TestModeUI>(), ("service", service));
            Wire(controls.gameObject.AddComponent<CoordinateInputUI>(),
                ("service", service), ("latitudeInput", latInput), ("longitudeInput", lonInput),
                ("getWeatherButton", getWeather), ("validationText", validation));

            UnityEventTools.AddPersistentListener(refresh.onClick, new UnityAction(service.Refresh));

            banner.gameObject.SetActive(false);
            loading.gameObject.SetActive(false);
            error.gameObject.SetActive(false);
            validation.gameObject.SetActive(false);
        }

        private static TMP_DefaultControls.Resources LoadUiResources() => new TMP_DefaultControls.Resources
        {
            standard = Builtin("UI/Skin/UISprite.psd"),
            background = Builtin("UI/Skin/Background.psd"),
            inputField = Builtin("UI/Skin/InputFieldBackground.psd"),
            knob = Builtin("UI/Skin/Knob.psd"),
            checkmark = Builtin("UI/Skin/Checkmark.psd"),
            dropdown = Builtin("UI/Skin/DropdownArrow.psd"),
            mask = Builtin("UI/Skin/UIMask.psd")
        };

        private static Sprite Builtin(string path) => AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

        private static RectTransform NewUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image Panel(string name, Transform parent, Color color, bool rounded)
        {
            RectTransform rt = NewUi(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded)
            {
                img.sprite = ui.background;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color,
                                             FontStyles style = FontStyles.Normal,
                                             TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            GameObject go = TMP_DefaultControls.CreateText(ui);
            go.name = name;
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            return t;
        }

        private static T Fixed<T>(T component, float height) where T : Component
        {
            var le = component.GetComponent<LayoutElement>();
            if (le == null) le = component.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleHeight = 0f;
            return component;
        }

        private static TMP_Dropdown Dropdown(string name, Transform parent)
        {
            GameObject go = TMP_DefaultControls.CreateDropdown(ui);
            go.name = name;
            go.transform.SetParent(parent, false);
            var dd = go.GetComponent<TMP_Dropdown>();
            dd.captionText.fontSize = 20f;
            dd.itemText.fontSize = 18f;

            var template = (RectTransform)go.transform.Find("Template");
            if (template != null) template.sizeDelta = new Vector2(template.sizeDelta.x, 300f);
            var content = go.transform.Find("Template/Viewport/Content") as RectTransform;
            if (content != null) content.sizeDelta = new Vector2(content.sizeDelta.x, 40f);
            var item = go.transform.Find("Template/Viewport/Content/Item") as RectTransform;
            if (item != null) item.sizeDelta = new Vector2(item.sizeDelta.x, 40f);

            Fixed(dd, 52f);
            return dd;
        }

        private static TMP_InputField Input(string name, Transform parent, string placeholder)
        {
            GameObject go = TMP_DefaultControls.CreateInputField(ui);
            go.name = name;
            go.transform.SetParent(parent, false);
            var input = go.GetComponent<TMP_InputField>();
            input.contentType = TMP_InputField.ContentType.Standard;
            input.pointSize = 20f;
            if (input.placeholder is TMP_Text ph) ph.text = placeholder;
            Fixed(input, 52f);
            return input;
        }

        private static Button ButtonUi(string name, Transform parent, string label, Color color)
        {
            GameObject go = TMP_DefaultControls.CreateButton(ui);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var text = go.GetComponentInChildren<TMP_Text>();
            text.text = label;
            text.fontSize = 22f;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            var button = go.GetComponent<Button>();
            Fixed(button, 52f);
            return button;
        }

        private static TextMeshProUGUI Row(string name, Transform parent, string caption)
        {
            RectTransform row = NewUi(name, parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            h.spacing = 12f;
            Fixed(row, 40f);

            Label("Label", row, caption, 22f, MutedText);
            return Label("Value", row, "N/A", 22f, Color.white, FontStyles.Normal, TextAlignmentOptions.Right);
        }

        private static void VerticalLayout(GameObject go, int padding, float spacing)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.spacing = spacing;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                  Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, float margin)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
        }

        private static void Wire(Object target, params (string property, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var pair in refs)
            {
                SerializedProperty p = so.FindProperty(pair.property);
                if (p == null)
                {
                    Debug.LogError($"[WeatherApp] {target.GetType().Name} has no field '{pair.property}'.");
                    continue;
                }
                p.objectReferenceValue = pair.value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Material m, string property, float value)
        {
            if (m.HasProperty(property)) m.SetFloat(property, value);
        }
    }
}
