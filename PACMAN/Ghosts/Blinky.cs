using System.Drawing;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public class Blinky : Ghost
    {
        private int elroyLevel = 0; // 0 = normal, 1 = Elroy 1, 2 = Elroy 2
        public bool IsCruiseElroy1 { get { return elroyLevel == 1; } }
        public bool IsCruiseElroy2 { get { return elroyLevel == 2; } }

        // Constructeur de Blinky
        // - Position initiale : Case (14, 11) -> Juste au-dessus du nid des fantômes, prêt à attaquer.
        // - Cible Scatter (Patrouille) : Case (27, 0) -> Le coin supérieur droit de l'écran.
        public Blinky() : base(14, 11+3, new Point(27, 0))
        {
            
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

        // Déterminer si Blinky passe en Elroy et ajuster sa vitesse
        public void UpdateElroyStatus(int dotsRemaining, LevelSpecs specs, bool enabled)
        {
            if (!enabled) elroyLevel = 0;
            else if (dotsRemaining <= specs.Elroy2Dots) elroyLevel = 2;
            else if (dotsRemaining <= specs.Elroy1Dots) elroyLevel = 1;
            else elroyLevel = 0;
        }

        protected override float GetNormalSpeed(LevelSpecs specs)
        {
            if (elroyLevel == 2) return specs.Elroy2Speed;
            if (elroyLevel == 1) return specs.Elroy1Speed;
            return specs.GhostNormal;
        }

        protected override Point GetCurrentTargetTile(Pacman pacman)
        {
            // RÈGLE CRUISE ELROY : Si Elroy est actif et qu'on est en mode Scatter, 
            // on court-circuite le comportement normal pour foncer sur Pac-Man (Chase).
            if ((IsCruiseElroy1 || IsCruiseElroy2) && this.CurrentState == GhostState.Scatter)
            {
                return GetChaseTargetTile(pacman);
            }

            // Pour TOUS les autres cas (Chase normal, Scatter normal, Eaten, Frightened, InHouse, Exiting),
            // on fait confiance à la logique générique définie dans la classe Ghost.
            return base.GetCurrentTargetTile(pacman);
        }
    }
}