using System;

namespace PacmanWinForms
{
    /// <summary>
    /// Convention unique : la position (X, Y) d'une entité est le coin haut-gauche de sa boîte logique 8x8.
    /// Son CENTRE est en (X + 4, Y + 4). La tuile d'une entité est toujours celle de son centre.
    /// </summary>
    public static class Tiles
    {
        public const int Size = GameConfig.TileSize;
        public const int HalfTile = GameConfig.TileSize / 2;

        // Tuile contenant le centre d'une entité dont la position (coin) est "pos"
        public static int Of(float pos)
        {
            return (int)Math.Floor((pos + HalfTile) / Size);
        }

        // L'entité est-elle exactement centrée sur une tuile ?
        public static bool IsAligned(float x, float y)
        {
            return x % Size == 0 && y % Size == 0;
        }
    }
}
