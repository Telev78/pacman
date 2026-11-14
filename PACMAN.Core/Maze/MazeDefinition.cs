using System.Collections.Generic;
using System.Drawing;

namespace PacmanWinForms
{
    /// <summary>
    /// Ce qui distingue un labyrinthe d'un autre en plus de ses tuiles : lignes des tunnels,
    /// zones où les fantômes ralentissent, intersections interdites aux virages vers le haut,
    /// jeu de tuiles de la feuille de sprites (Ms Pac-Man).
    /// Le nid des fantômes et le départ de Pac-Man sont identiques dans tous les labyrinthes d'un même jeu.
    /// </summary>
    public sealed class MazeDefinition
    {
        // Case où se trouve Pac-Man au départ (colonnes 13 et 14 de la ligne 26)
        private const int StartTileX = 13;
        private const int StartTileY = 26;

        /// <summary>Nom lisible (diagnostic).</summary>
        public string Name { get; private set; }

        /// <summary>Ressource embarquée contenant la physique puis le visuel (72 lignes), ex. "asset.pacman.maps.original_maze.txt".</summary>
        public string ResourcePath { get; private set; }

        /// <summary>Jeu de tuiles de la feuille de sprites (0 à 5, Ms Pac-Man) ; -1 pour Pac-Man (tuiles à l'ancienne).</summary>
        public int TileSetIndex { get; private set; }

        /// <summary>Numéro de la tuile "gomme" dans le jeu de tuiles (Ms Pac-Man) ; la super-gomme est la suivante.</summary>
        public int DotTileId { get; private set; }

        /// <summary>Lignes (en tuiles) où le labyrinthe est ouvert sur les deux bords.</summary>
        public int[] TunnelRows { get { return tunnelRows; } }

        private int[] tunnelRows;
        private int[] slowLeftMax;     // Par ligne de tunnel : colonnes <= à cette valeur = zone lente à gauche
        private int[] slowRightMin;    // Par ligne de tunnel : colonnes >= à cette valeur = zone lente à droite
        private readonly bool detectTunnels;
        private bool analyzed;

        /// <summary>Intersections où les fantômes (Scatter et Chase) ne peuvent pas tourner vers le haut.</summary>
        public Point[] NoUpTiles { get; private set; }

        /// <summary>Labyrinthe aux tunnels connus d'avance (Pac-Man).</summary>
        public MazeDefinition(string name, string resourcePath, int[] tunnelRows,
            int slowLeftMaxColumn, int slowRightMinColumn, Point[] noUpTiles)
        {
            Name = name;
            ResourcePath = resourcePath;
            TileSetIndex = -1;
            DotTileId = -1;
            this.tunnelRows = tunnelRows;

            slowLeftMax = new int[tunnelRows.Length];
            slowRightMin = new int[tunnelRows.Length];
            for (int i = 0; i < tunnelRows.Length; i++)
            {
                slowLeftMax[i] = slowLeftMaxColumn;
                slowRightMin[i] = slowRightMinColumn;
            }

            NoUpTiles = noUpTiles;
            detectTunnels = false;
            analyzed = true;
        }

        /// <summary>Labyrinthe de Ms Pac-Man : les tunnels sont repérés dans la carte au chargement.</summary>
        public MazeDefinition(string name, string resourcePath, int tileSetIndex, int dotTileId)
        {
            Name = name;
            ResourcePath = resourcePath;
            TileSetIndex = tileSetIndex;
            DotTileId = dotTileId;
            tunnelRows = new int[0];
            slowLeftMax = new int[0];
            slowRightMin = new int[0];
            NoUpTiles = new Point[0];
            detectTunnels = true;
            analyzed = false;
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

        /// <summary>
        /// Repère les tunnels dans la carte chargée (une seule fois). Une ligne est un tunnel si ses deux bords
        /// sont accessibles depuis le départ de Pac-Man. La zone lente va du bord jusqu'à la dernière case
        /// encadrée de murs au-dessus et en dessous.
        /// </summary>
        public void Analyze(TileType[,] grid)
        {
            if (analyzed) return;
            analyzed = true;
            if (!detectTunnels) return;

            int height = grid.GetLength(0);
            int width = grid.GetLength(1);

            // Cases accessibles depuis le départ de Pac-Man (sans passer d'un bord à l'autre)
            bool[,] reached = new bool[height, width];
            Stack<Point> todo = new Stack<Point>();
            todo.Push(new Point(StartTileX, StartTileY));

            while (todo.Count > 0)
            {
                Point p = todo.Pop();
                if (p.X < 0 || p.X >= width || p.Y < 0 || p.Y >= height) continue;
                if (reached[p.Y, p.X]) continue;
                if (grid[p.Y, p.X] == TileType.Wall || grid[p.Y, p.X] == TileType.GhostDoor) continue;

                reached[p.Y, p.X] = true;
                todo.Push(new Point(p.X + 1, p.Y));
                todo.Push(new Point(p.X - 1, p.Y));
                todo.Push(new Point(p.X, p.Y + 1));
                todo.Push(new Point(p.X, p.Y - 1));
            }

            List<int> rows = new List<int>();
            List<int> lefts = new List<int>();
            List<int> rights = new List<int>();

            for (int r = 1; r < height - 1; r++)
            {
                if (!reached[r, 0] || !reached[r, width - 1]) continue;

                int left = -1;
                for (int c = 0; c < width && grid[r - 1, c] == TileType.Wall && grid[r + 1, c] == TileType.Wall; c++)
                    left = c;

                int right = width;
                for (int c = width - 1; c >= 0 && grid[r - 1, c] == TileType.Wall && grid[r + 1, c] == TileType.Wall; c--)
                    right = c;

                rows.Add(r);
                lefts.Add(left);
                rights.Add(right);
            }

            tunnelRows = rows.ToArray();
            slowLeftMax = lefts.ToArray();
            slowRightMin = rights.ToArray();
        }

        private int IndexOfTunnelRow(int tileY)
        {
            for (int i = 0; i < tunnelRows.Length; i++)
            {
                if (tunnelRows[i] == tileY) return i;
            }
            return -1;
        }

        public bool IsTunnelRow(int tileY)
        {
            return IndexOfTunnelRow(tileY) >= 0;
        }

        // La case (colonne, ligne) est-elle dans la zone où le tunnel ralentit les fantômes ?
        public bool IsInTunnelSlowZone(int tileX, int tileY)
        {
            int i = IndexOfTunnelRow(tileY);
            return i >= 0 && (tileX <= slowLeftMax[i] || tileX >= slowRightMin[i]);
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
