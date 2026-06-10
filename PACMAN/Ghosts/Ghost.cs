using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public abstract class Ghost
    {
        public float X { get; protected set; }
        public float Y { get; protected set; }
        public float Speed { get; protected set; } = 1.0f; // 1 pixel par frame à 60 FPS

        public Keys CurrentDirection { get; protected set; }
        public GhostState CurrentState { get; set; } = GhostState.Scatter;
        protected float initialY;       // Mémorise le Y de départ pour l'oscillation
        protected int bobDirection = 1; // 1 = descend, -1 = monte
        protected Point ScatterTargetTile { get; set; }

        public Ghost(int startTileX, int startTileY, Point scatterTarget)
        {
            // Positionnement initial calé sur la grille
            this.X = startTileX * GameConfig.TileSize;
            this.Y = startTileY * GameConfig.TileSize;
            this.initialY = this.Y;
            this.ScatterTargetTile = scatterTarget;
            this.CurrentDirection = Keys.Left;
        }

        // --- MÉTHODE PRINCIPALE DE DÉPLACEMENT ---
        public virtual void Move(LevelMap map, Pacman pacman)
        {
            // --- ROUTINE DU NID ---
            if (CurrentState == GhostState.InHouse)
            {
                // Vitesse réduite à l'intérieur du nid (historiquement ~50% de la vitesse normale)
                float houseSpeed = this.Speed * 0.5f;

                // On applique le mouvement vertical
                Y += bobDirection * houseSpeed;

                // Limite de l'oscillation : 4 pixels vers le haut ou vers le bas
                int bobRange = 4;

                if (Y >= initialY + bobRange)
                {
                    Y = initialY + bobRange;
                    bobDirection = -1;          // On change de sens (on remonte)
                    CurrentDirection = Keys.Up; // L'animation des yeux regardera vers le bas
                }
                else if (Y <= initialY - bobRange)
                {
                    Y = initialY - bobRange;
                    bobDirection = 1;           // On change de sens (on redescend)
                    CurrentDirection = Keys.Down;   // L'animation des yeux regardera vers le haut
                }

                // TRÈS IMPORTANT : On fait un "return" ici pour ignorer complètement 
                // le reste de la méthode Move (pas de calcul de tuiles ni de murs) !
                return;
            }

            // --- CINÉMATIQUE DE SORTIE ---
            if (CurrentState == GhostState.Exiting)
            {
                // Coordonnées cibles basées sur tes offsets d'alignement (-4)
                float targetX = 14 * GameConfig.TileSize - 4; // Centre parfait (colonne de Pinky)
                float targetY = 14 * GameConfig.TileSize; // Juste dehors (ligne de Blinky)

                // Phase A : On s'aligne horizontalement sur le centre du nid
                if (X != targetX)
                {
                    if (Math.Abs(X - targetX) <= this.Speed)
                    {
                        X = targetX; // Alignement précis
                    }
                    else if (X < targetX)
                    {
                        X += this.Speed;
                        CurrentDirection = Keys.Right;
                    }
                    else if (X > targetX)
                    {
                        X -= this.Speed;
                        CurrentDirection = Keys.Left;
                    }
                }
                // Phase B : Une fois centré, on monte vers la sortie
                else if (Y > targetY)
                {
                    if (Math.Abs(Y - targetY) <= this.Speed)
                    {
                        Y = targetY;
                    }
                    else
                    {
                        Y -= this.Speed;
                        CurrentDirection = Keys.Up; // Les yeux regardent vers le haut
                    }
                }
                // Phase C : Le fantôme est officiellement dehors !
                else
                {
                    CurrentState = GhostState.Scatter; // Il commence sa patrouille extérieure
                    CurrentDirection = Keys.Left;      // Direction par défaut à la sortie
                }

                return; // On court-circuite le pathfinding tant qu'on sort
            }

            // 1. Sommes-nous parfaitement centrés sur une tuile (grille de 8x8) ?
            if ((X % GameConfig.TileSize == 0) && (Y % GameConfig.TileSize == 0))
            {
                int currentTileX = (int)(X / GameConfig.TileSize);
                int currentTileY = (int)(Y / GameConfig.TileSize);

                // Gérer le passage secret des tunnels (bords de l'écran)
                HandleTunnelTeleport(currentTileX);

                // Calculer la prochaine direction à prendre à cette intersection
                CurrentDirection = ChooseNextDirection(map, pacman, currentTileX, currentTileY);
            }

            // 2. Déplacement physique en fonction de la direction validée
            switch (CurrentDirection)
            {
                case Keys.Up: Y -= Speed; break;
                case Keys.Down: Y += Speed; break;
                case Keys.Left: X -= Speed; break;
                case Keys.Right: X += Speed; break;
            }
        }

        // --- L'ALGORITHME DE DÉCISION ARCADE ---
        private Keys ChooseNextDirection(LevelMap map, Pacman pacman, int currentTileX, int currentTileY)
        {
            Point targetTile = GetCurrentTargetTile(pacman);

            // Liste des directions possibles (Haut, Gauche, Bas, Droite -> Ordre de priorité officiel)
            Keys[] directionsToTest = { Keys.Up, Keys.Left, Keys.Down, Keys.Right };

            Keys bestDirection = CurrentDirection;
            double shortestDistance = double.MaxValue;

            foreach (Keys dir in directionsToTest)
            {
                // Un fantôme ne fait JAMAIS de demi-tour volontaire (180° interdit)
                if (dir == GetOppositeDirection(CurrentDirection))
                    continue;

                // Calculer la position de la tuile voisine selon la direction testée
                int nextX = currentTileX;
                int nextY = currentTileY;

                switch (dir)
                {
                    case Keys.Up: nextY--; break;
                    case Keys.Down: nextY++; break;
                    case Keys.Left: nextX--; break;
                    case Keys.Right: nextX++; break;
                }

                // Vérifier si cette tuile est un mur (1) ou s'il s'agit de la grille valide
                if (nextX >= 0 && nextX < GameConfig.MapWidth && nextY >= 0 && nextY < GameConfig.MapHeight)
                {
                    int tileType = map.Grid[nextY, nextX];

                    // Le fantôme peut marcher partout SAUF sur les murs (1)
                    // Note : En mode normal, il ne retourne dans le nid (4) que s'il est mort (Eaten)
                    if (tileType != 1 && (tileType != 4 || CurrentState == GhostState.Eaten))
                    {
                        // Calcul de la distance de Pythagore (au carré pour optimiser) entre la tuile testée et la cible
                        double distance = Math.Pow(nextX - targetTile.X, 2) + Math.Pow(nextY - targetTile.Y, 2);

                        // Si c'est plus court (ou premier choix égal grâce à l'ordre du tableau), on valide
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

        // --- GESTION ADMINISTRATIVE DU FANTÔME ---
        private Point GetCurrentTargetTile(Pacman pacman)
        {
            if (CurrentState == GhostState.Scatter)
                return ScatterTargetTile;

            if (CurrentState == GhostState.Chase)
                return GetChaseTargetTile(pacman);

            if (CurrentState == GhostState.Frightened)
            {
                // Mode effrayé : Choix semi-aléatoire (Simulé ici par une cible changeante)
                Random rand = new Random();
                return new Point(rand.Next(0, GameConfig.MapWidth), rand.Next(0, GameConfig.MapHeight));
            }

            return new Point(13, 14); // Mode Eaten : Ils ciblent le dessus de la porte pour rentrer
        }

        private Keys GetOppositeDirection(Keys dir)
        {
            if (dir == Keys.Up) return Keys.Down;
            if (dir == Keys.Down) return Keys.Up;
            if (dir == Keys.Left) return Keys.Right;
            if (dir == Keys.Right) return Keys.Left;
            return Keys.None;
        }

        private void HandleTunnelTeleport(int currentTileX)
        {
            // Téléportation fluide dans le tunnel (gauche <-> droite)
            if (currentTileX < 0 && CurrentDirection == Keys.Left)
                X = (GameConfig.MapWidth - 1) * GameConfig.TileSize;
            else if (currentTileX >= GameConfig.MapWidth && CurrentDirection == Keys.Right)
                X = 0;
        }

        // --- MÉTHODE ABSTRAITE ---
        protected abstract Point GetChaseTargetTile(Pacman pacman);
    }
}
