using System.Drawing;

namespace PacmanWinForms
{
    public class Clyde : Ghost
    {
        public override GhostType Type { get { return GhostType.Clyde; } }

        // Départ : droite du nid
        public Clyde() : base(16, 13 + 3, MazeLayout.ClydeScatterTarget)
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
            int dx = TileX - pacman.TileX;
            int dy = TileY - pacman.TileY;

            // Distance >= 8 cases (comparée au carré : 8 x 8 = 64)
            if (dx * dx + dy * dy >= 64)
            {
                // Loin de Pac-Man : attaque directe
                return new Point(pacman.TileX, pacman.TileY);
            }

            // Trop près : fuite vers son coin
            return ScatterTargetTile;
        }
    }
}