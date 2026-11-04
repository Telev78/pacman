using System.Windows.Forms;

namespace PacmanWinForms
{
    public enum PacmanState { Alive, Dying }

    public class Pacman
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Speed { get; set; } = 1.0f;
        private float speedAccumulator = 0.0f; // Accumulateur physique

        public Keys CurrentDirection { get; set; }
        public Keys NextDirection { get; set; }

        public int AnimFrame { get; private set; } = 0;
        private int animTick = 0;
        private const int TicksPerFrame = 4;
        public PacmanState State { get; set; } = PacmanState.Alive;
        public int DeathAnimationFrame { get; set; } = 0;

        public Pacman()
        {
            X = 104;
            Y = 208;
            CurrentDirection = Keys.Left;
            NextDirection = Keys.None;
        }

        // --- MÉTHODE PRINCIPALE MISE À JOUR ---
        public void Move(LevelMap map, int currentLevel, bool isFrightenedMode)
        {
            if (State == PacmanState.Dying) return;

            // 1. Lire la table de vitesse
            var specs = GameConfig.GetSpecsForLevel(currentLevel);

            // 2. Détecter si Pac-Man se trouve actuellement sur une gomme
            int currentTileX = (int)(X / 8);
            int currentTileY = (int)(Y / 8);
            bool isOnDot = false;

            if (currentTileX >= 0 && currentTileX < GameConfig.MapWidth && currentTileY >= 0 && currentTileY < GameConfig.MapHeight)
            {
                int tile = map.Grid[currentTileY, currentTileX];
                isOnDot = (tile == 2 || tile == 3); // 2 = Gomme, 3 = Super-Gomme
            }

            // 3. Choix de la vitesse exacte selon le tableau A.1
            float currentSpeed = specs.PacmanNormal;
            if (isFrightenedMode)
            {
                currentSpeed = isOnDot ? specs.FrightenedPacmanDots : specs.FrightenedPacman;
            }
            else
            {
                currentSpeed = isOnDot ? specs.PacmanDots : specs.PacmanNormal;
            }

            // 4. Accumulation mécanique
            speedAccumulator += currentSpeed;
            int pixelsToMove = (int)speedAccumulator;
            speedAccumulator -= pixelsToMove;

            // 5. Déplacement pixel par pixel
            for (int i = 0; i < pixelsToMove; i++)
            {
                MoveOnePixel(map);
            }
        }

        // --- DÉPLACEMENT UNITAIRE (1 PIXEL) ---
        private void MoveOnePixel(LevelMap map)
        {
            bool isAlignedWithGrid = (X % 8 == 0) && (Y % 8 == 0);

            if (isAlignedWithGrid)
            {
                int currentTileX = (int)(X / 8);
                int currentTileY = (int)(Y / 8);

                if (NextDirection != Keys.None)
                {
                    if (CanMoveInDirection(currentTileX, currentTileY, NextDirection, map))
                    {
                        CurrentDirection = NextDirection;
                        NextDirection = Keys.None;
                    }
                }

                if (!CanMoveInDirection(currentTileX, currentTileY, CurrentDirection, map))
                {
                    return; // Stoppé net contre un mur
                }

                if (this.X < -8)
                {
                    this.X = 224 + 8;
                }
                else if (this.X > 224 + 8)
                {
                    this.X = -8;
                }
            }
            else
            {
                if (IsOppositeDirection(CurrentDirection, NextDirection))
                {
                    CurrentDirection = NextDirection;
                    NextDirection = Keys.None;
                }
            }

            // Déplacement physique de 1 pixel
            switch (CurrentDirection)
            {
                case Keys.Up: Y -= 1; break;
                case Keys.Down: Y += 1; break;
                case Keys.Left: X -= 1; break;
                case Keys.Right: X += 1; break;
            }

            // Animation
            animTick++;
            if (animTick >= TicksPerFrame)
            {
                animTick = 0;
                AnimFrame = (AnimFrame + 1) % 4;
            }
        }

        private bool CanMoveInDirection(int tileX, int tileY, Keys dir, LevelMap map)
        {
            int targetX = tileX;
            int targetY = tileY;

            switch (dir)
            {
                case Keys.Up: targetY--; break;
                case Keys.Down: targetY++; break;
                case Keys.Left: targetX--; break;
                case Keys.Right: targetX++; break;
                default: return false;
            }

            if (targetX < 0 || targetX >= 28 || targetY < 0 || targetY >= 36)
            {
                return (targetY == 17 && (dir == Keys.Left || dir == Keys.Right));
            }

            return map.Grid[targetY, targetX] != 1 && map.Grid[targetY, targetX] != 4;
        }

        private bool IsOppositeDirection(Keys dir1, Keys dir2)
        {
            if (dir1 == Keys.Left && dir2 == Keys.Right) return true;
            if (dir1 == Keys.Right && dir2 == Keys.Left) return true;
            if (dir1 == Keys.Up && dir2 == Keys.Down) return true;
            if (dir1 == Keys.Down && dir2 == Keys.Up) return true;
            return false;
        }
    }
}