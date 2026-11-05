using System;
using System.Drawing;

namespace PacmanWinForms
{
    public class Clyde : Ghost
    {
        // Départ : droite du nid | Patrouille : coin inférieur gauche
        public Clyde() : base(16, 13 + 3, new Point(0, GameConfig.MapHeight - 1))
        {
            this.Phase = GhostPhase.InHouse;
            this.CurrentDirection = Direction.Up;
            this.bobDirection = -1;

            // Centrage dans le nid
            this.X -= 4;
            this.Y += 8;
            this.initialY = this.Y;
        }

        protected override Point GetChaseTargetTile(Pacman pacman)
        {
            int currentTileX = TileX;
            int currentTileY = TileY;
            int pacmanTileX = pacman.TileX;
            int pacmanTileY = pacman.TileY;

            double distance = Math.Sqrt(Math.Pow(currentTileX - pacmanTileX, 2) + Math.Pow(currentTileY - pacmanTileY, 2));

            if (distance >= 8)
            {
                // Loin de Pac-Man : attaque directe
                return new Point(pacmanTileX, pacmanTileY);
            }
            else
            {
                // Trop près : fuite vers son coin
                return ScatterTargetTile;
            }
        }
    }
}