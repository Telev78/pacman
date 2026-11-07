using System;

namespace PacmanWinForms
{
    /// <summary>
    /// Fruit bonus : apparition (70 et 170 gommes mangées), durée, collision et affichage du score.
    /// </summary>
    public class FruitManager
    {
        private const int FruitMinFrames = 540;   // 9 secondes à 60 Hz
        private const int FruitMaxFrames = 600;   // 10 secondes
        private readonly Random random = new Random();
        private const int ScoreDisplayFrames = 120;   // 2 secondes

        public bool IsActive { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public bool IsScoreVisible { get; private set; }
        public int Value { get; private set; } = 100;

        private int fruitTimer = 0;
        private int scoreTimer = 0;
        private int totalDots = 0;
        private bool spawned70 = false;
        private bool spawned170 = false;

        // Nouveau niveau : remet à zéro le suivi des seuils d'apparition
        public void StartLevel(int totalDots)
        {
            this.totalDots = totalDots;
            spawned70 = false;
            spawned170 = false;
        }

        // Nouvelle vie : le fruit visible disparaît, mais les seuils déjà passés ne se réinitialisent pas
        public void ResetForNewLife()
        {
            IsActive = false;
            IsScoreVisible = false;
        }

        // À appeler à chaque frame de jeu. Renvoie les points gagnés si Pac-Man mange le fruit, sinon 0.
        public int Update(int level, int dotsRemaining, Pacman pacman)
        {
            int pointsEarned = 0;

            if (IsActive)
            {
                fruitTimer--;
                if (fruitTimer <= 0)
                {
                    IsActive = false; // Le fruit disparaît après ~10 secondes
                }
                else if (pacman.TileX == Tiles.Of(X) && pacman.TileY == Tiles.Of(Y))
                {
                    IsActive = false;
                    IsScoreVisible = true;
                    scoreTimer = ScoreDisplayFrames;
                    pointsEarned = Value;
                }
            }

            if (IsScoreVisible)
            {
                scoreTimer--;
                if (scoreTimer <= 0)
                {
                    IsScoreVisible = false;
                }
            }

            int dotsEaten = totalDots - dotsRemaining;

            if (!spawned70 && dotsEaten >= 70)
            {
                spawned70 = true;
                Spawn(level);
            }
            if (!spawned170 && dotsEaten >= 170)
            {
                spawned170 = true;
                Spawn(level);
            }

            return pointsEarned;
        }

        private void Spawn(int level)
        {
            IsActive = true;
            // Le fruit reste entre 9 et 10 secondes, durée variable et non reproductible
            fruitTimer = random.Next(FruitMinFrames, FruitMaxFrames + 1);

            // Alignement sous le nid (même colonne que le spawn de Pac-Man, ligne 20)
            X = 13 * GameConfig.TileSize;
            Y = 20 * GameConfig.TileSize - 4;

            Value = GetValueForLevel(level);
        }

        private static int GetValueForLevel(int level)
        {
            if (level == 1) return 100;
            if (level == 2) return 300;
            if (level == 3 || level == 4) return 500;
            if (level == 5 || level == 6) return 700;
            if (level == 7 || level == 8) return 1000;
            if (level == 9 || level == 10) return 2000;
            if (level == 11 || level == 12) return 3000;
            return 5000; // Niveau 13+ (Clé)
        }
    }
}