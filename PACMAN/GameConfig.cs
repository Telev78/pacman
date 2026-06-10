using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacmanWinForms
{
    public static class GameConfig
    {
        public const int TileSize = 8;       // Taille d'une case d'origine (8x8 pixels)
        public const int MapWidth = 28;      // 28 colonnes
        public const int MapHeight = 36;     // 36 lignes

        public const int Zoom = 3;           // Multiplicateur pour écran moderne

        // Tailles calculées pour l'affichage de la fenêtre
        public const int ScaledTileSize = TileSize * Zoom; // 24 pixels
        public const int ScreenWidth = MapWidth * ScaledTileSize;  // 672 pixels
        public const int ScreenHeight = MapHeight * ScaledTileSize; // 864 pixels
    }
}
