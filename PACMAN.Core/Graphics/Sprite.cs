namespace PacmanWinForms
{
    /// <summary>
    /// Un rectangle d'une image (le plus souvent la feuille de sprites). Un Sprite ne copie rien :
    /// c'est une simple "vue" sur les pixels du Canvas source. Les sprites sont partagés, ne les modifiez pas.
    /// </summary>
    public sealed class Sprite
    {
        public Canvas Source { get; private set; }   // null : sprite vide (rien à dessiner)
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public Sprite(Canvas source, int x, int y, int width, int height)
        {
            Source = source;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>Un sprite entièrement transparent de la taille demandée.</summary>
        public static Sprite Empty(int width, int height)
        {
            return new Sprite(null, 0, 0, width, height);
        }

        /// <summary>Lit un pixel du sprite (coordonnées relatives au sprite). Hors du sprite : transparent.</summary>
        public uint GetPixel(int x, int y)
        {
            if (Source == null || x < 0 || y < 0 || x >= Width || y >= Height) return 0;
            return Source.GetPixel(X + x, Y + y);
        }
    }
}
