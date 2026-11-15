using System;
using System.Drawing;

namespace PacmanWinForms
{
    public abstract class Ghost
    {
        public float X { get; protected set; }
        public float Y { get; protected set; }
        private float speedAccumulator = 0.0f; // Accumulateur pour la gestion physique fine

        // Identité du fantôme (sprites, ordre de sortie du nid)
        public abstract GhostType Type { get; }

        public Direction CurrentDirection { get; protected set; }

        // Cycle de vie : dans le nid / en sortie / actif dans le labyrinthe / yeux
        public GhostPhase Phase { get; set; } = GhostPhase.Active;

        // Mode de vague (Scatter / Chase), significatif quand le fantôme est Active
        public GhostMode Mode { get; set; } = GhostMode.Scatter;

        // Vulnérable (après une super-gomme). Peut être vrai même dans le nid ou à la sortie.
        public bool IsFrightened { get; set; } = false;

        /// <summary>Ms Pac-Man : en Scatter, le fantôme erre au hasard au lieu de rejoindre son coin.</summary>
        public bool WandersInScatter { get; set; }

        /// <summary>Ms Pac-Man : en sortant du nid, le fantôme choisit gauche/droite selon sa cible (au lieu de toujours partir à gauche).</summary>
        public bool ExitsTowardTarget { get; set; }

        // Le fantôme actif choisit-il ses virages au hasard (comme un fantôme effrayé) ?
        protected virtual bool ShouldWander()
        {
            return WandersInScatter && Phase == GhostPhase.Active && Mode == GhostMode.Scatter;
        }

        protected float initialY;
        protected int bobDirection = 1;
        protected Point ScatterTargetTile { get; set; }
        private bool reverseRequested = false;

        // Graine fixe : les fantômes effrayés suivent les mêmes parcours à chaque niveau et à chaque vie
        private const int FrightenedSeed = 1;
        private static Random rand = new Random(FrightenedSeed);

        // À appeler au début de chaque niveau et après chaque vie perdue
        public static void ResetRandom()
        {
            rand = new Random(FrightenedSeed);
        }

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

        public virtual void Move(LevelMap map, Pacman pacman, LevelSpecs specs, GhostMode globalMode)
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
            else
            {
                if (IsFrightened)
                {
                    currentSpeed = specs.FrightenedGhost; // Ralentissement mode panique
                }

                // Le tunnel ralentit tous les fantômes, y compris les fantômes effrayés
                if (map.Definition.IsInTunnelSlowZone(currentTileX, currentTileY))
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
                    // Direction de sortie : vers la gauche, ou vers la droite si le mode a changé pendant qu'il était dans le nid
                    if (ExitsTowardTarget && !IsFrightened)
                    {
                        CurrentDirection = ChooseExitDirection(pacman);
                    }
                    else
                    {
                        CurrentDirection = reverseRequested ? Direction.Right : Direction.Left;
                    }
                    reverseRequested = false;
                }
                return;
            }

            // Sommes-nous alignés au pixel près sur le centre d'une tuile ?
            if (Tiles.IsAligned(X, Y))
            {
                // Passage du tunnel : réapparition de l'autre côté
                X = MazeLayout.WrapTunnelX(X);

                int currentTileX = TileX;
                int currentTileY = TileY;

                bool isActive = Phase == GhostPhase.Active;

                if (reverseRequested && isActive)
                {
                    CurrentDirection = CurrentDirection.Opposite();
                    reverseRequested = false;
                }
                else
                {
                    reverseRequested = false;

                    if (isActive && (IsFrightened || ShouldWander()))
                        CurrentDirection = ChooseFrightenedDirection(map);
                    else
                        CurrentDirection = ChooseNextDirection(map, pacman, currentTileX, currentTileY);
                }
            }

            // Déplacement physique d'un pixel strict
            X += CurrentDirection.Dx();
            Y += CurrentDirection.Dy();
        }

        // Sortie du nid (Ms Pac-Man) : au hasard si le fantôme erre, sinon la case (gauche ou droite) la plus proche de sa cible
        private Direction ChooseExitDirection(Pacman pacman)
        {
            if (ShouldWander()) return rand.Next(0, 2) == 0 ? Direction.Left : Direction.Right;

            Point target = GetCurrentTargetTile(pacman);
            int row = MazeLayout.HouseExitY / GameConfig.TileSize;
            int leftX = MazeLayout.HouseExitX / GameConfig.TileSize;   // 13
            int rightX = leftX + 1;                                     // 14

            int dl = (leftX - target.X) * (leftX - target.X) + (row - target.Y) * (row - target.Y);
            int dr = (rightX - target.X) * (rightX - target.X) + (row - target.Y) * (row - target.Y);
            return dr < dl ? Direction.Right : Direction.Left;          // Égalité : gauche (priorité arcade)
        }

        private Direction ChooseNextDirection(LevelMap map, Pacman pacman, int currentTileX, int currentTileY)
        {
            Point targetTile = GetCurrentTargetTile(pacman);

            // L'ordre est important : il départage les égalités (priorité arcade : Haut, Gauche, Bas, Droite)
            Direction[] directionsToTest = { Direction.Up, Direction.Left, Direction.Down, Direction.Right };

            Direction bestDirection = CurrentDirection;
            int shortestDistance = int.MaxValue;
            
            // Aux intersections interdites aux virages vers le haut, les fantômes en Scatter ou Chase ne peuvent pas monter
            bool upForbidden = Phase == GhostPhase.Active && map.Definition.IsNoUpTile(currentTileX, currentTileY);

            foreach (Direction dir in directionsToTest)
            {
                if (dir == CurrentDirection.Opposite())
                    continue;

                if (dir == Direction.Up && upForbidden)
                    continue;

                int nextX = currentTileX + dir.Dx();
                int nextY = currentTileY + dir.Dy();

                if (nextX >= 0 && nextX < GameConfig.MapWidth && nextY >= 0 && nextY < GameConfig.MapHeight)
                {
                    TileType tileType = map.Grid[nextY, nextX];

                    if (tileType != TileType.Wall && (tileType != TileType.GhostDoor || Phase == GhostPhase.Eaten))
                    {
                        // Distance au carré (inutile de calculer la racine pour comparer)
                        int dx = nextX - targetTile.X;
                        int dy = nextY - targetTile.Y;
                        int distance = dx * dx + dy * dy;

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

        // Demande un demi-tour (changement de mode ou super-gomme).
        // Fantôme actif : demi-tour à la prochaine tuile.
        // Dans le nid ou en sortie : la demande est gardée, et le fantôme sortira vers la droite au lieu de la gauche.
        public void RequestReverse()
        {
            if (Phase == GhostPhase.Active || Phase == GhostPhase.InHouse || Phase == GhostPhase.Exiting)
            {
                reverseRequested = true;
            }
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
                    if (map.Definition.IsTunnelRow(targetY) && (testDir == Direction.Left || testDir == Direction.Right))
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