using System.Drawing;

namespace PacmanWinForms
{
    /// <summary>
    /// Ce qui distingue un labyrinthe d'un autre en plus de ses tuiles : lignes des tunnels,
    /// zones où les fantômes ralentissent, intersections interdites aux virages vers le haut.
    /// Le nid des fantômes et le départ de Pac-Man sont identiques partout (voir MazeLayout).
    /// </summary>
    public sealed class MazeDefinition
    {
        /// <summary>Nom lisible (diagnostic).</summary>
        public string Name { get; private set; }

        /// <summary>Ressource embarquée contenant la physique puis le visuel (72 lignes), ex. "asset.pacman.maps.original_maze.txt".</summary>
        public string ResourcePath { get; private set; }

        /// <summary>Lignes (en tuiles) où le labyrinthe est ouvert sur les deux bords.</summary>
        public int[] TunnelRows { get; private set; }

        /// <summary>Colonnes <= à cette valeur : zone lente à gauche d'un tunnel.</summary>
        public int TunnelSlowLeftMaxColumn { get; private set; }

        /// <summary>Colonnes >= à cette valeur : zone lente à droite d'un tunnel.</summary>
        public int TunnelSlowRightMinColumn { get; private set; }

        /// <summary>Intersections où les fantômes (Scatter et Chase) ne peuvent pas tourner vers le haut.</summary>
        public Point[] NoUpTiles { get; private set; }

        public MazeDefinition(string name, string resourcePath, int[] tunnelRows,
            int slowLeftMaxColumn, int slowRightMinColumn, Point[] noUpTiles)
        {
            Name = name;
            ResourcePath = resourcePath;
            TunnelRows = tunnelRows;
            TunnelSlowLeftMaxColumn = slowLeftMaxColumn;
            TunnelSlowRightMinColumn = slowRightMinColumn;
            NoUpTiles = noUpTiles;
        }

        /// <summary>Le labyrinthe de Pac-Man.</summary>
        public static readonly MazeDefinition Original = new MazeDefinition(
            "Pac-Man",
            PacManVariant.Folder + "maps.original_maze.txt",
            new[] { 17 },
            5,
            22,
            // Deux au-dessus du nid et deux près du départ de Pac-Man (colonnes 12 et 15, lignes 14 et 26).
            // Seul Pac-Man peut entrer dans ces couloirs par le bas ; les fantômes effrayés ignorent cette règle.
            new[] { new Point(12, 14), new Point(15, 14), new Point(12, 26), new Point(15, 26) });

        public bool IsTunnelRow(int tileY)
        {
            for (int i = 0; i < TunnelRows.Length; i++)
            {
                if (TunnelRows[i] == tileY) return true;
            }
            return false;
        }

        // La case (colonne, ligne) est-elle dans la zone où le tunnel ralentit les fantômes ?
        public bool IsInTunnelSlowZone(int tileX, int tileY)
        {
            return IsTunnelRow(tileY)
                && (tileX <= TunnelSlowLeftMaxColumn || tileX >= TunnelSlowRightMinColumn);
        }

        public bool IsNoUpTile(int tileX, int tileY)
        {
            for (int i = 0; i < NoUpTiles.Length; i++)
            {
                if (NoUpTiles[i].X == tileX && NoUpTiles[i].Y == tileY) return true;
            }
            return false;
        }
    }
}
