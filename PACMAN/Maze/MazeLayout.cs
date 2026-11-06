using System.Drawing;

namespace PacmanWinForms
{
    /// <summary>
    /// Repères fixes du labyrinthe d'origine.
    /// </summary>
    public static class MazeLayout
    {
        // --- Tunnel latéral ---
        public const int TunnelRow = 17;
        public const int TunnelSlowLeftMaxColumn = 5;    // colonnes <= 5 : zone lente à gauche
        public const int TunnelSlowRightMinColumn = 22;  // colonnes >= 22 : zone lente à droite

        // --- Nid des fantômes ---
        public const int HouseExitX = 14 * GameConfig.TileSize - 4;  // 108 : X de la porte
        public const int HouseExitY = 14 * GameConfig.TileSize;      // 112 : Y juste au-dessus de la porte
        public const int HouseInsideY = 17 * GameConfig.TileSize;    // 136 : Y d'un fantôme qui renaît dans le nid
        public const int HouseEntranceRow = 14;                      // Ligne où les yeux "entrent" dans le nid
        public static readonly Point EyesTargetTile = new Point(13, 14);

        // --- Départ de Pac-Man ---
        public const int PacmanStartX = 13 * GameConfig.TileSize + 4;  // 108 (entre deux tuiles, comme à l'arcade)
        public const int PacmanStartY = 26 * GameConfig.TileSize;      // 208
    }
}