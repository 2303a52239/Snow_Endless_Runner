using UnityEngine;
using UnityEngine.UI;

namespace SnowRunner
{
    /// <summary>In-game HUD: coins, score, distance, power-up indicator, pause button.</summary>
    public class GameplayUI : MonoBehaviour
    {
        GameManager game;
        GameObject root;
        Text coinsText, scoreText, distText, powerText, toastText;
        float refreshTimer, toastTimer;

        public void Build(Canvas canvas, GameManager g)
        {
            game = g;
            var rt = UIFactory.NewRect("HUD", canvas.transform);
            UIFactory.Stretch(rt);
            root = rt.gameObject;

            var tl = new Vector2(0f, 1f);
            var tc = new Vector2(0.5f, 1f);
            var tr = new Vector2(1f, 1f);
            var bc = new Vector2(0.5f, 0f);

            // coin badge
            var badge = UIFactory.Panel(rt, "CoinBadge", new Color(0.05f, 0.14f, 0.26f, 0.6f));
            UIFactory.Place(badge.rectTransform, tl, new Vector2(30f, -30f), new Vector2(260f, 70f));
            badge.raycastTarget = false;
            var coinIcon = UIFactory.Panel(badge.transform, "CoinIcon", UIFactory.Gold);
            UIFactory.Place(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(40f, 40f));
            coinIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
            coinIcon.raycastTarget = false;
            coinsText = UIFactory.Label(badge.transform, "0", 40, Color.white, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(180f, 60f), TextAnchor.MiddleRight);

            scoreText = UIFactory.Label(rt, "0", 72, Color.white, tc, new Vector2(0f, -30f), new Vector2(500f, 90f));
            scoreText.rectTransform.pivot = tc;
            distText = UIFactory.Label(rt, "0 m", 34, new Color(0.8f, 0.92f, 1f), tc, new Vector2(0f, -115f), new Vector2(400f, 50f));
            distText.rectTransform.pivot = tc;

            UIFactory.MakeButton(rt, "II", UIFactory.ButtonColor, tr, new Vector2(-30f, -30f), new Vector2(90f, 90f), () => game.PauseGame(), 44)
                .name = "PauseButton";
            var pb = rt.Find("PauseButton");
            if (pb != null) ((RectTransform)pb).pivot = tr;

            powerText = UIFactory.Label(rt, "", 40, new Color(0.6f, 1f, 0.9f), bc, new Vector2(0f, 60f), new Vector2(1200f, 60f));
            powerText.rectTransform.pivot = bc;

            toastText = UIFactory.Label(rt, "", 64, UIFactory.Gold, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1000f, 90f));
        }

        public void Show() { if (root != null) root.SetActive(true); Refresh(); }
        public void Hide() { if (root != null) root.SetActive(false); }

        public void Toast(string msg)
        {
            if (toastText == null) return;
            toastText.text = msg;
            toastTimer = 1.4f;
        }

        void Update()
        {
            if (game == null || root == null || !root.activeSelf) return;

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) toastText.text = "";
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.1f;
            Refresh();
        }

        void Refresh()
        {
            if (game == null || game.Score == null) return;
            coinsText.text = game.Coins.RunCoins.ToString();
            scoreText.text = game.Score.Score.ToString();
            distText.text = ((int)game.Score.Distance) + " m";
            powerText.text = game.Powers.Describe();
        }
    }
}
