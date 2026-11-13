using System;

namespace PacmanWinForms
{
    /// <summary>
    /// Points des joueurs, high score (partagé) et vie bonus.
    /// </summary>
    public class ScoreKeeper
    {
        public int HighScore { get; private set; }

        public ScoreKeeper(int initialHighScore = 0)
        {
            HighScore = initialHighScore;
        }

        // Ajoute des points au joueur. Renvoie true si une vie bonus vient d'être gagnée.
        public bool Add(PlayerState player, int points)
        {
            player.Score += points;

            if (player.Score > HighScore)
            {
                HighScore = player.Score;
            }

            int threshold = Settings.BonusLife;
            if (threshold > 0 && !player.BonusLifeAwarded && player.Score >= threshold)
            {
                player.BonusLifeAwarded = true;
                return true;
            }

            return false;
        }
    }
}