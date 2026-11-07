using System.Drawing;

namespace PacmanWinForms
{
    public class Pinky : Ghost
    {
        public override GhostType Type { get { return GhostType.Pinky; } }

        // Départ : milieu du nid
        public Pinky() : base(14, 13 + 3, MazeLayout.PinkyScatterTarget)
        {
            this.Phase = GhostPhase.InHouse;
            this.CurrentDirection = Direction.Down;

            // Centrage dans le nid
            this.X -= 4;
            this.Y += 8;
            this.initialY = this.Y;
        }

        protected override Point GetChaseTargetTile(Pacman pacman)
        {
            int targetX = pacman.TileX;
            int targetY = pacman.TileY;

            // 4 cases devant Pac-Man
            switch (pacman.CurrentDirection)
            {
                case Direction.Up:
                    targetY -= 4;
                    targetX -= 4; // Bug d'overflow de l'arcade
                    break;
                case Direction.Down: targetY += 4; break;
                case Direction.Left: targetX -= 4; break;
                case Direction.Right: targetX += 4; break;
            }

            return new Point(targetX, targetY);
        }
    }
}