namespace SnowRunner
{
    /// <summary>Resolves end-of-run data (best score) once per run.</summary>
    public class GameOverManager
    {
        readonly GameManager game;

        public int FinalScore { get; private set; }
        public int Best { get; private set; }
        public int RunCoins { get; private set; }
        public int Distance { get; private set; }
        public bool NewRecord { get; private set; }

        public GameOverManager(GameManager game) { this.game = game; }

        public void Resolve()
        {
            FinalScore = game.Score.Score;
            Distance = (int)game.Score.Distance;
            RunCoins = game.Coins.RunCoins;
            NewRecord = game.Score.CommitBest();
            Best = game.Score.BestScore;
        }
    }
}
