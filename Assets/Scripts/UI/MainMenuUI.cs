using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SnowRunner
{
    /// <summary>
    /// Main menu scene: scenic snowy backdrop (reuses the track scenery) plus the menu UI.
    /// Everything is built in code in Awake.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MainMenuUI : MonoBehaviour
    {
        GameObject mainPanel, settingsPanel;
        Button soundBtnMain, soundBtnSettings;
        Text bestText, coinsText;
        Transform camT, hero;
        float orbit;

        void Awake()
        {
            Time.timeScale = 1f;

            // ---- 3D backdrop
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            camT = cam.transform;
            EnvironmentSpawner.ApplyLighting(cam);

            var world = new GameObject("World").transform;
            var env = new EnvironmentSpawner();
            env.Build(world, cam);
            var seg1 = new TrackSegment(world, 0); seg1.Place(-20f);
            var seg2 = new TrackSegment(world, 1); seg2.Place(20f);

            var heroGo = new GameObject("Hero");
            heroGo.transform.position = new Vector3(0f, 0f, 4f);
            PlayerAnimation.BuildCharacter(heroGo.transform);
            heroGo.AddComponent<PlayerAnimation>();
            hero = heroGo.transform;
            env.Tick(new Vector3(0f, 0f, 4f));

            // ---- UI
            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.CreateCanvas("MenuCanvas", 10);
            BuildMain(canvas);
            BuildSettings(canvas);
            Refresh();
            AudioManager.Instance.PlayMusic();
        }

        void BuildMain(Canvas canvas)
        {
            var rt = UIFactory.NewRect("MainPanel", canvas.transform);
            UIFactory.Stretch(rt);
            mainPanel = rt.gameObject;

            var tc = new Vector2(0.5f, 1f);
            UIFactory.Label(rt, "SNOW", 150, Color.white, tc, new Vector2(0f, -70f), new Vector2(1400f, 170f)).rectTransform.pivot = tc;
            UIFactory.Label(rt, "ENDLESS RUNNER", 100, new Color(0.65f, 0.88f, 1f), tc, new Vector2(0f, -230f), new Vector2(1400f, 120f)).rectTransform.pivot = tc;

            var c = new Vector2(0.5f, 0.5f);
            var size = new Vector2(480f, 110f);
            UIFactory.MakeButton(rt, "PLAY", UIFactory.ButtonGreen, c, new Vector2(0f, -60f), size, OnPlay, 56);
            UIFactory.MakeButton(rt, "SETTINGS", UIFactory.ButtonColor, c, new Vector2(0f, -190f), size, () => ShowSettings(true), 48);
            soundBtnMain = UIFactory.MakeButton(rt, "", new Color(0.4f, 0.5f, 0.7f), c, new Vector2(0f, -320f), size, ToggleSound, 44);

            // best score / coins (bottom-left / bottom-right)
            var bl = new Vector2(0f, 0f);
            var br = new Vector2(1f, 0f);
            var badgeL = UIFactory.Panel(rt, "BestBadge", new Color(0.05f, 0.14f, 0.26f, 0.65f));
            UIFactory.Place(badgeL.rectTransform, bl, new Vector2(30f, 30f), new Vector2(420f, 90f));
            badgeL.raycastTarget = false;
            bestText = UIFactory.Label(badgeL.transform, "", 40, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 80f));

            var badgeR = UIFactory.Panel(rt, "CoinBadge", new Color(0.05f, 0.14f, 0.26f, 0.65f));
            UIFactory.Place(badgeR.rectTransform, br, new Vector2(-30f, 30f), new Vector2(420f, 90f));
            badgeR.raycastTarget = false;
            coinsText = UIFactory.Label(badgeR.transform, "", 40, UIFactory.Gold, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 80f));

            UIFactory.Label(rt, "Swipe or use Arrow keys / WASD", 30, new Color(1f, 1f, 1f, 0.8f),
                new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(900f, 40f)).rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        void BuildSettings(Canvas canvas)
        {
            var dim = UIFactory.Panel(canvas.transform, "SettingsPanel", new Color(0f, 0.05f, 0.12f, 0.7f));
            UIFactory.Stretch(dim.rectTransform);
            settingsPanel = dim.gameObject;

            var panel = UIFactory.Panel(dim.transform, "Panel", UIFactory.PanelColor);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 700f));
            UIFactory.Label(panel.transform, "SETTINGS", 76, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -85f), new Vector2(600f, 100f));

            var c = new Vector2(0.5f, 0.5f);
            var size = new Vector2(460f, 100f);
            soundBtnSettings = UIFactory.MakeButton(panel.transform, "", new Color(0.4f, 0.5f, 0.7f), c, new Vector2(0f, 90f), size, ToggleSound, 44);
            UIFactory.MakeButton(panel.transform, "RESET PROGRESS", UIFactory.ButtonDanger, c, new Vector2(0f, -40f), size, ResetProgress, 38);
            UIFactory.MakeButton(panel.transform, "BACK", UIFactory.ButtonColor, c, new Vector2(0f, -190f), size, () => ShowSettings(false), 48);
            settingsPanel.SetActive(false);
        }

        void ShowSettings(bool show)
        {
            settingsPanel.SetActive(show);
            mainPanel.SetActive(!show);
            Refresh();
        }

        void ToggleSound()
        {
            AudioManager.Instance.SetSound(!AudioManager.Instance.SoundOn);
            Refresh();
        }

        void ResetProgress()
        {
            SaveData.BestScore = 0;
            SaveData.TotalCoins = 0;
            Refresh();
        }

        void OnPlay()
        {
            SceneManager.LoadScene(GameManager.GameScene);
        }

        void Refresh()
        {
            string s = "SOUND: " + (AudioManager.Instance.SoundOn ? "ON" : "OFF");
            UIFactory.SetButtonText(soundBtnMain, s);
            UIFactory.SetButtonText(soundBtnSettings, s);
            bestText.text = "BEST: " + SaveData.BestScore;
            coinsText.text = "COINS: " + SaveData.TotalCoins;
        }

        void Update()
        {
            // slow camera orbit around the hero for a lively backdrop
            orbit += Time.deltaTime * 0.25f;
            if (camT != null && hero != null)
            {
                camT.position = hero.position + new Vector3(Mathf.Sin(orbit) * 4.5f, 2.6f, -Mathf.Cos(orbit) * 4.5f - 3f);
                camT.LookAt(hero.position + Vector3.up * 1.2f);
            }
        }
    }
}
