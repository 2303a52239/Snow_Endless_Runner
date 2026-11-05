using UnityEngine;

namespace SnowRunner
{
    /// <summary>PlayerPrefs wrapper for persistent data.</summary>
    public static class SaveData
    {
        const string KBest = "SER_BestScore";
        const string KCoins = "SER_TotalCoins";
        const string KSound = "SER_Sound";

        public static int BestScore
        {
            get => PlayerPrefs.GetInt(KBest, 0);
            set { PlayerPrefs.SetInt(KBest, value); PlayerPrefs.Save(); }
        }

        public static int TotalCoins
        {
            get => PlayerPrefs.GetInt(KCoins, 0);
            set { PlayerPrefs.SetInt(KCoins, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(KSound, 1) == 1;
            set { PlayerPrefs.SetInt(KSound, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }
}
