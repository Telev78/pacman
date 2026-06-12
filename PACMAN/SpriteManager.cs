using System;
using System.Collections.Generic;
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
        private Bitmap ghostDoorSprite;
        private Bitmap[] pacmanDeathSprites;
        //private Dictionary<Keys, Bitmap[]> blinkySprites;
        private Dictionary<string, Dictionary<Keys, Bitmap[]>> ghostSprites;
        private Bitmap[] frightenedBlueSprites;
        private Bitmap[] frightenedWhiteSprites; // Utile pour la fin du mode panic !
        private Dictionary<Keys, Bitmap> eyesSprites;

        public SpriteManager()
        {
            string assetPath = Path.Combine(Application.StartupPath, "asset");
            generalSheet = new Bitmap(Path.Combine(assetPath, "general.png"));
            mazeSheet = new Bitmap(Path.Combine(assetPath, "maze.png"));
            textSheet = new Bitmap(Path.Combine(assetPath, "text.png"));

            int doorWidth = 16;
            int doorHeight = 8;
            ghostDoorSprite = new Bitmap(doorWidth, doorHeight);
            using (Graphics g = Graphics.FromImage(ghostDoorSprite))
            {
                // X = 228 (début du maze vide) + 104 (colonne 13 * 8 px) = 332
                // Y = 96 (ligne 12 du maze physique * 8 px) = 96
                g.DrawImage(generalSheet,
                    new Rectangle(0, 0, doorWidth, doorHeight),
                    new Rectangle(332, 96, doorWidth, doorHeight),
                    GraphicsUnit.Pixel);
            }

            ghostSprites = new Dictionary<string, Dictionary<Keys, Bitmap[]>>();

            string[] ghostNames = { "Blinky", "Pinky", "Inky", "Clyde" };
            Keys[] dirOrder = { Keys.Right, Keys.Left, Keys.Up, Keys.Down };

            int spriteSize = 16;
            int startX = 456; // Ton offset horizontal validé
            int startY = 64;  // Ligne de Blinky

            for (int i = 0; i < ghostNames.Length; i++)
            {
                var currentGhostDict = new Dictionary<Keys, Bitmap[]>();
                // Chaque fantôme est sur la ligne immédiatement inférieure (offset de 16 pixels verticaux)
                int currentGhostY = startY + (i * spriteSize);

                for (int d = 0; d < dirOrder.Length; d++)
                {
                    Bitmap[] frames = new Bitmap[2];
                    for (int f = 0; f < 2; f++)
                    {
                        frames[f] = new Bitmap(spriteSize, spriteSize);
                        using (Graphics g = Graphics.FromImage(frames[f]))
                        {
                            int sheetX = startX + (d * 2 + f) * spriteSize;
                            g.DrawImage(generalSheet,
                                new Rectangle(0, 0, spriteSize, spriteSize),
                                new Rectangle(sheetX, currentGhostY, spriteSize, spriteSize),
                                GraphicsUnit.Pixel);
                        }
                    }
                    currentGhostDict.Add(dirOrder[d], frames);
                }
                ghostSprites.Add(ghostNames[i], currentGhostDict);
            }

            // --- CHARGEMENT DES SPRITES DE PANIQUE ET DES YEUX ---
            frightenedBlueSprites = new Bitmap[2];
            frightenedWhiteSprites = new Bitmap[2];
            int fY = 64; // Situé juste sur la ligne de Blinky 
            startX = 456 + (8 * spriteSize); //Tout à droite des 8 sprites de Blinky
            // 1. Fantôme effrayé Bleu (2 frames d'animation)
            for (int f = 0; f < 2; f++)
            {
                frightenedBlueSprites[f] = new Bitmap(spriteSize, spriteSize);
                using (Graphics g = Graphics.FromImage(frightenedBlueSprites[f]))
                {
                    g.DrawImage(generalSheet,
                        new Rectangle(0, 0, spriteSize, spriteSize),
                        new Rectangle(startX + (f * spriteSize), fY, spriteSize, spriteSize),
                        GraphicsUnit.Pixel);
                }
            }

            // 2. Fantôme effrayé Blanc/Bleu clignotant (2 frames d'animation)
            fY = 64; // Situé juste sur la ligne de Blinky 
            startX = 456 + (8 * spriteSize); 
            for (int f = 0; f < 2; f++)
            {
                frightenedWhiteSprites[f] = new Bitmap(spriteSize, spriteSize);
                using (Graphics g = Graphics.FromImage(frightenedWhiteSprites[f]))
                {
                    g.DrawImage(generalSheet,
                        new Rectangle(0, 0, spriteSize, spriteSize),
                        new Rectangle(startX + ((2 + f) * spriteSize), fY, spriteSize, spriteSize),
                        GraphicsUnit.Pixel);
                }
            }

            // 3. Yeux seuls pour le retour au nid (4 directions : Droite, Gauche, Haut, Bas)
            eyesSprites = new Dictionary<Keys, Bitmap>();
            Keys[] eyeDirs = { Keys.Right, Keys.Left, Keys.Up, Keys.Down };
            for (int d = 0; d < eyeDirs.Length; d++)
            {
                Bitmap eyeBmp = new Bitmap(spriteSize, spriteSize);
                using (Graphics g = Graphics.FromImage(eyeBmp))
                {
                    // Les yeux commencent en dessous des 4 sprites de panique 
                    int eyeY = 80;
                    int eyeX = 456 + ((8 + d) * spriteSize);
                    g.DrawImage(generalSheet,
                        new Rectangle(0, 0, spriteSize, spriteSize),
                        new Rectangle(eyeX, eyeY, spriteSize, spriteSize),
                        GraphicsUnit.Pixel);
                }
                eyesSprites.Add(eyeDirs[d], eyeBmp);
            }

            pacmanDeathSprites = new Bitmap[12];
            spriteSize = 16;
            startX = 456 + (3*16); 
            startY = 0;

            for (int f = 0; f < 12; f++)
            {
                pacmanDeathSprites[f] = new Bitmap(spriteSize, spriteSize);
                using (Graphics g = Graphics.FromImage(pacmanDeathSprites[f]))
                {
                    g.DrawImage(generalSheet,
                        new Rectangle(0, 0, spriteSize, spriteSize),
                        // Les frames sont généralement alignées horizontalement
                        new Rectangle(startX + (f * spriteSize), startY, spriteSize, spriteSize),
                        GraphicsUnit.Pixel);
                }
            }
        }
        // Nouvelle méthode de récupération universelle
        public Bitmap GetGhostSprite(string ghostName, Keys direction, int frame)
        {
            if (ghostSprites.ContainsKey(ghostName) && ghostSprites[ghostName].ContainsKey(direction))
            {
                return ghostSprites[ghostName][direction][frame % 2];
            }
            return ghostSprites["Blinky"][Keys.Left][0]; // Repli de secours
        }

        public Bitmap GetGhostDoorSprite()
        {
            return ghostDoorSprite;
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

        // Méthode publique pour récupérer la frame d'animation de mort
        public Bitmap GetPacmanDeathSprite(int frame)
        {
            // Sécurité pour ne pas dépasser le tableau
            if (frame >= 0 && frame < 12)
                return pacmanDeathSprites[frame];

            // Si on dépasse, on renvoie une frame vide (Pac-Man a disparu)
            Bitmap empty = new Bitmap(16, 16);
            return empty;
        }

        public Bitmap GetGhostFrightenedSprite(int frame, bool flashWhite = false)
        {
            if (flashWhite)
                return frightenedWhiteSprites[frame % 2];
            return frightenedBlueSprites[frame % 2];
        }

        public Bitmap GetGhostEyesSprite(Keys direction)
        {
            if (eyesSprites.ContainsKey(direction))
                return eyesSprites[direction];
            return frightenedBlueSprites[0]; // Sécurité
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

        /// <summary>
        /// Extrait un caractère 8x8 depuis text.png.
        /// Index des couleurs (colorSet) : 0=Blanc, 1=Rouge, 2=Rose, 3=Cyan, 4=Orange, 5=Chair, 6=Jaune
        /// </summary>
        public Bitmap GetArcadeCharacter(char c, int colorSet = 6) // 6 = Jaune par défaut pour READY!
        {
            int rowOffset = 0;
            int col = 0;
            char upperC = char.ToUpper(c);

            // Ligne 1 du bloc de couleur : A-O
            if (upperC >= 'A' && upperC <= 'O')
            {
                rowOffset = 0;
                col = upperC - 'A';
            }
            // Ligne 2 du bloc de couleur : P-Z, !, ©
            else if (upperC >= 'P' && upperC <= 'Z')
            {
                rowOffset = 1;
                col = upperC - 'P';
            }
            else if (upperC == '!')
            {
                rowOffset = 1;
                col = 11;
            }
            else if (upperC == '©')
            {
                rowOffset = 1;
                col = 12;
            }
            // Ligne 3 du bloc de couleur : 0-9, /, -, "
            else if (upperC >= '0' && upperC <= '9')
            {
                rowOffset = 2;
                col = upperC - '0';
            }
            else if (upperC == '/')
            {
                rowOffset = 2;
                col = 10;
            }
            else if (upperC == '-')
            {
                rowOffset = 2;
                col = 11;
            }
            else if (upperC == '"')
            {
                rowOffset = 2;
                col = 12;
            }
            else
            {
                // Espace ou caractère inconnu -> renvoie un carré transparent vide de 8x8
                return new Bitmap(8, 8);
            }

            // Chaque bloc de couleur fait exactement 4 lignes de 8 pixels de haut (= 32 pixels)
            int pixelX = col * 8;
            int pixelY = ((colorSet * 4) + rowOffset) * 8;

            Rectangle srcRect = new Rectangle(pixelX, pixelY, 8, 8);
            return textSheet.Clone(srcRect, textSheet.PixelFormat);
        }

        /// <summary>
        /// Extrait le glyphe combiné "PTS" (17x8 pixels) à la fin de la 2ème ligne d'un bloc de couleur.
        /// </summary>
        public Bitmap GetPtsSprite(int colorSet = 6)
        {
            int localRow = 1;    // 2ème ligne du sous-bloc de couleur
            int startCol = 13;   // Commence juste après le '©' (13 * 8 = 104 pixels)

            int pixelX = startCol * 8;
            int pixelY = ((colorSet * 4) + localRow) * 8;

            int width = 17;      // Correction : Largeur exacte en pixels
            int height = 8;

            Rectangle srcRect = new Rectangle(pixelX, pixelY, width, height);
            return textSheet.Clone(srcRect, textSheet.PixelFormat);
        }

        /// <summary>
        /// Extrait le logo complet "NAMCO" (50x8 pixels) sur la 4ème ligne d'un bloc de couleur.
        /// </summary>
        public Bitmap GetNamcoLogo(int colorSet = 1)
        {
            int localRow = 3;    // 4ème ligne du sous-bloc de couleur
            int startCol = 0;    // Commence tout au début de la ligne (0 pixel)

            int pixelX = startCol * 8;
            int pixelY = ((colorSet * 4) + localRow) * 8;

            int width = 50;      // Correction : Largeur exacte en pixels
            int height = 8;

            Rectangle srcRect = new Rectangle(pixelX, pixelY, width, height);
            return textSheet.Clone(srcRect, textSheet.PixelFormat);
        }
    }
}