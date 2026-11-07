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

        // Passage du tunnel : l'entité réapparaît de l'autre côté quand son sprite est entièrement hors écran
        public static float WrapTunnelX(float x)
        {
            const int T = GameConfig.TileSize;
            if (x <= -2 * T) return (GameConfig.MapWidth + 1) * T;   // sorti à gauche (-16) -> réapparaît à droite (232)
            if (x >= (GameConfig.MapWidth + 2) * T) return -T;        // sorti à droite (240) -> réapparaît à gauche (-8)
            return x;
        }

        // --- Cibles de patrouille (Scatter), en tuiles ---
        // Blinky et Pinky visent des cases décalées vers l'intérieur des coins du haut (comme à l'arcade),
        // ce qui les fait boucler autour des blocs. Inky et Clyde visent juste sous les coins du bas.
        public static readonly Point BlinkyScatterTarget = new Point(25, 0);
        public static readonly Point PinkyScatterTarget = new Point(2, 0);
        public static readonly Point InkyScatterTarget = new Point(27, GameConfig.MapHeight - 2);
        public static readonly Point ClydeScatterTarget = new Point(0, GameConfig.MapHeight - 2);

        // La case (colonne, ligne) est-elle dans la zone où le tunnel ralentit les fantômes ?
        public static bool IsInTunnelSlowZone(int tileX, int tileY)
        {
            return tileY == TunnelRow
                && (tileX <= TunnelSlowLeftMaxColumn || tileX >= TunnelSlowRightMinColumn);
        }

        // Intersections où les fantômes (Scatter et Chase) n'ont pas le droit de tourner vers le haut :
        // deux au-dessus du nid et deux près du départ de Pac-Man (colonnes 12 et 15, lignes 14 et 26).
        // Seul Pac-Man peut entrer dans ces couloirs par le bas ; les fantômes effrayés ignorent cette règle.
        private static readonly Point[] NoUpTiles =
        {
            new Point(12, 14), new Point(15, 14),
            new Point(12, 26), new Point(15, 26)
        };

        public static bool IsNoUpTile(int tileX, int tileY)
        {
            for (int i = 0; i < NoUpTiles.Length; i++)
            {
                if (NoUpTiles[i].X == tileX && NoUpTiles[i].Y == tileY) return true;
            }
            return false;
        }
    }
}