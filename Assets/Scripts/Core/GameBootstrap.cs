using UnityEngine;

namespace SnowRunner
{
    /// <summary>
    /// Lives in the Gameplay scene. Builds the whole world procedurally at start
    /// (camera, lighting, player, track, spawner, UI) and wires every reference in code.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Time.timeScale = 1f;
            var world = new GameObject("World").transform;

            // Camera
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            EnvironmentSpawner.ApplyLighting(cam);

            // Player
            var playerGo = new GameObject("Player");
            playerGo.AddComponent<CapsuleCollider>();
            playerGo.AddComponent<Rigidbody>();
            PlayerAnimation.BuildCharacter(playerGo.transform);
            playerGo.AddComponent<PlayerAnimation>();
            var player = playerGo.AddComponent<PlayerController>();

            // Camera follow
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Target = playerGo.transform;

            // World systems
            var env = new EnvironmentSpawner();
            env.Build(world, cam);
            var spawner = new ObstacleSpawner(world);
            var track = new TrackGenerator(world, spawner);

            // Game manager + UI
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.Player = player;
            gm.CameraRig = follow;

            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.CreateCanvas("GameCanvas", 10);
            var hud = gmGo.AddComponent<GameplayUI>();
            var pause = gmGo.AddComponent<PauseMenuUI>();
            var over = gmGo.AddComponent<GameOverUI>();
            gm.Hud = hud;
            gm.Pause = pause;
            gm.GameOverScreen = over;
            hud.Build(canvas, gm);
            pause.Build(canvas, gm);
            over.Build(canvas, gm);

            gm.Init(spawner, track, env);
            AudioManager.Instance.PlayMusic();
        }
    }
}
