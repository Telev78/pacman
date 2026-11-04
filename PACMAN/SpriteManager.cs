using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
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
        private Dictionary<string, Dictionary<Keys, Bitmap[]>> ghostSprites;
        private Bitmap[] frightenedBlueSprites;
        private Bitmap[] frightenedWhiteSprites; // Utile pour la fin du mode panic !
        private Dictionary<Keys, Bitmap> eyesSprites;

        public enum ArcadeColor
        {
            White = 0,
            Red = 1,
            Pink = 2,
            Cyan = 3,
            Orange = 4,
            Flesh = 5,  // Couleur chair
            Yellow = 6
        }

        private Bitmap LoadEmbeddedBitmap(string resourcePath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            // Format cible : PacmanWinForms.asset.general.png
            string fullResourceName = $"PacmanWinForms.{resourcePath}";

            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Ressource image embarquée introuvable : {fullResourceName}");
                }
                return new Bitmap(stream);
            }
        }

        // Découpe un rectangle de general.png dans un nouveau Bitmap
        private Bitmap CropGeneral(int x, int y, int width = 16, int height = 16)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(generalSheet,
                    new Rectangle(0, 0, width, height),
                    new Rectangle(x, y, width, height),
                    GraphicsUnit.Pixel);
            }
            return bmp;
        }

        public SpriteManager()
        {
            generalSheet = LoadEmbeddedBitmap("asset.general.png");
            mazeSheet = LoadEmbeddedBitmap("asset.maze.png");
            textSheet = LoadEmbeddedBitmap("asset.text.png");

            generalSheet.MakeTransparent(Color.Black);

            const int SpriteSize = 16;
            const int SpritesX = 456; // Début des sprites de personnages dans general.png

            // Porte du nid (16x8)
            ghostDoorSprite = CropGeneral(332, 96, 16, 8);

            // Fantômes : 4 directions x 2 frames, une ligne de 16 px par fantôme
            ghostSprites = new Dictionary<string, Dictionary<Keys, Bitmap[]>>();
            string[] ghostNames = { "Blinky", "Pinky", "Inky", "Clyde" };
            Keys[] dirOrder = { Keys.Right, Keys.Left, Keys.Up, Keys.Down };

            for (int i = 0; i < ghostNames.Length; i++)
            {
                var currentGhostDict = new Dictionary<Keys, Bitmap[]>();
                int y = 64 + (i * SpriteSize);

                for (int d = 0; d < dirOrder.Length; d++)
                {
                    Bitmap[] frames = new Bitmap[2];
                    for (int f = 0; f < 2; f++)
                    {
                        frames[f] = CropGeneral(SpritesX + (d * 2 + f) * SpriteSize, y);
                    }
                    currentGhostDict.Add(dirOrder[d], frames);
                }
                ghostSprites.Add(ghostNames[i], currentGhostDict);
            }

            // Fantômes effrayés : à droite des 8 sprites de Blinky (2 bleus puis 2 blancs)
            int frightX = SpritesX + (8 * SpriteSize);
            frightenedBlueSprites = new Bitmap[2];
            frightenedWhiteSprites = new Bitmap[2];
            for (int f = 0; f < 2; f++)
            {
                frightenedBlueSprites[f] = CropGeneral(frightX + f * SpriteSize, 64);
                frightenedWhiteSprites[f] = CropGeneral(frightX + (2 + f) * SpriteSize, 64);
            }

            // Yeux (Droite, Gauche, Haut, Bas) : ligne en dessous
            eyesSprites = new Dictionary<Keys, Bitmap>();
            Keys[] eyeDirs = { Keys.Right, Keys.Left, Keys.Up, Keys.Down };
            for (int d = 0; d < eyeDirs.Length; d++)
            {
                eyesSprites.Add(eyeDirs[d], CropGeneral(SpritesX + (8 + d) * SpriteSize, 80));
            }

            // Animation de mort de Pac-Man (12 frames alignées horizontalement)
            pacmanDeathSprites = new Bitmap[12];
            for (int f = 0; f < 12; f++)
            {
                pacmanDeathSprites[f] = CropGeneral(SpritesX + (3 + f) * SpriteSize, 0);
            }
        }

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
        public Bitmap GetMazeTile(int sheetColumn, int sheetRow, MazeColor color = MazeColor.Blue)
        {
            int size = GameConfig.TileSize; // 8
            sheetRow = sheetRow + ((int)color*3);

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

        public enum MazeColor
        {  
            Red = 0,
            Blue = 1, 
            White = 2
        }

        /// <summary>
        /// Extrait un caractère 8x8 depuis text.png en utilisant la couleur spécifiée.
        /// </summary>
        public Bitmap GetArcadeCharacter(char c, ArcadeColor color = ArcadeColor.Yellow) // Jaune par défaut
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

            // REFACTORING : On cast l'enum en entier pour le calcul mathématique de l'offset Y
            int pixelY = (((int)color * 4) + rowOffset) * 8;

            Rectangle srcRect = new Rectangle(pixelX, pixelY, 8, 8);
            return textSheet.Clone(srcRect, textSheet.PixelFormat);
        }

        /// <summary>
        /// Extrait le glyphe combiné "PTS" (17x8 pixels) à la fin de la 2ème ligne d'un bloc de couleur.
        /// </summary>
        public Bitmap GetPtsSprite(ArcadeColor color = ArcadeColor.Yellow)
        {
            int localRow = 1;    // 2ème ligne du sous-bloc de couleur
            int startCol = 13;   // Comme avant

            int pixelX = startCol * 8;
            int pixelY = (((int)color * 4) + localRow) * 8; // REFACTORING ici

            int width = 17;
            int height = 8;

            Rectangle srcRect = new Rectangle(pixelX, pixelY, width, height);
            return textSheet.Clone(srcRect, textSheet.PixelFormat);
        }

        /// <summary>
        /// Extrait le logo complet "NAMCO" (50x8 pixels) sur la 4ème ligne d'un bloc de couleur.
        /// </summary>
        public Bitmap GetNamcoLogo(ArcadeColor color = ArcadeColor.Red) // Rouge réglementaire par défaut
        {
            int localRow = 3;    // 4ème ligne du sous-bloc de couleur
            int startCol = 0;

            int pixelX = startCol * 8;
            int pixelY = (((int)color * 4) + localRow) * 8; // REFACTORING ici

            int width = 50;
            int height = 8;

            Rectangle srcRect = new Rectangle(pixelX, pixelY, width, height);
            return textSheet.Clone(srcRect, textSheet.PixelFormat);
        }

        /// <summary>
        /// Extrait le sprite de score stylisé d'origine (200, 400, 800, 1600) 
        /// depuis la generalSheet (Ligne 9) lorsqu'un fantôme est mangé.
        /// </summary>
        public Bitmap GetGhostPointsSprite(int points)
        {
            int colIndex = 0;

            switch (points)
            {
                case 200: colIndex = 0; break; // Case 1
                case 400: colIndex = 1; break; // Case 2
                case 800: colIndex = 2; break; // Case 3
                case 1600: colIndex = 3; break; // Case 4
                default:
                    return new Bitmap(16, 16); // Sécurité
            }

            // Ajustement des coordonnées sur la generalSheet
            // Si tes sprites de la ligne 9 font 16x16 pixels :
            int spriteSize = 16;
            int pixelX = 456+(colIndex * spriteSize);
            int pixelY = 8 * spriteSize; // 9ème ligne = index 8

            // Note : Si dans ta texture le sprite fait 16x8 au lieu de 16x16,
            // tu pourras ajuster la hauteur (height) à 8 et le pixelY si nécessaire.
            Rectangle srcRect = new Rectangle(pixelX, pixelY, spriteSize, spriteSize);

            return generalSheet.Clone(srcRect, generalSheet.PixelFormat);
        }

        /// <summary>
        /// Extrait le sprite du fruit correspondant au niveau actuel
        /// depuis la generalSheet (Ligne 5, à côté de Pac-Man bas).
        /// </summary>
        public Bitmap GetFruitSprite(int level)
        {
            int fruitIndex = 0; // 0: Cerise, 1: Fraise, 2: Pêche, 3: Pomme, 4: Raisin, 5: Galaxian, 6: Cloche, 7: Clé

            if (level == 1) fruitIndex = 0;
            else if (level == 2) fruitIndex = 1;
            else if (level == 3 || level == 4) fruitIndex = 2;
            else if (level == 5 || level == 6) fruitIndex = 3;
            else if (level == 7 || level == 8) fruitIndex = 4;
            else if (level == 9 || level == 10) fruitIndex = 5;
            else if (level == 11 || level == 12) fruitIndex = 6;
            else fruitIndex = 7; // Niveau 13 et plus

            int spriteSize = 16;
            int colIndex = 2 + fruitIndex; // Saute les 2 sprites de Pac-Man orienté vers le bas

            int pixelX = 456 + (colIndex * spriteSize);
            int pixelY = 3 * spriteSize; // 4ème ligne = index 3

            Rectangle srcRect = new Rectangle(pixelX, pixelY, spriteSize, spriteSize);
            return generalSheet.Clone(srcRect, generalSheet.PixelFormat);
        }

        /// <summary>
        /// Extrait le sprite de score stylisé d'origine pour les fruits (100 à 5000)
        /// depuis la generalSheet (Ligne index 9, Y=144) en gérant les largeurs variables et l'alignement.
        /// </summary>
        public Bitmap GetFruitPointsSprite(int points)
        {
            int pixelX = 456;
            int pixelY = 144; // Ligne index 9
            int width = 16;
            int height = 16;

            switch (points)
            {
                case 100: pixelX += 0 * 16; width = 16; break;
                case 300: pixelX += 1 * 16; width = 16; break;
                case 500: pixelX += 2 * 16; width = 16; break;
                case 700: pixelX += 3 * 16; width = 16; break;

                case 1000:
                    pixelX += 4 * 16; // 520
                    width = 18;       // 18 pixels de large (déborde à droite)
                    break;

                case 2000:
                    pixelX += (4 * 16) - 3; // 517 (3 pixels plus tôt à gauche)
                    pixelY += 1 * 16;       // 1 ligne en dessous (160)
                    width = 21;             // 21 pixels de large
                    break;

                case 3000:
                    pixelX += (4 * 16) - 3; // 517
                    pixelY += 2 * 16;       // 2 lignes en dessous (176)
                    width = 21;
                    break;

                case 5000:
                    pixelX += (4 * 16) - 3; // 517
                    pixelY += 3 * 16;       // 3 lignes en dessous (192)
                    width = 21;
                    break;

                default:
                    return new Bitmap(16, 16); // Sécurité
            }

            Rectangle srcRect = new Rectangle(pixelX, pixelY, width, height);
            return generalSheet.Clone(srcRect, generalSheet.PixelFormat);
        }
    }
}