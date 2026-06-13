using System;
using System.Drawing;
using System.Windows.Forms;

namespace PacmanWinForms
{
    
    public abstract class Ghost
    {
        public float X { get; protected set; }
        public float Y { get; protected set; }
        public float Speed { get; set; } = 1.0f;
        private float speedAccumulator = 0.0f; // Accumulateur pour la gestion physique fine

        public Keys CurrentDirection { get; protected set; }
        public GhostState CurrentState { get; set; } = GhostState.Scatter;
        protected float initialY;
        protected int bobDirection = 1;
        protected Point ScatterTargetTile { get; set; }
        public bool IsFrightened { get; set; } = false;
        public int DotLimit { get; set; }
        public int DotCounter { get; set; } = 0;

        public Ghost(int startTileX, int startTileY, Point scatterTarget)
        {
            // On repasse sur un calcul de grille brut et neutre
            this.X = startTileX * GameConfig.TileSize;
            this.Y = startTileY * GameConfig.TileSize;
            this.initialY = this.Y;
            this.ScatterTargetTile = scatterTarget;
            this.CurrentDirection = Keys.Left;
        }

        // --- MÉTHODE PRINCIPALE MISE À JOUR ---
        public virtual void Move(LevelMap map, Pacman pacman, int currentLevel)
        {
            int currentTileX = (int)(X / GameConfig.TileSize);
            int currentTileY = (int)(Y / GameConfig.TileSize);

            // ÉTAPE DE RÉSURRECTION : Si les yeux atteignent la porte du nid (Ligne 14, colonnes 13 ou 14)
            if (CurrentState == GhostState.Eaten && currentTileY == 14 && (currentTileX == 13 || currentTileX == 14))
            {
                // 1. On le passe en mode "Sortie du nid" (il reprend sa couleur normale)
                this.CurrentState = GhostState.Exiting;

                // 2. On le repositionne proprement à l'intérieur du nid (Ligne 17) pour une transition fluide
                this.Y = 17 * GameConfig.TileSize;
                this.X = 14 * GameConfig.TileSize - 4; // Centré au milieu de la porte

                return; // On stoppe l'I.A des yeux pour ce tick afin d'éviter les conflits
            }

            // 1. Récupération des spécifications de vitesse du niveau
            var specs = GameConfig.GetSpecsForLevel(currentLevel);
            float currentSpeed = specs.GhostNormal;

            // 2. Ajustement de la vitesse selon l'état du fantôme
            if (CurrentState == GhostState.InHouse)
            {
                currentSpeed = specs.GhostNormal * 0.5f; // Danse ralentie dans le nid
            }
            else if (CurrentState == GhostState.Exiting)
            {
                currentSpeed = specs.GhostNormal * 0.4f; // Vitesse de transition pour sortir
            }
            else if (CurrentState == GhostState.Frightened)
            {
                currentSpeed = specs.FrightenedGhost;   // Ralentissement mode panique
            }
            else if (CurrentState == GhostState.Eaten)
            {
                currentSpeed = 2.0f; // Les yeux morts foncent au nid à 200% de vitesse (2px/frame)
            }
            else
            {
                // Vérification du tunnel de l'arcade (Ligne 17, colonnes extérieures)
                if (currentTileY == 17 && (currentTileX < 6 || currentTileX > 21))
                {
                    currentSpeed = specs.GhostTunnel;
                }
            }

            // 3. Accumulation de la vitesse fractionnaire
            speedAccumulator += currentSpeed;
            int pixelsToMove = (int)speedAccumulator;
            speedAccumulator -= pixelsToMove;

            // 4. Déplacement physique d'exactement 1 pixel par itération
            for (int i = 0; i < pixelsToMove; i++)
            {
                MoveOnePixel(map, pacman);
            }
        }

        // --- DÉPLACEMENT UNITAIRE (1 PIXEL) ---
        private void MoveOnePixel(LevelMap map, Pacman pacman)
        {
            // --- ROUTINE DU NID ---
            if (CurrentState == GhostState.InHouse)
            {
                Y += bobDirection;
                int bobRange = 4;

                if (Y >= initialY + bobRange)
                {
                    Y = initialY + bobRange;
                    bobDirection = -1;
                    CurrentDirection = Keys.Up;
                }
                else if (Y <= initialY - bobRange)
                {
                    Y = initialY - bobRange;
                    bobDirection = 1;
                    CurrentDirection = Keys.Down;
                }
                return;
            }

            // --- CINÉMATIQUE DE SORTIE ---
            if (CurrentState == GhostState.Exiting)
            {
                float targetX = 14 * GameConfig.TileSize - 4;
                float targetY = 14 * GameConfig.TileSize;

                if (X != targetX)
                {
                    if (X < targetX) { X += 1; CurrentDirection = Keys.Right; }
                    else { X -= 1; CurrentDirection = Keys.Left; }
                }
                else if (Y > targetY)
                {
                    Y -= 1;
                    CurrentDirection = Keys.Up;
                }
                else
                {
                    // Le fantôme vient de franchir le seuil de la maison !
                    if (IsFrightened)
                    {
                        // S'il a été terrifié pendant qu'il était dedans ou qu'il sortait,
                        // il bascule immédiatement dans le comportement de fuite du labyrinthe
                        CurrentState = GhostState.Frightened;
                    }
                    else
                    {
                        // Sinon, il reprend sa routine normale (ici Scatter par défaut)
                        CurrentState = GhostState.Scatter;
                    }

                    CurrentDirection = Keys.Left;
                }
                return;
            }

            // 1. Sommes-nous alignés au pixel près sur le centre d'une tuile ?
            if ((X % GameConfig.TileSize == 0) && (Y % GameConfig.TileSize == 0))
            {
                int currentTileX = (int)(X / GameConfig.TileSize);
                int currentTileY = (int)(Y / GameConfig.TileSize);

                HandleTunnelTeleport(currentTileX);
                CurrentDirection = ChooseNextDirection(map, pacman, currentTileX, currentTileY);
            }

            // 2. Déplacement physique d'un pixel strict
            switch (CurrentDirection)
            {
                case Keys.Up: Y -= 1; break;
                case Keys.Down: Y += 1; break;
                case Keys.Left: X -= 1; break;
                case Keys.Right: X += 1; break;
            }
        }

        private Keys ChooseNextDirection(LevelMap map, Pacman pacman, int currentTileX, int currentTileY)
        {
            Point targetTile = GetCurrentTargetTile(pacman);
            Keys[] directionsToTest = { Keys.Up, Keys.Left, Keys.Down, Keys.Right };

            Keys bestDirection = CurrentDirection;
            double shortestDistance = double.MaxValue;

            foreach (Keys dir in directionsToTest)
            {
                if (dir == GetOppositeDirection(CurrentDirection))
                    continue;

                int nextX = currentTileX;
                int nextY = currentTileY;

                switch (dir)
                {
                    case Keys.Up: nextY--; break;
                    case Keys.Down: nextY++; break;
                    case Keys.Left: nextX--; break;
                    case Keys.Right: nextX++; break;
                }

                if (nextX >= 0 && nextX < GameConfig.MapWidth && nextY >= 0 && nextY < GameConfig.MapHeight)
                {
                    int tileType = map.Grid[nextY, nextX];

                    if (tileType != 1 && (tileType != 4 || CurrentState == GhostState.Eaten))
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

        private Point GetCurrentTargetTile(Pacman pacman)
        {
            if (CurrentState == GhostState.Scatter)
                return ScatterTargetTile;

            if (CurrentState == GhostState.Chase)
                return GetChaseTargetTile(pacman);

            if (CurrentState == GhostState.Frightened)
            {
                Random rand = new Random();
                return new Point(rand.Next(0, GameConfig.MapWidth), rand.Next(0, GameConfig.MapHeight));
            }

            return new Point(13, 14);
        }

        private Keys GetOppositeDirection(Keys dir)
        {
            if (dir == Keys.Up) return Keys.Down;
            if (dir == Keys.Down) return Keys.Up;
            if (dir == Keys.Left) return Keys.Right;
            if (dir == Keys.Right) return Keys.Left;
            return Keys.None;
        }

        public void ForceDemiTour()
        {
            // Un fantôme ne fait demi-tour que s'il est actif sur la carte
            // (On ne force pas le demi-tour s'il est dans le nid ou déjà mort sous forme d'yeux)
            if (CurrentState == GhostState.Chase || CurrentState == GhostState.Scatter || CurrentState == GhostState.Frightened)
            {
                this.CurrentDirection = GetOppositeDirection(this.CurrentDirection);
            }
        }

        private void HandleTunnelTeleport(int currentTileX)
        {
            if (currentTileX < 0 && CurrentDirection == Keys.Left)
                X = (GameConfig.MapWidth - 1) * GameConfig.TileSize;
            else if (currentTileX >= GameConfig.MapWidth && CurrentDirection == Keys.Right)
                X = 0;
        }

        protected abstract Point GetChaseTargetTile(Pacman pacman);
    }
}