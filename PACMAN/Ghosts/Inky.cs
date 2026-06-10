using System.Drawing;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public class Inky : Ghost
    {
        private Ghost blinky;

        // Départ : Gauche du nid (12, 13+3) | Patrouille : Coin inférieur droit (27, Max)
        public Inky(Ghost blinkyRef) : base(12, 13 + 3, new Point(27, GameConfig.MapHeight - 1))
        {
            this.blinky = blinkyRef;
            this.Speed = 1.0f;
            this.CurrentState = GhostState.InHouse;
            this.CurrentDirection = Keys.Up;

            // AJUSTEMENT ARCADE : Centrage parfait dans le nid
            this.X -= 4;
            this.Y += 8;
            this.initialY = this.Y; // On réaligne la base de son oscillation verticale
        }

        protected override Point GetChaseTargetTile(Pacman pacman)
        {
            int pacmanX = (int)(pacman.X / GameConfig.TileSize);
            int pacmanY = (int)(pacman.Y / GameConfig.TileSize);

            // 1. Déterminer la case pivot (2 cases devant Pac-Man)
            switch (pacman.CurrentDirection)
            {
                case Keys.Up: pacmanY -= 2; break;
                case Keys.Down: pacmanY += 2; break;
                case Keys.Left: pacmanX -= 2; break;
                case Keys.Right: pacmanX += 2; break;
            }

            // 2. Obtenir les coordonnées de Blinky sur la grille
            int blinkyX = (int)(blinky.X / GameConfig.TileSize);
            int blinkyY = (int)(blinky.Y / GameConfig.TileSize);

            // 3. Calculer le vecteur Blinky -> Pivot et le doubler
            int vectorX = pacmanX - blinkyX;
            int vectorY = pacmanY - blinkyY;

            return new Point(blinkyX + (vectorX * 2), blinkyY + (vectorY * 2));
        }
    }
}