using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace PacmanWinForms
{
    /// <summary>
    /// Découpe les sprites de la feuille unique (asset.png) et les met en cache.
    /// IMPORTANT : les Bitmap renvoyés sont partagés. Ne jamais les Dispose ni les modifier.
    /// </summary>
    public class SpriteManager : IDisposable
    {
        private const int SpriteSize = 16;
        private const int SpritesX = 0;    // Début des sprites de personnages dans la feuille
        private const int SpritesY = 81;
        private const int TextY = 292;     // Début des blocs de texte dans la feuille

        private Bitmap assetSheet;
        private Bitmap ghostDoorSprite;
        private Bitmap emptySprite16;
        private Bitmap emptyChar;
        private Bitmap[] pacmanDeathSprites;
        private Dictionary<GhostType, Dictionary<Direction, Bitmap[]>> ghostSprites;
        private Dictionary<Direction, Bitmap> eyesSprites;
        private Bitmap[] frightenedBlueSprites;
        private Bitmap[] frightenedWhiteSprites;
        private Bitmap lifeSprite;
        private Bitmap[] giantPacmanSprites;

        // Caches (remplis à la demande)
        private readonly Dictionary<(int, Direction), Bitmap> pacmanSprites = new Dictionary<(int, Direction), Bitmap>();
        private readonly Dictionary<(int, int, MazeColor), Bitmap> mazeTiles = new Dictionary<(int, int, MazeColor), Bitmap>();
        private readonly Dictionary<MazeColor, Bitmap> mazeLayers = new Dictionary<MazeColor, Bitmap>();
        private readonly Dictionary<(char, ArcadeColor), Bitmap> charSprites = new Dictionary<(char, ArcadeColor), Bitmap>();
        private readonly Dictionary<ArcadeColor, Bitmap> ptsSprites = new Dictionary<ArcadeColor, Bitmap>();
        private readonly Dictionary<ArcadeColor, Bitmap> namcoLogos = new Dictionary<ArcadeColor, Bitmap>();
        private readonly Dictionary<int, Bitmap> ghostPointsSprites = new Dictionary<int, Bitmap>();
        private readonly Dictionary<int, Bitmap> fruitSprites = new Dictionary<int, Bitmap>();
        private readonly Dictionary<int, Bitmap> fruitPointsSprites = new Dictionary<int, Bitmap>();

        private readonly Dictionary<(int, int), Bitmap> sheetSprites = new Dictionary<(int, int), Bitmap>();

        /// <summary>
        /// Sprite 16x16 de la feuille aux coordonnées (x, y), mis en cache (intermissions).
        /// </summary>
        public Bitmap GetSheetSprite(int x, int y)
        {
            Bitmap sprite;
            if (!sheetSprites.TryGetValue((x, y), out sprite))
            {
                sprite = CropAsset(x, y);
                sheetSprites[(x, y)] = sprite;
            }
            return sprite;
        }

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

        public enum MazeColor
        {
            Red = 0,
            Blue = 1,
            White = 2
        }

        private Bitmap LoadEmbeddedBitmap(string resourcePath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            // Format cible : PacmanWinForms.asset.asset.png
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

        // Découpe un rectangle de la feuille dans un nouveau Bitmap
        private Bitmap CropAsset(int x, int y, int width = 16, int height = 16)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(assetSheet,
                    new Rectangle(0, 0, width, height),
                    new Rectangle(x, y, width, height),
                    GraphicsUnit.Pixel);
            }
            return bmp;
        }

        public SpriteManager()
        {
            assetSheet = LoadEmbeddedBitmap("asset.asset.png");
            assetSheet.MakeTransparent(Color.Black);

            emptySprite16 = new Bitmap(SpriteSize, SpriteSize);
            emptyChar = new Bitmap(8, 8);

            // Porte du nid (8x8, étirée en 16x8 au dessin)
            ghostDoorSprite = CropAsset(144, 72, 8, 8);

            // Fantômes : 4 directions x 2 frames, une ligne de 16 px par fantôme (dans l'ordre de GhostType)
            ghostSprites = new Dictionary<GhostType, Dictionary<Direction, Bitmap[]>>();
            Direction[] dirOrder = { Direction.Right, Direction.Left, Direction.Up, Direction.Down };

            foreach (GhostType type in Enum.GetValues(typeof(GhostType)))
            {
                var currentGhostDict = new Dictionary<Direction, Bitmap[]>();
                int y = SpritesY + 64 + ((int)type * SpriteSize);

                for (int d = 0; d < dirOrder.Length; d++)
                {
                    Bitmap[] frames = new Bitmap[2];
                    for (int f = 0; f < 2; f++)
                    {
                        frames[f] = CropAsset(SpritesX + (d * 2 + f) * SpriteSize, y);
                    }
                    currentGhostDict.Add(dirOrder[d], frames);
                }
                ghostSprites.Add(type, currentGhostDict);
            }

            // Fantômes effrayés : à droite des 8 sprites de Blinky (2 bleus puis 2 blancs)
            int frightX = SpritesX + (8 * SpriteSize);
            frightenedBlueSprites = new Bitmap[2];
            frightenedWhiteSprites = new Bitmap[2];
            for (int f = 0; f < 2; f++)
            {
                frightenedBlueSprites[f] = CropAsset(frightX + f * SpriteSize, SpritesY + 64);
                frightenedWhiteSprites[f] = CropAsset(frightX + (2 + f) * SpriteSize, SpritesY + 64);
            }

            // Yeux (Droite, Gauche, Haut, Bas)
            eyesSprites = new Dictionary<Direction, Bitmap>();
            Direction[] eyeDirs = { Direction.Right, Direction.Left, Direction.Up, Direction.Down };
            for (int d = 0; d < eyeDirs.Length; d++)
            {
                eyesSprites.Add(eyeDirs[d], CropAsset(SpritesX + (8 + d) * SpriteSize, SpritesY + 80));
            }

            // Animation de mort de Pac-Man (12 frames alignées horizontalement)
            pacmanDeathSprites = new Bitmap[12];
            for (int f = 0; f < 12; f++)
            {
                pacmanDeathSprites[f] = CropAsset(SpritesX + (3 + f) * SpriteSize, SpritesY);
            }
        }

        // ------------------------------------------------------------------
        // Personnages
        // ------------------------------------------------------------------

        public Bitmap GetGhostSprite(GhostType type, Direction direction, int frame)
        {
            if (ghostSprites.ContainsKey(type) && ghostSprites[type].ContainsKey(direction))
            {
                return ghostSprites[type][direction][frame % 2];
            }
            return ghostSprites[GhostType.Blinky][Direction.Left][0]; // Repli de secours
        }

        public Bitmap GetGhostFrightenedSprite(int frame, bool flashWhite = false)
        {
            if (flashWhite)
                return frightenedWhiteSprites[frame % 2];
            return frightenedBlueSprites[frame % 2];
        }

        public Bitmap GetGhostEyesSprite(Direction direction)
        {
            if (eyesSprites.ContainsKey(direction))
                return eyesSprites[direction];
            return frightenedBlueSprites[0]; // Sécurité
        }

        /// <summary>
        /// Sprite de Pac-Man selon sa frame d'animation et sa direction.
        /// La frame 3 (bouche fermée) est partagée par toutes les directions.
        /// </summary>
        public Bitmap GetPacmanSprite(int animFrame, Direction direction)
        {
            Direction cacheDirection = (animFrame == 3) ? Direction.None : direction;
            var key = (animFrame, cacheDirection);

            Bitmap sprite;
            if (pacmanSprites.TryGetValue(key, out sprite))
                return sprite;

            int startX = 0;
            int startY = SpritesY;

            if (animFrame == 3)
            {
                startX = 32; // Bouche complètement fermée
            }
            else
            {
                switch (direction)
                {
                    case Direction.Right: startY += 0; break;
                    case Direction.Left: startY += 16; break;
                    case Direction.Up: startY += 32; break;
                    case Direction.Down: startY += 48; break;
                    default: startY += 16; break; // Gauche par défaut au spawn
                }

                // animFrame 1 = bouche grande ouverte
                if (animFrame == 1)
                {
                    startX += SpriteSize;
                }
            }

            sprite = CropAsset(startX, startY);
            pacmanSprites[key] = sprite;
            return sprite;
        }

        public Bitmap GetPacmanDeathSprite(int frame)
        {
            if (frame >= 0 && frame < 12)
                return pacmanDeathSprites[frame];

            // Au-delà de l'animation, Pac-Man a disparu
            return emptySprite16;
        }

        // ------------------------------------------------------------------
        // Labyrinthe
        // ------------------------------------------------------------------

        /// <summary>
        /// Tuile 8x8 de la feuille du labyrinthe selon sa colonne et sa ligne.
        /// </summary>
        public Bitmap GetMazeTile(int sheetColumn, int sheetRow, MazeColor color = MazeColor.Blue)
        {
            var key = (sheetColumn, sheetRow, color);

            Bitmap tile;
            if (mazeTiles.TryGetValue(key, out tile))
                return tile;

            int size = GameConfig.TileSize;
            int row = sheetRow + ((int)color * 3);

            tile = CropAsset((sheetColumn * size) + sheetColumn, (row * size) + row, size, size);
            mazeTiles[key] = tile;
            return tile;
        }

        public Bitmap GetDotSprite()
        {
            return GetMazeTile(13, 2);
        }

        public Bitmap GetEnergizerSprite()
        {
            return GetMazeTile(15, 2);
        }

        /// <summary>
        /// Icône de la réserve de vies (HUD du bas), située en (128, 97) dans la feuille de sprites.
        /// </summary>
        public Bitmap GetLifeSprite()
        {
            if (lifeSprite == null)
            {
                lifeSprite = CropAsset(128, 97);
            }
            return lifeSprite;
        }
        public Bitmap GetGhostDoorSprite()
        {
            return ghostDoorSprite;
        }

        /// <summary>
        /// Image pré-dessinée du labyrinthe (murs et porte du nid, sans les gommes), à l'échelle native 224x288.
        /// Construite une seule fois par couleur : les murs ne changent jamais.
        /// </summary>
        public Bitmap GetMazeLayer(LevelMap map, MazeColor color)
        {
            Bitmap layer;
            if (mazeLayers.TryGetValue(color, out layer))
                return layer;

            int tile = GameConfig.TileSize;
            layer = new Bitmap(GameConfig.MapWidth * tile, GameConfig.MapHeight * tile);

            using (Graphics g = Graphics.FromImage(layer))
            {
                for (int r = 0; r < GameConfig.MapHeight; r++)
                {
                    for (int c = 0; c < GameConfig.MapWidth; c++)
                    {
                        int tileID = map.VisualGrid[r, c];
                        if (tileID != -1)
                        {
                            Bitmap tileSprite = GetMazeTile(tileID % 16, tileID / 16, color);
                            g.DrawImage(tileSprite, new Rectangle(c * tile, r * tile, tile, tile),
                                0, 0, tile, tile, GraphicsUnit.Pixel);
                        }

                        // Porte du nid : un sprite 8x8 étiré sur 16x8 (deux colonnes)
                        if (map.Grid[r, c] == TileType.GhostDoor && c == 13)
                        {
                            g.DrawImage(ghostDoorSprite, new Rectangle(c * tile, r * tile, 16, 8),
                                0, 0, ghostDoorSprite.Width, ghostDoorSprite.Height, GraphicsUnit.Pixel);
                        }
                    }
                }
            }

            mazeLayers[color] = layer;
            return layer;
        }

        // ------------------------------------------------------------------
        // Texte
        // ------------------------------------------------------------------

        /// <summary>
        /// Caractère 8x8 de la feuille de texte dans la couleur demandée.
        /// </summary>
        public Bitmap GetArcadeCharacter(char c, ArcadeColor color = ArcadeColor.Yellow)
        {
            char upperC = char.ToUpper(c);
            var key = (upperC, color);

            Bitmap sprite;
            if (charSprites.TryGetValue(key, out sprite))
                return sprite;

            int rowOffset;
            int col;

            // Ligne 1 du bloc de couleur : A-O
            if (upperC >= 'A' && upperC <= 'O') { rowOffset = 0; col = upperC - 'A'; }
            // Ligne 2 du bloc de couleur : P-Z, !, ©
            else if (upperC >= 'P' && upperC <= 'Z') { rowOffset = 1; col = upperC - 'P'; }
            else if (upperC == '!') { rowOffset = 1; col = 11; }
            else if (upperC == '©') { rowOffset = 1; col = 12; }
            // Ligne 3 du bloc de couleur : 0-9, /, -, "
            else if (upperC >= '0' && upperC <= '9') { rowOffset = 2; col = upperC - '0'; }
            else if (upperC == '/') { rowOffset = 2; col = 10; }
            else if (upperC == '-') { rowOffset = 2; col = 11; }
            else if (upperC == '"') { rowOffset = 2; col = 12; }
            else if (upperC == '.')
            {
                sprite = CreateDotGlyph(color);
                charSprites[key] = sprite;
                return sprite;
            }
            else
            {
                // Espace ou caractère inconnu : carré transparent
                return emptyChar;
            }

            // Chaque bloc de couleur fait 4 lignes de 8 pixels
            sprite = CropAsset(col * 8, TextY + ((((int)color * 4) + rowOffset) * 8), 8, 8);
            charSprites[key] = sprite;
            return sprite;
        }

        // Le point n'existe pas dans la feuille : 2x2 pixels en bas à gauche de la tuile, de la couleur du texte
        private Bitmap CreateDotGlyph(ArcadeColor color)
        {
            Bitmap reference = GetArcadeCharacter('A', color);
            Color ink = Color.White;
            for (int y = 0; y < reference.Height; y++)
            {
                for (int x = 0; x < reference.Width; x++)
                {
                    Color p = reference.GetPixel(x, y);
                    if (p.A > 0) { ink = p; y = reference.Height; break; }
                }
            }

            Bitmap dot = new Bitmap(8, 8);
            for (int y = 5; y <= 6; y++)
                for (int x = 1; x <= 2; x++)
                    dot.SetPixel(x, y, ink);
            return dot;
        }

        /// <summary>
        /// Glyphe combiné "PTS" (17x8) à la fin de la 2ème ligne d'un bloc de couleur.
        /// </summary>
        public Bitmap GetPtsSprite(ArcadeColor color = ArcadeColor.Yellow)
        {
            Bitmap sprite;
            if (ptsSprites.TryGetValue(color, out sprite))
                return sprite;

            int localRow = 1;    // 2ème ligne du sous-bloc de couleur
            int startCol = 13;

            sprite = CropAsset(startCol * 8, TextY + ((((int)color * 4) + localRow) * 8), 17, 8);
            ptsSprites[color] = sprite;
            return sprite;
        }

        /// <summary>
        /// Logo "NAMCO" (50x8) sur la 4ème ligne d'un bloc de couleur.
        /// </summary>
        public Bitmap GetNamcoLogo(ArcadeColor color = ArcadeColor.Red)
        {
            Bitmap sprite;
            if (namcoLogos.TryGetValue(color, out sprite))
                return sprite;

            int localRow = 3;    // 4ème ligne du sous-bloc de couleur
            int startCol = 0;

            sprite = CropAsset(startCol * 8, TextY + ((((int)color * 4) + localRow) * 8), 50, 8);
            namcoLogos[color] = sprite;
            return sprite;
        }

        // ------------------------------------------------------------------
        // Scores et fruits
        // ------------------------------------------------------------------

        /// <summary>
        /// Score stylisé affiché quand un fantôme est mangé (200, 400, 800, 1600).
        /// </summary>
        public Bitmap GetGhostPointsSprite(int points)
        {
            Bitmap sprite;
            if (ghostPointsSprites.TryGetValue(points, out sprite))
                return sprite;

            int colIndex;
            switch (points)
            {
                case 200: colIndex = 0; break;
                case 400: colIndex = 1; break;
                case 800: colIndex = 2; break;
                case 1600: colIndex = 3; break;
                default: return emptySprite16;
            }

            sprite = CropAsset(colIndex * SpriteSize, SpritesY + (8 * SpriteSize));
            ghostPointsSprites[points] = sprite;
            return sprite;
        }

        /// <summary>
        /// Sprite du fruit correspondant au niveau.
        /// </summary>
        public Bitmap GetFruitSprite(int level)
        {
            int fruitIndex; // 0: Cerise, 1: Fraise, 2: Pêche, 3: Pomme, 4: Melon, 5: Galaxian, 6: Cloche, 7: Clé

            if (level == 1) fruitIndex = 0;
            else if (level == 2) fruitIndex = 1;
            else if (level == 3 || level == 4) fruitIndex = 2;
            else if (level == 5 || level == 6) fruitIndex = 3;
            else if (level == 7 || level == 8) fruitIndex = 4;
            else if (level == 9 || level == 10) fruitIndex = 5;
            else if (level == 11 || level == 12) fruitIndex = 6;
            else fruitIndex = 7; // Niveau 13 et plus

            Bitmap sprite;
            if (fruitSprites.TryGetValue(fruitIndex, out sprite))
                return sprite;

            int colIndex = 2 + fruitIndex; // Saute les 2 sprites de Pac-Man orienté vers le bas
            sprite = CropAsset(colIndex * SpriteSize, SpritesY + (3 * SpriteSize));
            fruitSprites[fruitIndex] = sprite;
            return sprite;
        }

        /// <summary>
        /// Score stylisé d'un fruit (100 à 5000), largeurs et alignements variables.
        /// </summary>
        public Bitmap GetFruitPointsSprite(int points)
        {
            Bitmap sprite;
            if (fruitPointsSprites.TryGetValue(points, out sprite))
                return sprite;

            int pixelX = 0;
            int pixelY = SpritesY + 144;
            int width = 16;

            switch (points)
            {
                case 100: pixelX = 0 * 16; break;
                case 300: pixelX = 1 * 16; break;
                case 500: pixelX = 2 * 16; break;
                case 700: pixelX = 3 * 16; break;

                case 1000:
                    pixelX = 4 * 16;
                    width = 18;        // Déborde à droite
                    break;

                case 2000:
                    pixelX = (4 * 16) - 3;  // 3 pixels plus tôt à gauche
                    pixelY += 1 * 16;
                    width = 21;
                    break;

                case 3000:
                    pixelX = (4 * 16) - 3;
                    pixelY += 2 * 16;
                    width = 21;
                    break;

                case 5000:
                    pixelX = (4 * 16) - 3;
                    pixelY += 3 * 16;
                    width = 21;
                    break;

                default:
                    return emptySprite16;
            }

            sprite = CropAsset(pixelX, pixelY, width, 16);
            fruitPointsSprites[points] = sprite;
            return sprite;
        }

        /// <summary>
        /// Pac-Man géant (32x32) de l'intermission 1 : large, ouvert, fermé, ouvert (puis on recommence).
        /// </summary>
        public Bitmap GetGiantPacmanSprite(int animFrame)
        {
            if (giantPacmanSprites == null)
            {
                giantPacmanSprites = new Bitmap[3];
                giantPacmanSprites[0] = CropAsset(32, 97, 32, 32);   // Bouche grande ouverte
                giantPacmanSprites[1] = CropAsset(64, 97, 32, 32);   // Bouche ouverte
                giantPacmanSprites[2] = CropAsset(96, 97, 32, 32);   // Bouche fermée
            }

            int[] order = { 0, 1, 2, 1 };
            return giantPacmanSprites[order[animFrame % 4]];
        }

        private readonly Dictionary<string, Bitmap> wideSheetSprites = new Dictionary<string, Bitmap>();

        public Bitmap GetWideSheetSprite(int x, int y, int width, int height)
        {
            string key = x + "," + y + "," + width + "," + height;
            if (!wideSheetSprites.TryGetValue(key, out Bitmap sprite))
            {
                sprite = CropAsset(x, y, width, height);
                wideSheetSprites[key] = sprite;
            }
            return sprite;
        }

        public void Dispose()
        {
            List<Bitmap> bitmaps = new List<Bitmap> { assetSheet, ghostDoorSprite, emptySprite16, emptyChar, lifeSprite };
            bitmaps.AddRange(pacmanDeathSprites);
            bitmaps.AddRange(frightenedBlueSprites);
            bitmaps.AddRange(frightenedWhiteSprites);
            bitmaps.AddRange(eyesSprites.Values);
            if (giantPacmanSprites != null) bitmaps.AddRange(giantPacmanSprites);

            foreach (var perDirection in ghostSprites.Values)
            {
                foreach (Bitmap[] frames in perDirection.Values)
                {
                    bitmaps.AddRange(frames);
                }
            }

            bitmaps.AddRange(pacmanSprites.Values);
            bitmaps.AddRange(mazeTiles.Values);
            bitmaps.AddRange(mazeLayers.Values);
            bitmaps.AddRange(charSprites.Values);
            bitmaps.AddRange(ptsSprites.Values);
            bitmaps.AddRange(namcoLogos.Values);
            bitmaps.AddRange(ghostPointsSprites.Values);
            bitmaps.AddRange(fruitSprites.Values);
            bitmaps.AddRange(fruitPointsSprites.Values);
            bitmaps.AddRange(sheetSprites.Values);
            bitmaps.AddRange(wideSheetSprites.Values);

            foreach (Bitmap bmp in bitmaps)
            {
                if (bmp != null) bmp.Dispose();
            }
        }
    }
}