namespace PacmanWinForms
{
    /// <summary>
    /// Score, high score et vie bonus.
    /// </summary>
    public class ScoreKeeper
    {
        public int Score { get; private set; }
        public int HighScore { get; private set; }

        private bool bonusLifeAwarded = false;

        public ScoreKeeper(int initialHighScore = 0)
        {
            HighScore = initialHighScore;
        }

        // Nouvelle partie : le high score est conservé
        public void NewGame()
        {
            Score = 0;
            bonusLifeAwarded = false;
        }

        // Ajoute des points. Renvoie true si une vie bonus vient d'être gagnée.
        public bool Add(int points)
        {
            Score += points;

            if (Score > HighScore)
            {
                HighScore = Score;
            }

            // Règle arcade : une seule vie bonus par partie
            if (!bonusLifeAwarded && Score >= GameConfig.BonusLifeThreshold)
            {
                bonusLifeAwarded = true;
                return true;
            }

            return false;
        }
    }
}