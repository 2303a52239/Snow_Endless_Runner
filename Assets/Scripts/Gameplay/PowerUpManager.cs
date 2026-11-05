using System.Text;
using UnityEngine;

namespace SnowRunner
{
    /// <summary>Handles the five power-ups: Magnet, Shield, Speed Boost, Score Multiplier, Coin Bonus.</summary>
    public class PowerUpManager
    {
        public const float MagnetDuration = 10f;
        public const float SpeedDuration = 6f;
        public const float MultiplierDuration = 10f;
        public const int CoinBonusAmount = 50;
        public const float MagnetRadius = 9f;

        readonly ScoreManager score;
        readonly CoinManager coins;

        float magnetT, speedT, multT, graceT;
        public bool ShieldActive { get; private set; }

        public bool MagnetActive => magnetT > 0f;
        public bool SpeedBoostActive => speedT > 0f;
        public bool MultiplierActive => multT > 0f;
        public float SpeedFactor => SpeedBoostActive ? 1.6f : 1f;
        public bool AnyActive => MagnetActive || SpeedBoostActive || MultiplierActive || ShieldActive;

        public PowerUpManager(ScoreManager score, CoinManager coins)
        {
            this.score = score;
            this.coins = coins;
        }

        public void Reset()
        {
            magnetT = speedT = multT = graceT = 0f;
            ShieldActive = false;
            score.Multiplier = 1;
        }

        public void Activate(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Magnet: magnetT = MagnetDuration; break;
                case PickupKind.Shield: ShieldActive = true; break;
                case PickupKind.SpeedBoost: speedT = SpeedDuration; break;
                case PickupKind.ScoreMultiplier: multT = MultiplierDuration; score.Multiplier = 2; break;
                case PickupKind.CoinBonus: coins.Add(CoinBonusAmount); break;
            }
        }

        public void GiveGrace(float seconds) { graceT = Mathf.Max(graceT, seconds); }

        public void Tick(float dt)
        {
            if (magnetT > 0f) magnetT -= dt;
            if (speedT > 0f) speedT -= dt;
            if (graceT > 0f) graceT -= dt;
            if (multT > 0f)
            {
                multT -= dt;
                if (multT <= 0f) score.Multiplier = 1;
            }
        }

        /// <summary>
        /// Called on collision with an obstacle. Returns true if the hit is absorbed
        /// (shield consumed, or speed boost / revive grace period active).
        /// </summary>
        public bool TryAbsorbHit()
        {
            if (graceT > 0f || SpeedBoostActive) return true;
            if (ShieldActive) { ShieldActive = false; GiveGrace(1.0f); return true; }
            return false;
        }

        public string Describe()
        {
            var sb = new StringBuilder();
            if (ShieldActive) sb.Append("SHIELD   ");
            if (MagnetActive) sb.Append("MAGNET ").Append(Mathf.CeilToInt(magnetT)).Append("s   ");
            if (SpeedBoostActive) sb.Append("SPEED ").Append(Mathf.CeilToInt(speedT)).Append("s   ");
            if (MultiplierActive) sb.Append("x2 SCORE ").Append(Mathf.CeilToInt(multT)).Append("s");
            return sb.ToString().TrimEnd();
        }
    }
}
