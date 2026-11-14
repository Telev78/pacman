using System.Drawing;

namespace PacmanWinForms
{
    public class Inky : Ghost
    {
        private Ghost blinky;

        public override GhostType Type { get { return GhostType.Inky; } }

        // Départ : gauche du nid
        private readonly bool overflowBug;

        public Inky(Ghost blinkyRef, bool overflowBug = true) : base(12, 13 + 3, MazeLayout.InkyScatterTarget)
        {
            this.overflowBug = overflowBug;
            this.blinky = blinkyRef;
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
            int pacmanX = pacman.TileX;
            int pacmanY = pacman.TileY;

            // 1. Case pivot : 2 cases devant Pac-Man
            switch (pacman.CurrentDirection)
            {
                case Direction.Up:
                    pacmanY -= 2;
                    if (overflowBug) pacmanX -= 2; // Bug d'overflow de l'arcade
                    break;
                case Direction.Down: pacmanY += 2; break;
                case Direction.Left: pacmanX -= 2; break;
                case Direction.Right: pacmanX += 2; break;
            }

            // 2. Position de Blinky
            int blinkyX = blinky.TileX;
            int blinkyY = blinky.TileY;

            // 3. Vecteur Blinky -> pivot, doublé
            int targetX = blinkyX + (pacmanX - blinkyX) * 2;
            int targetY = blinkyY + (pacmanY - blinkyY) * 2;

            return new Point(targetX, targetY);
        }
    }
}