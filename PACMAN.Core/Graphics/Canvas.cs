using System;

namespace PacmanWinForms
{
    /// <summary>
    /// Une image en mémoire sur laquelle on dessine : des pixels ARGB 32 bits (0xAARRGGBB), sans aucune
    /// dépendance à la plateforme. Le jeu dessine toute son image (224x288) dans un Canvas ; l'hôte
    /// (WinForms aujourd'hui, SDL demain) n'a plus qu'à afficher ces pixels, agrandis.
    ///
    /// En mémoire, chaque pixel est écrit octet par octet dans l'ordre B, G, R, A (processeurs little-endian) :
    /// c'est le format "ARGB 8888" de GDI+ (Format32bppArgb) et de SDL (SDL_PIXELFORMAT_ARGB8888).
    /// </summary>
    public sealed class Canvas
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>Les pixels, ligne par ligne, de haut en bas (Width * Height éléments).</summary>
        public uint[] Pixels { get; private set; }

        public Canvas(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException("width");
            if (height <= 0) throw new ArgumentOutOfRangeException("height");

            Width = width;
            Height = height;
            Pixels = new uint[width * height];   // Au départ : entièrement transparent (0x00000000)
        }

        /// <summary>Remplit toute l'image d'une couleur (par exemple 0xFF000000 pour du noir opaque).</summary>
        public void Clear(uint argb)
        {
            for (int i = 0; i < Pixels.Length; i++)
            {
                Pixels[i] = argb;
            }
        }

        /// <summary>Lit un pixel. Hors de l'image : transparent.</summary>
        public uint GetPixel(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return 0;
            return Pixels[y * Width + x];
        }

        /// <summary>Écrit un pixel (sans mélange). Hors de l'image : ignoré.</summary>
        public void SetPixel(int x, int y, uint argb)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            Pixels[y * Width + x] = argb;
        }

        /// <summary>
        /// Rend transparents tous les pixels dont la couleur (0x00RRGGBB) est exactement celle donnée,
        /// quelle que soit leur opacité. Équivalent de Bitmap.MakeTransparent.
        /// </summary>
        public void MakeTransparent(uint rgb)
        {
            for (int i = 0; i < Pixels.Length; i++)
            {
                if ((Pixels[i] & 0x00FFFFFFu) == rgb)
                {
                    Pixels[i] = 0;
                }
            }
        }

        /// <summary>
        /// Dessine un sprite à l'échelle 1:1, avec son coin supérieur gauche en (x, y).
        /// Les pixels transparents ne changent rien ; ce qui sort de l'image est ignoré.
        /// </summary>
        public void Draw(Sprite sprite, int x, int y)
        {
            DrawScaled(sprite, x, y, sprite.Width, sprite.Height);
        }

        /// <summary>
        /// Dessine un sprite étiré (ou réduit) à width x height pixels, sans lissage (plus proche voisin).
        /// </summary>
        public void DrawScaled(Sprite sprite, int x, int y, int width, int height)
        {
            Canvas source = sprite.Source;
            if (source == null || width <= 0 || height <= 0 || sprite.Width <= 0 || sprite.Height <= 0) return;

            for (int row = 0; row < height; row++)
            {
                int dy = y + row;
                if (dy < 0 || dy >= Height) continue;

                int sy = sprite.Y + (row * sprite.Height) / height;
                if (sy < 0 || sy >= source.Height) continue;

                for (int col = 0; col < width; col++)
                {
                    int dx = x + col;
                    if (dx < 0 || dx >= Width) continue;

                    int sx = sprite.X + (col * sprite.Width) / width;
                    if (sx < 0 || sx >= source.Width) continue;

                    Blend(ref Pixels[dy * Width + dx], source.Pixels[sy * source.Width + sx]);
                }
            }
        }

        // Pose le pixel src sur le pixel dst (mélange "source par-dessus", alpha non prémultiplié)
        private static void Blend(ref uint dst, uint src)
        {
            uint a = src >> 24;
            if (a == 0) return;                      // Transparent : rien à faire
            if (a == 255) { dst = src; return; }     // Opaque : remplace

            uint da = dst >> 24;
            uint inv = 255 - a;
            uint outA = a + (da * inv + 127) / 255;
            uint denominator = outA * 255;

            uint r = Mix((src >> 16) & 0xFF, a, (dst >> 16) & 0xFF, da * inv, denominator);
            uint g = Mix((src >> 8) & 0xFF, a, (dst >> 8) & 0xFF, da * inv, denominator);
            uint b = Mix(src & 0xFF, a, dst & 0xFF, da * inv, denominator);

            dst = (outA << 24) | (r << 16) | (g << 8) | b;
        }

        private static uint Mix(uint s, uint a, uint d, uint dWeight, uint denominator)
        {
            return (s * a * 255 + d * dWeight + denominator / 2) / denominator;
        }
    }
}
