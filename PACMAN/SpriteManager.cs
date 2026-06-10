using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public class SpriteManager
    {
        private Bitmap generalSheet;
        private Bitmap mazeSheet;
        private Bitmap textSheet;

        public SpriteManager()
        {
            string assetPath = Path.Combine(Application.StartupPath, "asset");
            generalSheet = new Bitmap(Path.Combine(assetPath, "general.png"));
            mazeSheet = new Bitmap(Path.Combine(assetPath, "maze.png"));
            textSheet = new Bitmap(Path.Combine(assetPath, "text.png"));
        }

        public Bitmap GetEmptyMaze()
        {
            int width = 224;
            int height = 248;
            Bitmap croppedMaze = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(croppedMaze))
            {
                g.DrawImage(generalSheet, new Rectangle(0, 0, width, height), new Rectangle(228, 0, width, height), GraphicsUnit.Pixel);
            }
            return croppedMaze;
        }

        /// <summary>
        /// Récupère le bon sprite de Pac-Man selon sa direction et sa frame d'animation
        /// </summary>
        public Bitmap GetPacmanSprite(int animFrame, Keys direction)
        {
            int size = 16;
            int startX = 456;
            int startY = 0;

            // État 3 : Bouche complètement fermée (Partagée par toutes les directions)
            if (animFrame == 3)
            {
                startX = 488; // Position index 2 sur la première ligne
                startY = 0;
            }
            else
            {
                // Choix de la ligne (Y) selon la direction
                switch (direction)
                {
                    case Keys.Right: startY = 0; break;
                    case Keys.Left: startY = 16; break;
                    case Keys.Up: startY = 32; break;
                    case Keys.Down: startY = 48; break;
                    default: startY = 16; break; // Gauche par défaut au spawn
                }

                // animFrame 1 = 2ème sprite (Bouche grande ouverte)
                if (animFrame == 1)
                {
                    startX += size;
                }
            }

            Bitmap croppedPacman = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(croppedPacman))
            {
                g.DrawImage(generalSheet,
                    new Rectangle(0, 0, size, size),
                    new Rectangle(startX, startY, size, size),
                    GraphicsUnit.Pixel);
            }

            return croppedPacman;
        }

        /// <summary>
        /// Extrait une tuile 8x8 de maze.png selon sa colonne et sa ligne dans la feuille
        /// </summary>
        public Bitmap GetMazeTile(int sheetColumn, int sheetRow)
        {
            int size = GameConfig.TileSize; // 8
            Bitmap tile = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(tile))
            {
                g.DrawImage(mazeSheet,
                    new Rectangle(0, 0, size, size),
                    new Rectangle((sheetColumn * size)+225+sheetColumn, (sheetRow * size)+sheetRow, size, size),
                    GraphicsUnit.Pixel);
            }
            return tile;
        }

        /// <summary>
        /// Scanne le labyrinthe de general.png et génère automatiquement la matrice VisualGrid
        /// </summary>
        public void AutoScanAndBuildVisualGrid(LevelMap map)
        {
            Bitmap emptyMaze = GetEmptyMaze(); // Image bleue de 224x248
            int size = GameConfig.TileSize;

            // 1. Précharger les 48 tuiles de la variante BLEUE (Lignes 3 à 5 de maze.png)
            Bitmap[] bank = new Bitmap[48];
            for (int i = 0; i < 48; i++)
            {
                int col = i % 16;
                int row = (i / 16) + 3; // AJOUT : +3 car le bleu est en deuxième position (lignes 3, 4, 5)
                bank[i] = GetMazeTile(col, row);
            }

            // 2. Parcourir uniquement la zone du labyrinthe (lignes 3 à 33 sur 36)
            for (int r = 3; r < 34; r++)
            {
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    // Si la physique dit que c'est un mur, on cherche sa tuile visuelle
                    if (map.Grid[r, c] == 1)
                    {
                        int srcX = c * size;
                        int srcY = (r - 3) * size; // Décalage car emptyMaze commence à la ligne 3

                        using (Bitmap currentTile = emptyMaze.Clone(new Rectangle(srcX, srcY, size, size), emptyMaze.PixelFormat))
                        {
                            // Comparer avec la banque de tuiles bleues
                            for (int tileID = 0; tileID < 48; tileID++)
                            {
                                if (CompareBitmaps(currentTile, bank[tileID]))
                                {
                                    map.VisualGrid[r, c] = tileID; // On a trouvé l'index exact !
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Nettoyage de la mémoire
            for (int i = 0; i < 48; i++) bank[i].Dispose();
            emptyMaze.Dispose();
        }

        // Méthode utilitaire rapide pour comparer deux Bitmaps de 8x8 pixels
        private bool CompareBitmaps(Bitmap bmp1, Bitmap bmp2)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (bmp1.GetPixel(x, y).ToArgb() != bmp2.GetPixel(x, y).ToArgb())
                        return false;
                }
            }
            return true;
        }
    }
}