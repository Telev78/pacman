using System;
using System.Drawing;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public class Clyde : Ghost
    {
        // Départ : Droite du nid (16, 13+3) | Patrouille : Coin inférieur gauche (0, Max)
        public Clyde() : base(16, 13 + 3, new Point(0, GameConfig.MapHeight - 1))
        {
            
            this.CurrentState = GhostState.InHouse;
            this.CurrentDirection = Keys.Up;

            // AJUSTEMENT ARCADE : Centrage parfait dans le nid
            this.X -= 4;
            this.Y += 8;
            this.initialY = this.Y; // On réaligne la base de son oscillation verticale
        }

        protected override Point GetChaseTargetTile(Pacman pacman)
        {
            int currentTileX = (int)(X / GameConfig.TileSize);
            int currentTileY = (int)(Y / GameConfig.TileSize);
            int pacmanTileX = (int)(pacman.X / GameConfig.TileSize);
            int pacmanTileY = (int)(pacman.Y / GameConfig.TileSize);

            // Calcul de la distance à vol d'oiseau (Pythagore)
            double distance = Math.Sqrt(Math.Pow(currentTileX - pacmanTileX, 2) + Math.Pow(currentTileY - pacmanTileY, 2));

            if (distance >= 8)
            {
                // Mode Blinky : Attaque directe
                return new Point(pacmanTileX, pacmanTileY);
            }
            else
            {
                // Mode panique : Fuite vers sa zone Scatter
                return ScatterTargetTile;
            }
        }
    }
}