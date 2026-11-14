using System.Drawing;

namespace PacmanWinForms
{
    /// <summary>
    /// Fruit de Ms Pac-Man : il entre par un tunnel, se promène au hasard dans le labyrinthe (sans jamais faire
    /// demi-tour), puis rejoint un tunnel et quitte l'écran. Il apparaît après 64 et 176 gommes mangées.
    /// Sept fruits aux niveaux 1 à 7, puis un fruit tiré au hasard (mais fixe pour un niveau donné).
    /// </summary>
    public sealed class MsFruitManager : FruitManager
    {
        private static readonly int[] FruitValues = { 100, 200, 500, 700, 1000, 2000, 5000 };

        private const float FruitSpeed = 0.6f;             // pixels par frame
        private const int MinWalkFrames = 420;             // 7 secondes de promenade au minimum
        private const int MaxWalkFrames = 600;             // 10 secondes au maximum
        private const int TileSize = GameConfig.TileSize;
        private const int BounceFrames = 16;

        // Position logique (coin haut-gauche de la boîte 8x8, comme les fantômes)
        private float fx, fy;
        private Direction direction = Direction.Left;
        private float accumulator;
        private int bounceFrames;
        private int walkFramesLeft;
        private Point exitTile;

        public MsFruitManager(int? seed = null) : base(seed)
        {
        }

        /// <summary>Indice du fruit (0 cerise ... 6 banane) d'un niveau.</summary>
        public static int FruitIndexForLevel(int level)
        {
            if (level <= 7) return System.Math.Max(0, level - 1);

            // Tirage "au hasard" mais identique à chaque partie (l'indicateur de niveau reste cohérent)
            unchecked
            {
                uint h = (uint)level * 2654435761u;
                h ^= h >> 15;
                return (int)((h >> 4) % 7u);
            }
        }

        protected override int FirstFruitDots { get { return 64; } }
        protected override int SecondFruitDots { get { return 176; } }
        protected override bool ExpiresByTimer { get { return false; } }

        protected override bool IsTouchedBy(Pacman pacman)
        {
            return pacman.TileX == Tiles.Of(fx) && pacman.TileY == Tiles.Of(fy);
        }

        protected override void Spawn(int level, LevelMap map)
        {
            int[] rows = map.Definition.TunnelRows;
            if (rows.Length == 0) return;   // Pas de tunnel : pas de fruit

            int row = rows[Rng.Next(rows.Length)];
            bool fromLeft = Rng.Next(2) == 0;

            fy = row * TileSize;
            fx = fromLeft ? -2 * TileSize : (GameConfig.MapWidth + 1) * TileSize;
            direction = fromLeft ? Direction.Right : Direction.Left;
            accumulator = 0;
            bounceFrames = 0;
            walkFramesLeft = Rng.Next(MinWalkFrames, MaxWalkFrames + 1);
            exitTile = new Point(-1, row);

            Value = FruitValues[FruitIndexForLevel(level)];
            IsActive = true;
            Sync();
        }

        protected override void MoveFruit(LevelMap map)
        {
            accumulator += FruitSpeed;
            int pixels = (int)accumulator;
            accumulator -= pixels;

            for (int i = 0; i < pixels && IsActive; i++)
            {
                if (walkFramesLeft > 0) walkFramesLeft--;
                if (walkFramesLeft == 0) { ChooseExit(map); walkFramesLeft = -1; }

                if (Tiles.IsAligned(fx, fy)) Turn(map);

                fx += direction.Dx();
                fy += direction.Dy();

                // Sorti par un bord de l'écran
                if (direction == Direction.Left && fx <= -2 * TileSize) IsActive = false;
                if (direction == Direction.Right && fx >= (GameConfig.MapWidth + 1) * TileSize) IsActive = false;
            }

            // Rebond du fruit toutes les 16 frames (période mesurée sur la vidéo de l'arcade : 0,267 s)
            bounceFrames++;
            if (IsActive && bounceFrames % BounceFrames == 0) HasBounced = true;

            Sync();
        }

        // Le sprite de 16x16 est centré sur la boîte logique
        private void Sync()
        {
            X = (int)fx - 4;
            Y = (int)fy - 4;
        }

        // Après la promenade : viser le tunnel le plus proche
        private void ChooseExit(LevelMap map)
        {
            int best = int.MaxValue;
            foreach (int row in map.Definition.TunnelRows)
            {
                foreach (int col in new[] { -1, GameConfig.MapWidth })
                {
                    int dx = col - Tiles.Of(fx);
                    int dy = row - Tiles.Of(fy);
                    int d = dx * dx + dy * dy;
                    if (d < best) { best = d; exitTile = new Point(col, row); }
                }
            }
        }

        private void Turn(LevelMap map)
        {
            int tx = Tiles.Of(fx);
            int ty = Tiles.Of(fy);

            // Dans le tunnel hors du labyrinthe : tout droit
            if (tx < 0 || tx >= GameConfig.MapWidth) return;

            Direction[] order = { Direction.Up, Direction.Left, Direction.Down, Direction.Right };
            Direction[] options = new Direction[4];
            int count = 0;

            foreach (Direction dir in order)
            {
                if (dir == direction.Opposite()) continue;
                if (CanEnter(map, tx + dir.Dx(), ty + dir.Dy(), dir)) options[count++] = dir;
            }

            if (count == 0)
            {
                // Impasse : demi-tour
                direction = direction.Opposite();
                return;
            }

            if (walkFramesLeft > 0)
            {
                direction = options[Rng.Next(count)];
                return;
            }

            // Retour vers la sortie : la direction qui rapproche le plus de la case cible
            int best = int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                int dx = tx + options[i].Dx() - exitTile.X;
                int dy = ty + options[i].Dy() - exitTile.Y;
                int d = dx * dx + dy * dy;
                if (d < best) { best = d; direction = options[i]; }
            }
        }

        private static bool CanEnter(LevelMap map, int x, int y, Direction dir)
        {
            if (x < 0 || x >= GameConfig.MapWidth || y < 0 || y >= GameConfig.MapHeight)
            {
                // Seul le tunnel mène hors du labyrinthe
                return map.Definition.IsTunnelRow(y) && (dir == Direction.Left || dir == Direction.Right);
            }

            TileType tile = map.Grid[y, x];
            return tile != TileType.Wall && tile != TileType.GhostDoor;
        }
    }
}
