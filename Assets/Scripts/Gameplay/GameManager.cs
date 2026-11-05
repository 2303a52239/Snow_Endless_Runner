using UnityEngine;
using UnityEngine.SceneManagement;

namespace SnowRunner
{
    public enum GameState { Playing, Paused, GameOver }

    /// <summary>Central coordinator: state, speed ramp, collisions, pickups, scene flow.</summary>
    public class GameManager : MonoBehaviour
    {
        public const string MenuScene = "MainMenu";
        public const string GameScene = "Gameplay";
        public const float StartSpeed = 11f;
        public const float MaxSpeed = 30f;
        public const float SpeedRamp = 0.18f;     // units/sec^2 (gradual)
        public const int ReviveCost = 50;

        public static GameManager Instance { get; private set; }

        public PlayerController Player;
        public CameraFollow CameraRig;
        public GameplayUI Hud;
        public PauseMenuUI Pause;
        public GameOverUI GameOverScreen;

        public ScoreManager Score { get; private set; }
        public CoinManager Coins { get; private set; }
        public PowerUpManager Powers { get; private set; }
        public GameOverManager GameOver { get; private set; }

        ObstacleSpawner spawner;
        TrackGenerator track;
        EnvironmentSpawner environment;
        ParticleSystem footPuff;

        float baseSpeed;
        float runTime;
        float cleanupTimer;
        int revivesUsed;

        public GameState State { get; private set; } = GameState.Playing;
        public bool IsPlaying => State == GameState.Playing;
        public float Difficulty => Mathf.Clamp01((baseSpeed - StartSpeed) / (MaxSpeed - StartSpeed));

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            PickupRegistry.Clear();
            Time.timeScale = 1f;
        }

        /// <summary>Called by GameBootstrap once all references are wired.</summary>
        public void Init(ObstacleSpawner spawner, TrackGenerator track, EnvironmentSpawner env)
        {
            this.spawner = spawner;
            this.track = track;
            this.environment = env;

            Score = new ScoreManager();
            Coins = new CoinManager();
            Powers = new PowerUpManager(Score, Coins);
            GameOver = new GameOverManager(this);

            if (Player != null) Player.Game = this;
            if (Player != null) footPuff = SnowFx.CreateFootPuff(Player.transform);

            StartRun();
        }

        public void StartRun()
        {
            Time.timeScale = 1f;
            State = GameState.Playing;
            baseSpeed = StartSpeed;
            runTime = 0f;
            revivesUsed = 0;

            Score.Reset();
            Coins.ResetRun();
            Powers.Reset();

            spawner.ReleaseAll();
            Player.ResetPlayer();
            Player.Speed = baseSpeed;
            track.Begin(Player.transform);
            if (CameraRig != null) CameraRig.SnapToTarget();

            if (Pause != null) Pause.Hide();
            if (GameOverScreen != null) GameOverScreen.Hide();
            if (Hud != null) Hud.Show();
            if (footPuff != null && !footPuff.isPlaying) footPuff.Play();
            AudioManager.Instance.PlayMusic();
        }

        void Update()
        {
            if (State == GameState.Playing)
            {
                float dt = Time.deltaTime;
                runTime += dt;
                baseSpeed = Mathf.Min(MaxSpeed, baseSpeed + SpeedRamp * dt);
                float speed = baseSpeed * Powers.SpeedFactor;
                Player.Speed = speed;

                Powers.Tick(dt);
                Score.Tick(speed, dt);
                PickupRegistry.Tick(dt, Player.transform.position, Powers.MagnetActive, PowerUpManager.MagnetRadius, runTime);
                track.Tick(Difficulty);
                environment.Tick(Player.transform.position);

                cleanupTimer += dt;
                if (cleanupTimer > 0.4f)
                {
                    cleanupTimer = 0f;
                    spawner.Cleanup(Player.transform.position.z);
                }

                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) PauseGame();
            }
            else if (State == GameState.Paused)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) ResumeGame();
            }
            else if (State == GameState.GameOver)
            {
                environment.Tick(Player.transform.position);
            }
        }

        // ------------------------------------------------------------ gameplay events
        public void OnPickup(Pickup p)
        {
            if (p.Collecting) return;
            p.Collecting = true;
            p.CollectTimer = 0f;

            if (p.Kind == PickupKind.Coin)
            {
                Coins.Add(1);
                Score.AddBonus(5);
                AudioManager.Instance.PlayCoin();
                SnowFx.Burst(p.transform.position, new Color(1f, 0.85f, 0.2f), 8);
            }
            else
            {
                Powers.Activate(p.Kind);
                AudioManager.Instance.PlayPower();
                SnowFx.Burst(p.transform.position, new Color(0.5f, 0.9f, 1f), 24);
                if (Hud != null) Hud.Toast(PowerName(p.Kind));
            }
        }

        static string PowerName(PickupKind k)
        {
            switch (k)
            {
                case PickupKind.Magnet: return "MAGNET!";
                case PickupKind.Shield: return "SHIELD!";
                case PickupKind.SpeedBoost: return "SPEED BOOST!";
                case PickupKind.ScoreMultiplier: return "SCORE x2!";
                case PickupKind.CoinBonus: return "+" + PowerUpManager.CoinBonusAmount + " COINS!";
            }
            return "";
        }

        public void OnObstacleHit(Obstacle o)
        {
            if (State != GameState.Playing) return;
            if (Powers.TryAbsorbHit())
            {
                // Shield / boost smashes the obstacle
                SnowFx.Burst(o.transform.position + Vector3.up, new Color(0.8f, 0.95f, 1f), 30);
                AudioManager.Instance.PlayCrash();
                o.Release();
                return;
            }
            EndRun();
        }

        void EndRun()
        {
            State = GameState.GameOver;
            AudioManager.Instance.PlayCrash();
            AudioManager.Instance.StopMusic();
            Player.Speed = 0f;
            var anim = Player.GetComponent<PlayerAnimation>();
            if (anim != null) anim.SetState(PlayerAnimation.State.Dead);
            if (footPuff != null) footPuff.Stop();
            GameOver.Resolve();
            if (Hud != null) Hud.Hide();
            if (GameOverScreen != null) GameOverScreen.Show();
        }

        // ------------------------------------------------------------ UI actions
        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            if (Pause != null) Pause.Show();
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            if (Pause != null) Pause.Hide();
        }

        public void Restart()
        {
            if (State == GameState.Playing || State == GameState.Paused) GameOver.Resolve();
            StartRun();
        }

        public void GoHome()
        {
            if (State == GameState.Playing || State == GameState.Paused) GameOver.Resolve();
            Time.timeScale = 1f;
            AudioManager.Instance.StopMusic();
            SceneManager.LoadScene(MenuScene);
        }

        public bool CanRevive => State == GameState.GameOver && revivesUsed < 3 && SaveData.TotalCoins >= ReviveCost;

        public bool TryRevive()
        {
            if (State != GameState.GameOver || revivesUsed >= 3) return false;
            if (!Coins.TrySpend(ReviveCost)) return false;
            revivesUsed++;

            float z = Player.transform.position.z;
            spawner.ClearObstacles(z - 5f, z + 40f);
            Player.ResetPlayerAt(z);
            Player.Speed = baseSpeed;
            Powers.GiveGrace(3f);
            State = GameState.Playing;
            if (footPuff != null) footPuff.Play();
            if (GameOverScreen != null) GameOverScreen.Hide();
            if (Hud != null) Hud.Show();
            AudioManager.Instance.PlayMusic();
            return true;
        }
    }
}
