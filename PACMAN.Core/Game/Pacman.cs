using System;

namespace PacmanWinForms
{
    public enum PacmanState { Alive, Dying }

    public class Pacman
    {
        // Pause de Pac-Man quand il mange (en frames à 60 Hz)
        public const int DotStallFrames = 1;
        public const int EnergizerStallFrames = 3;

        // Décalage utilisé pour trouver l'intersection la plus proche de Pac-Man : chaque position le long
        // d'un couloir est alors à 3 pixels avant et 4 après le centre en allant vers la droite ou le bas,
        // et à 4 avant et 3 après en allant vers la gauche ou le haut (cornering de l'arcade).
        // Passer cette valeur à 4 inverse l'asymétrie si le ressenti est décalé d'un pixel.
        private const int CenterRoundingBias = 4;

        public float X { get; set; }
        public float Y { get; set; }
        private float speedAccumulator = 0.0f; // Accumulateur physique
        private int stallFrames = 0;

        // Tuile du CENTRE de Pac-Man
        public int TileX { get { return Tiles.Of(X); } }
        public int TileY { get { return Tiles.Of(Y); } }

        public Direction CurrentDirection { get; set; }
        public Direction NextDirection { get; set; }

        public int AnimFrame { get; private set; } = 0;
        private int animTick = 0;
        private const int TicksPerFrame = 4;
        public PacmanState State { get; set; } = PacmanState.Alive;
        public int DeathAnimationFrame { get; set; } = 0;

        public Pacman()
        {
            X = MazeLayout.PacmanStartX;
            Y = MazeLayout.PacmanStartY;
            CurrentDirection = Direction.Left;
            NextDirection = Direction.None;
        }

        // Immobilise Pac-Man pendant quelques frames (gomme mangée)
        public void Stall(int frames)
        {
            stallFrames = frames;
        }

        public void Move(LevelMap map, LevelSpecs specs, bool isFrightenedMode)
        {
            if (State == PacmanState.Dying) return;

            // Il s'arrête après avoir mangé une gomme
            if (stallFrames > 0)
            {
                stallFrames--;
                return;
            }

            float currentSpeed = isFrightenedMode ? specs.FrightenedPacman : specs.PacmanNormal;

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
            // Une demande dans la direction déjà suivie n'a plus lieu d'être
            if (NextDirection == CurrentDirection)
            {
                NextDirection = Direction.None;
            }

            if (NextDirection != Direction.None)
            {
                if (NextDirection == CurrentDirection.Opposite())
                {
                    // Demi-tour : autorisé n'importe où dans un couloir
                    CurrentDirection = NextDirection;
                    NextDirection = Direction.None;
                }
                else
                {
                    TryCornerTurn(map);
                }
            }

            // Centré sur une tuile : on s'arrête net contre un mur
            if (Tiles.IsAligned(X, Y))
            {
                if (!CanMoveInDirection(TileX, TileY, CurrentDirection, map))
                {
                    return;
                }

                // Passage du tunnel : réapparition de l'autre côté
                X = MazeLayout.WrapTunnelX(X);
            }

            X += CurrentDirection.Dx();
            Y += CurrentDirection.Dy();

            // Après un virage anticipé ou tardif, on se recentre d'un pixel par pas sur l'axe du couloir
            // (ce qui donne le déplacement à 45° du cornering)
            if (CurrentDirection == Direction.Left || CurrentDirection == Direction.Right)
                Y = Recenter(Y);
            else
                X = Recenter(X);

            animTick++;
            if (animTick >= TicksPerFrame)
            {
                animTick = 0;
                AnimFrame = (AnimFrame + 1) % 4;
            }
        }

        // Tente de prendre le virage demandé (à angle droit), avant, pile sur ou après le centre de l'intersection.
        private void TryCornerTurn(LevelMap map)
        {
            bool movingHorizontally = (CurrentDirection == Direction.Left || CurrentDirection == Direction.Right);
            bool turnIsVertical = (NextDirection == Direction.Up || NextDirection == Direction.Down);

            // Il faut un virage à angle droit
            if (movingHorizontally != turnIsVertical) return;

            float along = movingHorizontally ? X : Y;   // Position le long du couloir
            float lane = movingHorizontally ? Y : X;    // Coordonnée perpendiculaire (l'axe du couloir)

            // Pas de nouveau virage tant qu'on n'est pas recentré sur l'axe du couloir
            if (lane % GameConfig.TileSize != 0) return;

            // Intersection la plus proche (de -3 à +4 pixels par rapport à son centre)
            int intersection = (int)Math.Floor((along + CenterRoundingBias) / GameConfig.TileSize);
            int laneTile = (int)(lane / GameConfig.TileSize);

            int tileX = movingHorizontally ? intersection : laneTile;
            int tileY = movingHorizontally ? laneTile : intersection;

            if (CanMoveInDirection(tileX, tileY, NextDirection, map))
            {
                CurrentDirection = NextDirection;
                NextDirection = Direction.None;
            }
        }

        // Ramène d'un pixel une coordonnée perpendiculaire vers l'axe du couloir le plus proche
        private static float Recenter(float p)
        {
            int tile = (int)Math.Floor((p + CenterRoundingBias) / GameConfig.TileSize);
            float offset = p - tile * GameConfig.TileSize;

            if (offset > 0) return p - 1;
            if (offset < 0) return p + 1;
            return p;
        }

        private bool CanMoveInDirection(int tileX, int tileY, Direction dir, LevelMap map)
        {
            if (dir == Direction.None) return false;

            int targetX = tileX + dir.Dx();
            int targetY = tileY + dir.Dy();

            if (targetX < 0 || targetX >= GameConfig.MapWidth || targetY < 0 || targetY >= GameConfig.MapHeight)
            {
                return (map.Definition.IsTunnelRow(targetY) && (dir == Direction.Left || dir == Direction.Right));
            }

            TileType tile = map.Grid[targetY, targetX];
            return tile != TileType.Wall && tile != TileType.GhostDoor;
        }
    }
}