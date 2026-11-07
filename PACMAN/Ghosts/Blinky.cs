using System.Drawing;

namespace PacmanWinForms
{
    public class Blinky : Ghost
    {
        public override GhostType Type { get { return GhostType.Blinky; } }
        private int elroyLevel = 0; // 0 = normal, 1 = Elroy 1, 2 = Elroy 2
        public bool IsCruiseElroy1 { get { return elroyLevel == 1; } }
        public bool IsCruiseElroy2 { get { return elroyLevel == 2; } }

        // Départ : juste au-dessus du nid
        public Blinky() : base(14, 11 + 3, MazeLayout.BlinkyScatterTarget)
        {
            this.X -= 4;
            this.Phase = GhostPhase.Active; // Blinky est déjà dehors !
            this.Mode = GhostMode.Scatter;
            this.CurrentDirection = Direction.Left;
        }

        // Chase : cible directement la case de Pac-Man
        protected override Point GetChaseTargetTile(Pacman pacman)
        {
            return new Point(pacman.TileX, pacman.TileY);
        }

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
            // Cruise Elroy : en Scatter, Blinky continue de foncer sur Pac-Man
            if ((IsCruiseElroy1 || IsCruiseElroy2) && this.Phase == GhostPhase.Active && this.Mode == GhostMode.Scatter)
            {
                return GetChaseTargetTile(pacman);
            }

            return base.GetCurrentTargetTile(pacman);
        }
    }
}