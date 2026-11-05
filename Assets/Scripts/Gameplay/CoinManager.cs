using UnityEngine;

namespace SnowRunner
{
    /// <summary>Coins collected this run + persistent total balance.</summary>
    public class CoinManager
    {
        public int RunCoins { get; private set; }
        public int Total => SaveData.TotalCoins;

        public void ResetRun() { RunCoins = 0; }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            RunCoins += amount;
            SaveData.TotalCoins = SaveData.TotalCoins + amount;
        }

        public bool TrySpend(int amount)
        {
            if (SaveData.TotalCoins < amount) return false;
            SaveData.TotalCoins = SaveData.TotalCoins - amount;
            return true;
        }
    }
}
