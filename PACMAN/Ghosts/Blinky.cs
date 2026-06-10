using System.Drawing;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public class Blinky : Ghost
    {
        // Constructeur de Blinky
        // - Position initiale : Case (14, 11) -> Juste au-dessus du nid des fantômes, prêt à attaquer.
        // - Cible Scatter (Patrouille) : Case (27, 0) -> Le coin supérieur droit de l'écran.
        public Blinky() : base(14, 11+3, new Point(27, 0))
        {
            // Vitesse initiale (optionnel, hérité de Ghost)
            this.Speed = 1.0f;
            this.X -= 4;
            this.CurrentState = GhostState.Scatter; // Blinky est déjà dehors !
            this.CurrentDirection = Keys.Left;
        }

        // L'intelligence artificielle unique de Blinky en mode Chasse (Chase) :
        // Il cible directement la case où se trouve Pac-Man, sans fioritures.
        protected override Point GetChaseTargetTile(Pacman pacman)
        {
            int targetX = (int)(pacman.X / GameConfig.TileSize);
            int targetY = (int)(pacman.Y / GameConfig.TileSize);

            return new Point(targetX, targetY);
        }
       
    }
}