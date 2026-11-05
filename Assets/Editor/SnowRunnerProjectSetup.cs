using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SnowRunner.EditorTools
{
    /// <summary>
    /// One-click project setup. Runs automatically the first time the project opens
    /// (when the scenes are missing) and is available from the menu: Snow Runner > Setup Project.
    /// Creates the MainMenu + Gameplay scenes, registers them in Build Settings and configures
    /// Android / Windows player settings. All game objects are created in code at runtime.
    /// </summary>
    [InitializeOnLoad]
    public static class SnowRunnerProjectSetup
    {
        const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        const string GameScenePath = "Assets/Scenes/Gameplay.unity";

        static SnowRunnerProjectSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (File.Exists(MenuScenePath) && File.Exists(GameScenePath) && BuildSettingsOk()) return;
            SetupProject();
        }

        static bool BuildSettingsOk()
        {
            var s = EditorBuildSettings.scenes;
            if (s.Length < 2) return false;
            return s[0].path == MenuScenePath && s[1].path == GameScenePath;
        }

        [MenuItem("Snow Runner/Setup Project")]
        public static void SetupProject()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Resources");

            CreateShaderKeeper();
            CreateMenuScene();
            CreateGameScene();
            RegisterScenes();
            ConfigurePlayerSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(MenuScenePath);
            Debug.Log("[Snow Runner] Project setup complete. Press Play in the MainMenu scene.");
        }

        // ---------------------------------------------------------------- scenes
        static void CreateMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera();
            var go = new GameObject("MenuController");
            go.AddComponent<MainMenuUI>();
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        static void CreateGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera();
            var go = new GameObject("GameBootstrap");
            go.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        static void AddCamera()
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = EnvironmentSpawner.FogColor;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 4f, -8f);
            camGo.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        }

        static void RegisterScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
        }

        // Makes sure the Standard shader is packed into builds (materials are created at runtime).
        static void CreateShaderKeeper()
        {
            const string path = "Assets/Resources/ShaderKeeper.mat";
            if (File.Exists(path)) return;
            var shader = Shader.Find("Standard");
            if (shader == null) return;
            AssetDatabase.CreateAsset(new Material(shader), path);
        }

        // ---------------------------------------------------------------- player settings
        static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "SnowRunnerStudio";
            PlayerSettings.productName = "Snow Endless Runner";

            // Orientation: landscape on Android
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Windows
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;

            try
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.snowrunnerstudio.snowendlessrunner");
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Snow Runner] Could not apply Android settings (is Android Build Support installed?): " + e.Message);
            }

            // Quality (mobile friendly defaults)
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 2;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowDistance = 45f;
        }

        [MenuItem("Snow Runner/Build Windows")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            BuildPipelineWrapper.Build(BuildTarget.StandaloneWindows64, "Builds/Windows/SnowEndlessRunner.exe");
        }

        [MenuItem("Snow Runner/Build Android APK")]
        public static void BuildAndroid()
        {
            Directory.CreateDirectory("Builds/Android");
            BuildPipelineWrapper.Build(BuildTarget.Android, "Builds/Android/SnowEndlessRunner.apk");
        }
    }

    static class BuildPipelineWrapper
    {
        public static void Build(BuildTarget target, string path)
        {
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Gameplay.unity" },
                locationPathName = path,
                target = target,
                options = BuildOptions.None
            };
            var report = UnityEditor.BuildPipeline.BuildPlayer(opts);
            Debug.Log("[Snow Runner] Build " + target + ": " + report.summary.result + " -> " + path);
        }
    }
}
