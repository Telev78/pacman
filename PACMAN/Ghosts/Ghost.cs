using System;
using System.Drawing;

namespace PacmanWinForms
{
    public abstract class Ghost
    {
        public float X { get; protected set; }
        public float Y { get; protected set; }
        private float speedAccumulator = 0.0f; // Accumulateur pour la gestion physique fine

        public Direction CurrentDirection { get; protected set; }

        // Cycle de vie : dans le nid / en sortie / actif dans le labyrinthe / yeux
        public GhostPhase Phase { get; set; } = GhostPhase.Active;

        // Mode de vague (Scatter / Chase), significatif quand le fantôme est Active
        public GhostMode Mode { get; set; } = GhostMode.Scatter;

        // Vulnérable (après une super-gomme). Peut être vrai même dans le nid ou à la sortie.
        public bool IsFrightened { get; set; } = false;

        protected float initialY;
        protected int bobDirection = 1;
        protected Point ScatterTargetTile { get; set; }
        public int DotLimit { get; set; }
        public int DotCounter { get; set; } = 0;
        private bool reverseRequested = false;
        private static readonly Random rand = new Random();

        // Tuile du CENTRE du fantôme
        public int TileX { get { return Tiles.Of(X); } }
        public int TileY { get { return Tiles.Of(Y); } }

        public Ghost(int startTileX, int startTileY, Point scatterTarget)
        {
            this.X = startTileX * GameConfig.TileSize;
            this.Y = startTileY * GameConfig.TileSize;
            this.initialY = this.Y;
            this.ScatterTargetTile = scatterTarget;
            this.CurrentDirection = Direction.Left;
        }

        public virtual void Move(LevelMap map, Pacman pacman, int currentLevel, GhostMode globalMode)
        {
            int currentTileX = TileX;
            int currentTileY = TileY;

            // Résurrection : les yeux atteignent la porte du nid
            if (Phase == GhostPhase.Eaten
                && currentTileY == MazeLayout.HouseEntranceRow
                && (currentTileX == 13 || currentTileX == 14))
            {
                this.Phase = GhostPhase.Exiting;
                this.Y = MazeLayout.HouseInsideY;
                this.X = MazeLayout.HouseExitX;
                return;
            }

            var specs = GameConfig.GetSpecsForLevel(currentLevel);
            float currentSpeed = GetNormalSpeed(specs);

            if (Phase == GhostPhase.InHouse)
            {
                currentSpeed = specs.GhostNormal * 0.5f; // Danse ralentie dans le nid
            }
            else if (Phase == GhostPhase.Exiting)
            {
                currentSpeed = specs.GhostNormal * 0.4f; // Vitesse de transition pour sortir
            }
            else if (Phase == GhostPhase.Eaten)
            {
                currentSpeed = 2.0f; // Les yeux foncent au nid à 200 % de vitesse
            }
            else if (IsFrightened)
            {
                currentSpeed = specs.FrightenedGhost; // Ralentissement mode panique
            }
            else
            {
                // Tunnel de l'arcade (ligne 17, colonnes extérieures)
                if (currentTileY == MazeLayout.TunnelRow
                    && (currentTileX <= MazeLayout.TunnelSlowLeftMaxColumn || currentTileX >= MazeLayout.TunnelSlowRightMinColumn))
                {
                    currentSpeed = specs.GhostTunnel;
                }
            }

            speedAccumulator += currentSpeed;
            int pixelsToMove = (int)speedAccumulator;
            speedAccumulator -= pixelsToMove;

            for (int i = 0; i < pixelsToMove; i++)
            {
                MoveOnePixel(map, pacman, globalMode);
            }
        }

        // --- DÉPLACEMENT UNITAIRE (1 PIXEL) ---
        private void MoveOnePixel(LevelMap map, Pacman pacman, GhostMode globalMode)
        {
            // --- ROUTINE DU NID ---
            if (Phase == GhostPhase.InHouse)
            {
                Y += bobDirection;
                int bobRange = 4;

                if (Y >= initialY + bobRange)
                {
                    Y = initialY + bobRange;
                    bobDirection = -1;
                    CurrentDirection = Direction.Up;
                }
                else if (Y <= initialY - bobRange)
                {
                    Y = initialY - bobRange;
                    bobDirection = 1;
                    CurrentDirection = Direction.Down;
                }
                return;
            }

            // --- CINÉMATIQUE DE SORTIE ---
            if (Phase == GhostPhase.Exiting)
            {
                float targetX = MazeLayout.HouseExitX;
                float targetY = MazeLayout.HouseExitY;

                if (X != targetX)
                {
                    if (X < targetX) { X += 1; CurrentDirection = Direction.Right; }
                    else { X -= 1; CurrentDirection = Direction.Left; }
                }
                else if (Y > targetY)
                {
                    Y -= 1;
                    CurrentDirection = Direction.Up;
                }
                else
                {
                    // Le fantôme franchit le seuil : il devient actif, avec le mode de vague courant.
                    // S'il a été effrayé entre-temps, IsFrightened reste vrai.
                    Phase = GhostPhase.Active;
                    Mode = globalMode;
                    CurrentDirection = Direction.Left;
                }
                return;
            }

            // Sommes-nous alignés au pixel près sur le centre d'une tuile ?
            if (Tiles.IsAligned(X, Y))
            {
                int currentTileX = TileX;
                int currentTileY = TileY;

                HandleTunnelTeleport(currentTileX);

                bool isActive = Phase == GhostPhase.Active;

                if (reverseRequested && isActive)
                {
                    CurrentDirection = CurrentDirection.Opposite();
                    reverseRequested = false;
                }
                else
                {
                    reverseRequested = false;

                    if (isActive && IsFrightened)
                        CurrentDirection = ChooseFrightenedDirection(map);
                    else
                        CurrentDirection = ChooseNextDirection(map, pacman, currentTileX, currentTileY);
                }
            }

            // Déplacement physique d'un pixel strict
            X += CurrentDirection.Dx();
            Y += CurrentDirection.Dy();
        }

        private Direction ChooseNextDirection(LevelMap map, Pacman pacman, int currentTileX, int currentTileY)
        {
            Point targetTile = GetCurrentTargetTile(pacman);

            // L'ordre est important : il départage les égalités (priorité arcade : Haut, Gauche, Bas, Droite)
            Direction[] directionsToTest = { Direction.Up, Direction.Left, Direction.Down, Direction.Right };

            Direction bestDirection = CurrentDirection;
            double shortestDistance = double.MaxValue;

            foreach (Direction dir in directionsToTest)
            {
                if (dir == CurrentDirection.Opposite())
                    continue;

                int nextX = currentTileX + dir.Dx();
                int nextY = currentTileY + dir.Dy();

                if (nextX >= 0 && nextX < GameConfig.MapWidth && nextY >= 0 && nextY < GameConfig.MapHeight)
                {
                    TileType tileType = map.Grid[nextY, nextX];

                    if (tileType != TileType.Wall && (tileType != TileType.GhostDoor || Phase == GhostPhase.Eaten))
                    {
                        double distance = Math.Pow(nextX - targetTile.X, 2) + Math.Pow(nextY - targetTile.Y, 2);

                        if (distance < shortestDistance)
                        {
                            shortestDistance = distance;
                            bestDirection = dir;
                        }
                    }
                }
            }

            return bestDirection;
        }

        // Vitesse "normale" du fantôme (hors nid, yeux, effrayé, tunnel). Blinky la surcharge pour Elroy.
        protected virtual float GetNormalSpeed(LevelSpecs specs)
        {
            return specs.GhostNormal;
        }

        protected virtual Point GetCurrentTargetTile(Pacman pacman)
        {
            if (Phase == GhostPhase.Active)
            {
                return (Mode == GhostMode.Scatter) ? ScatterTargetTile : GetChaseTargetTile(pacman);
            }

            // Yeux : retour vers l'entrée du nid
            return MazeLayout.EyesTargetTile;
        }

        public void ForceDemiTour()
        {
            // Seuls les fantômes actifs dans le labyrinthe sont concernés.
            // Le demi-tour n'est effectif qu'au prochain centre de tuile.
            if (Phase == GhostPhase.Active)
            {
                reverseRequested = true;
            }
        }

        private void HandleTunnelTeleport(int currentTileX)
        {
            if (currentTileX < 0 && CurrentDirection == Direction.Left)
                X = (GameConfig.MapWidth - 1) * GameConfig.TileSize;
            else if (currentTileX >= GameConfig.MapWidth && CurrentDirection == Direction.Right)
                X = 0;
        }

        private Direction ChooseFrightenedDirection(LevelMap map)
        {
            // Ordre horaire officiel de l'arcade : Nord, Est, Sud, Ouest
            Direction[] clockwiseOrder = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };

            // Le PRNG choisit la direction de départ, puis on teste dans le sens horaire
            int startIndex = rand.Next(0, 4);

            int currentTileX = TileX;
            int currentTileY = TileY;

            for (int i = 0; i < 4; i++)
            {
                Direction testDir = clockwiseOrder[(startIndex + i) % 4];

                // Le demi-tour est interdit à une intersection
                if (testDir == CurrentDirection.Opposite())
                    continue;

                int targetX = currentTileX + testDir.Dx();
                int targetY = currentTileY + testDir.Dy();

                // Hors limites : seul le tunnel est valide
                if (targetX < 0 || targetX >= GameConfig.MapWidth || targetY < 0 || targetY >= GameConfig.MapHeight)
                {
                    if (targetY == MazeLayout.TunnelRow && (testDir == Direction.Left || testDir == Direction.Right))
                        return testDir;
                    continue;
                }

                TileType tile = map.Grid[targetY, targetX];
                if (tile != TileType.Wall && tile != TileType.GhostDoor)
                {
                    return testDir;
                }
            }

            // Sécurité : fantôme coincé (ne devrait jamais arriver)
            return CurrentDirection;
        }

        protected abstract Point GetChaseTargetTile(Pacman pacman);
    }
}