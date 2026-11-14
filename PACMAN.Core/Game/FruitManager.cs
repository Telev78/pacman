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
        private readonly Random random;

        public FruitManager(int? seed = null)
        {
            random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        private const int ScoreDisplayFrames = 120;   // 2 secondes

        // (X, Y) : coin haut-gauche du sprite 16x16 du fruit
        public bool IsActive { get; protected set; }
        public int X { get; protected set; }
        public int Y { get; protected set; }
        public bool IsScoreVisible { get; protected set; }
        public int Value { get; protected set; } = 100;

        // Nombre de gommes mangées qui font apparaître le fruit
        protected virtual int FirstFruitDots { get { return 70; } }
        protected virtual int SecondFruitDots { get { return 170; } }

        // Le fruit disparaît-il après 9 à 10 secondes ? (Ms Pac-Man : il disparaît en quittant le labyrinthe)
        protected virtual bool ExpiresByTimer { get { return true; } }

        // Déplace le fruit d'une frame (immobile dans Pac-Man)
        protected virtual void MoveFruit(LevelMap map) { }

        // Pac-Man touche-t-il le fruit ?
        protected virtual bool IsTouchedBy(Pacman pacman)
        {
            return pacman.TileX == Tiles.Of(X) && pacman.TileY == Tiles.Of(Y);
        }

        protected Random Rng { get { return random; } }

        /// <summary>Vrai pendant la frame où le fruit rebondit (bruitage de Ms Pac-Man).</summary>
        public bool HasBounced { get; protected set; }

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
        public int Update(int level, int dotsRemaining, Pacman pacman, LevelMap map)
        {
            int pointsEarned = 0;
            HasBounced = false;

            if (IsActive)
            {
                MoveFruit(map);
                fruitTimer--;
                if (!IsActive)
                {
                    // Le fruit a quitté le labyrinthe
                }
                else if (ExpiresByTimer && fruitTimer <= 0)
                {
                    IsActive = false; // Le fruit disparaît après ~10 secondes
                }
                else if (IsTouchedBy(pacman))
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

            if (!spawned70 && dotsEaten >= FirstFruitDots)
            {
                spawned70 = true;
                Spawn(level, map);
            }
            if (!spawned170 && dotsEaten >= SecondFruitDots)
            {
                spawned170 = true;
                Spawn(level, map);
            }

            return pointsEarned;
        }

        protected void StartTimer()
        {
            // Le fruit reste entre 9 et 10 secondes, durée variable et non reproductible
            fruitTimer = random.Next(FruitMinFrames, FruitMaxFrames + 1);
        }

        protected virtual void Spawn(int level, LevelMap map)
        {
            IsActive = true;
            StartTimer();

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

        public void SaveProgress(PlayerState player)
        {
            player.TotalDots = totalDots;
            player.FruitSpawned70 = spawned70;
            player.FruitSpawned170 = spawned170;
        }

        public void LoadProgress(PlayerState player)
        {
            totalDots = player.TotalDots;
            spawned70 = player.FruitSpawned70;
            spawned170 = player.FruitSpawned170;
        }
    }
}