using System.Drawing;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public class Pinky : Ghost
    {
        // Départ : Milieu du nid (14, 13+3) | Patrouille : Coin supérieur gauche (0, 0)
        public Pinky() : base(14, 13 + 3, new Point(0, 0))
        {
            
            this.CurrentState = GhostState.InHouse;
            this.CurrentDirection = Keys.Down;

            // AJUSTEMENT ARCADE : Centrage parfait dans le nid
            this.X -= 4;
            this.Y += 8;
            this.initialY = this.Y; // On réaligne la base de son oscillation verticale
        }

        protected override Point GetChaseTargetTile(Pacman pacman)
        {
            int targetX = (int)(pacman.X / GameConfig.TileSize);
            int targetY = (int)(pacman.Y / GameConfig.TileSize);

            // On ajoute 4 cases d'avance selon la direction de Pac-Man
            switch (pacman.CurrentDirection)
            {
                // Correction pour intégrer le bug officiel de l'arcade :
                case Keys.Up:
                    targetY -= 4;
                    targetX -= 4; // Le fameux overflow de l'arcade !
                    break;
                case Keys.Down: targetY += 4; break;
                case Keys.Left: targetX -= 4; break;
                case Keys.Right: targetX += 4; break;
            }

            return new Point(targetX, targetY);
        }
    }
}