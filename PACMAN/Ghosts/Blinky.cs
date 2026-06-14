using System.Drawing;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public class Blinky : Ghost
    {
        public bool IsCruiseElroy1 { get; private set; } = false;
        public bool IsCruiseElroy2 { get; private set; } = false;

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
        public void UpdateElroyStatus(int level, int dotsRemaining, LevelSpecs specs)
        {
            // Détermination des seuils selon le Pac-Man Dossier
            int elroy1Threshold = 20;
            int elroy2Threshold = 10;

            if (level == 2) { elroy1Threshold = 30; elroy2Threshold = 15; }
            else if (level >= 3 && level <= 5) { elroy1Threshold = 40; elroy2Threshold = 20; }
            else if (level >= 6 && level <= 18) { elroy1Threshold = 50; elroy2Threshold = 25; }
            else if (level >= 19) { elroy1Threshold = 60; elroy2Threshold = 30; }

            // Application des états et modification de la vitesse de Blinky
            if (dotsRemaining <= elroy2Threshold)
            {
                IsCruiseElroy2 = true;
                IsCruiseElroy1 = false;
                // À l'arcade, Elroy 2 augmente la vitesse de +5% par rapport à sa vitesse normale
                this.Speed = specs.GhostNormal + 0.05f;
            }
            else if (dotsRemaining <= elroy1Threshold)
            {
                IsCruiseElroy1 = true;
                IsCruiseElroy2 = false;
                // Elroy 1 augmente la vitesse de +5% (souvent identique à Pac-Man)
                this.Speed = specs.GhostNormal + 0.025f;
            }
            else
            {
                IsCruiseElroy1 = false;
                IsCruiseElroy2 = false;
                this.Speed = specs.GhostNormal;
            }
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