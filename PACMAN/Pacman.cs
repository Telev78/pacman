namespace PacmanWinForms
{
    public enum PacmanState { Alive, Dying }

    public class Pacman
    {
        public float X { get; set; }
        public float Y { get; set; }
        // Tuile du CENTRE de Pac-Man
        public int TileX { get { return Tiles.Of(X); } }
        public int TileY { get { return Tiles.Of(Y); } }
        private float speedAccumulator = 0.0f; // Accumulateur physique

        public Direction CurrentDirection { get; set; }
        public Direction NextDirection { get; set; }

        public int AnimFrame { get; private set; } = 0;
        private int animTick = 0;
        private const int TicksPerFrame = 4;
        public PacmanState State { get; set; } = PacmanState.Alive;
        public int DeathAnimationFrame { get; set; } = 0;

        public Pacman()
        {
            X = 104;
            Y = 208;
            CurrentDirection = Direction.Left;
            NextDirection = Direction.None;
        }

        public void Move(LevelMap map, int currentLevel, bool isFrightenedMode)
        {
            if (State == PacmanState.Dying) return;

            var specs = GameConfig.GetSpecsForLevel(currentLevel);

            // Pac-Man est-il sur une gomme ?
            int currentTileX = TileX;
            int currentTileY = TileY;
            bool isOnDot = false;

            if (currentTileX >= 0 && currentTileX < GameConfig.MapWidth && currentTileY >= 0 && currentTileY < GameConfig.MapHeight)
            {
                TileType tile = map.Grid[currentTileY, currentTileX];
                isOnDot = (tile == TileType.Dot || tile == TileType.Energizer);
            }

            float currentSpeed;
            if (isFrightenedMode)
                currentSpeed = isOnDot ? specs.FrightenedPacmanDots : specs.FrightenedPacman;
            else
                currentSpeed = isOnDot ? specs.PacmanDots : specs.PacmanNormal;

            speedAccumulator += currentSpeed;
            int pixelsToMove = (int)speedAccumulator;
            speedAccumulator -= pixelsToMove;

            for (int i = 0; i < pixelsToMove; i++)
            {
                MoveOnePixel(map);
            }
        }

        private void MoveOnePixel(LevelMap map)
        {
            bool isAlignedWithGrid = Tiles.IsAligned(X, Y);

            if (isAlignedWithGrid)
            {
                int currentTileX = TileX;
                int currentTileY = TileY;

                if (NextDirection != Direction.None)
                {
                    if (CanMoveInDirection(currentTileX, currentTileY, NextDirection, map))
                    {
                        CurrentDirection = NextDirection;
                        NextDirection = Direction.None;
                    }
                }

                if (!CanMoveInDirection(currentTileX, currentTileY, CurrentDirection, map))
                {
                    return; // Stoppé net contre un mur
                }

                // Tunnel
                int wrapLimit = GameConfig.MapWidth * GameConfig.TileSize + GameConfig.TileSize;
                if (this.X < -GameConfig.TileSize)
                {
                    this.X = wrapLimit;
                }
                else if (this.X > wrapLimit)
                {
                    this.X = -GameConfig.TileSize;
                }
            }
            else
            {
                // Demi-tour autorisé n'importe où entre deux tuiles
                if (CurrentDirection != Direction.None && CurrentDirection.Opposite() == NextDirection)
                {
                    CurrentDirection = NextDirection;
                    NextDirection = Direction.None;
                }
            }

            X += CurrentDirection.Dx();
            Y += CurrentDirection.Dy();

            animTick++;
            if (animTick >= TicksPerFrame)
            {
                animTick = 0;
                AnimFrame = (AnimFrame + 1) % 4;
            }
        }

        private bool CanMoveInDirection(int tileX, int tileY, Direction dir, LevelMap map)
        {
            if (dir == Direction.None) return false;

            int targetX = tileX + dir.Dx();
            int targetY = tileY + dir.Dy();

            if (targetX < 0 || targetX >= GameConfig.MapWidth || targetY < 0 || targetY >= GameConfig.MapHeight)
            {
                return (targetY == MazeLayout.TunnelRow && (dir == Direction.Left || dir == Direction.Right));
            }

            TileType tile = map.Grid[targetY, targetX];
            return tile != TileType.Wall && tile != TileType.GhostDoor;
        }
    }
}