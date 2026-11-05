using UnityEngine;
using UnityEngine.UI;

namespace SnowRunner
{
    /// <summary>Pause overlay: Resume, Restart, Home, Sound toggle.</summary>
    public class PauseMenuUI : MonoBehaviour
    {
        GameObject root;
        Button soundBtn;

        public void Build(Canvas canvas, GameManager g)
        {
            var dim = UIFactory.Panel(canvas.transform, "PauseOverlay", new Color(0f, 0.05f, 0.12f, 0.6f));
            UIFactory.Stretch(dim.rectTransform);
            root = dim.gameObject;

            var panel = UIFactory.Panel(dim.transform, "Panel", UIFactory.PanelColor);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 760f));

            UIFactory.Label(panel.transform, "PAUSED", 80, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(560f, 100f));

            var c = new Vector2(0.5f, 0.5f);
            var size = new Vector2(440f, 100f);
            UIFactory.MakeButton(panel.transform, "RESUME", UIFactory.ButtonGreen, c, new Vector2(0f, 130f), size, g.ResumeGame);
            UIFactory.MakeButton(panel.transform, "RESTART", UIFactory.ButtonColor, c, new Vector2(0f, 10f), size, g.Restart);
            UIFactory.MakeButton(panel.transform, "HOME", UIFactory.ButtonDanger, c, new Vector2(0f, -110f), size, g.GoHome);
            soundBtn = UIFactory.MakeButton(panel.transform, "", new Color(0.4f, 0.5f, 0.7f), c, new Vector2(0f, -250f), size, ToggleSound);
            RefreshSoundLabel();
            root.SetActive(false);
        }

        void ToggleSound()
        {
            AudioManager.Instance.SetSound(!AudioManager.Instance.SoundOn);
            RefreshSoundLabel();
        }

        void RefreshSoundLabel()
        {
            UIFactory.SetButtonText(soundBtn, "SOUND: " + (AudioManager.Instance.SoundOn ? "ON" : "OFF"));
        }

        public void Show() { if (root != null) { RefreshSoundLabel(); root.SetActive(true); } }
        public void Hide() { if (root != null) root.SetActive(false); }
    }
}
