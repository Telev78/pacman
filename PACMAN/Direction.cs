namespace PacmanWinForms
{
    public enum Direction
    {
        None,
        Up,
        Left,
        Down,
        Right
    }

    public static class DirectionExtensions
    {
        public static Direction Opposite(this Direction d)
        {
            switch (d)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
                default: return Direction.None;
            }
        }

        // Déplacement unitaire en X (-1, 0, +1)
        public static int Dx(this Direction d)
        {
            if (d == Direction.Left) return -1;
            if (d == Direction.Right) return 1;
            return 0;
        }

        // Déplacement unitaire en Y (-1, 0, +1)
        public static int Dy(this Direction d)
        {
            if (d == Direction.Up) return -1;
            if (d == Direction.Down) return 1;
            return 0;
        }
    }
}