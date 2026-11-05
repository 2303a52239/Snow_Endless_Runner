using UnityEngine;
using UnityEngine.UI;

namespace SnowRunner
{
    /// <summary>Game over screen: final score, best score, coins, Revive / Retry / Home.</summary>
    public class GameOverUI : MonoBehaviour
    {
        GameManager game;
        GameObject root;
        Text scoreText, bestText, coinsText, recordText, distText, reviveInfo;
        Button reviveBtn;

        public void Build(Canvas canvas, GameManager g)
        {
            game = g;
            var dim = UIFactory.Panel(canvas.transform, "GameOverOverlay", new Color(0f, 0.05f, 0.12f, 0.65f));
            UIFactory.Stretch(dim.rectTransform);
            root = dim.gameObject;

            var panel = UIFactory.Panel(dim.transform, "Panel", UIFactory.PanelColor);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 900f));

            var top = new Vector2(0.5f, 1f);
            UIFactory.Label(panel.transform, "GAME OVER", 88, new Color(1f, 0.5f, 0.5f), top, new Vector2(0f, -85f), new Vector2(700f, 110f));
            recordText = UIFactory.Label(panel.transform, "", 40, UIFactory.Gold, top, new Vector2(0f, -170f), new Vector2(700f, 50f));
            scoreText = UIFactory.Label(panel.transform, "", 54, Color.white, top, new Vector2(0f, -250f), new Vector2(700f, 70f));
            bestText = UIFactory.Label(panel.transform, "", 44, new Color(0.8f, 0.92f, 1f), top, new Vector2(0f, -320f), new Vector2(700f, 60f));
            coinsText = UIFactory.Label(panel.transform, "", 44, UIFactory.Gold, top, new Vector2(0f, -385f), new Vector2(700f, 60f));
            distText = UIFactory.Label(panel.transform, "", 36, new Color(0.8f, 0.92f, 1f), top, new Vector2(0f, -445f), new Vector2(700f, 50f));

            var c = new Vector2(0.5f, 0f);
            reviveBtn = UIFactory.MakeButton(panel.transform, "REVIVE", UIFactory.ButtonGreen, c, new Vector2(0f, 310f), new Vector2(520f, 100f), OnRevive);
            reviveInfo = UIFactory.Label(panel.transform, "", 28, new Color(1f, 1f, 1f, 0.8f), c, new Vector2(0f, 262f), new Vector2(700f, 36f));
            UIFactory.MakeButton(panel.transform, "RETRY", UIFactory.ButtonColor, c, new Vector2(-135f, 130f), new Vector2(250f, 100f), game.Restart);
            UIFactory.MakeButton(panel.transform, "HOME", UIFactory.ButtonDanger, c, new Vector2(135f, 130f), new Vector2(250f, 100f), game.GoHome);
            root.SetActive(false);
        }

        void OnRevive()
        {
            if (!game.TryRevive()) Refresh();
        }

        public void Show()
        {
            if (root == null) return;
            Refresh();
            root.SetActive(true);
        }

        public void Hide() { if (root != null) root.SetActive(false); }

        void Refresh()
        {
            var go = game.GameOver;
            scoreText.text = "SCORE: " + go.FinalScore;
            bestText.text = "BEST: " + go.Best;
            coinsText.text = "COINS: +" + go.RunCoins;
            distText.text = "DISTANCE: " + go.Distance + " m";
            recordText.text = go.NewRecord ? "NEW RECORD!" : "";

            bool can = game.CanRevive;
            reviveBtn.interactable = can;
            UIFactory.SetButtonText(reviveBtn, "REVIVE  (" + GameManager.ReviveCost + " coins)");
            reviveInfo.text = can ? "You have " + SaveData.TotalCoins + " coins"
                                  : "Need " + GameManager.ReviveCost + " coins (you have " + SaveData.TotalCoins + ")";
        }
    }
}
