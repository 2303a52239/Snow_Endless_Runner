using UnityEngine;

namespace SnowRunner
{
    /// <summary>Tracks distance, score (with multiplier), and persists the best score.</summary>
    public class ScoreManager
    {
        float scoreF;
        public float Distance { get; private set; }
        public int Score => Mathf.FloorToInt(scoreF);
        public int BestScore => SaveData.BestScore;
        public int Multiplier { get; set; } = 1;

        public void Reset()
        {
            scoreF = 0f;
            Distance = 0f;
            Multiplier = 1;
        }

        public void Tick(float speed, float dt)
        {
            float d = speed * dt;
            Distance += d;
            scoreF += d * Multiplier;
        }

        public void AddBonus(int points) { scoreF += points * Multiplier; }

        /// <summary>Saves best score if beaten. Returns true when a new record was set.</summary>
        public bool CommitBest()
        {
            if (Score > SaveData.BestScore) { SaveData.BestScore = Score; return true; }
            return false;
        }
    }
}
