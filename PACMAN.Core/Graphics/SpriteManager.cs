using System;
using System.Collections.Generic;
using System.IO;

namespace PacmanWinForms
{
    /// <summary>
    /// Découpe les sprites de la feuille unique (asset.png) et les met en cache.
    /// Un Sprite n'est qu'un rectangle de la feuille (aucune copie de pixels) : rien à libérer.
    /// IMPORTANT : les Sprite renvoyés sont partagés, ne jamais modifier leurs pixels.
    /// </summary>
    public class SpriteManager
    {
        protected const int SpriteSize = 16;
        private const int SpritesX = 0;    // Début des sprites de personnages dans la feuille

        // Position des sprites de personnages et des blocs de texte dans la feuille (propre à chaque jeu)
        private readonly int SpritesY;
        private readonly int TextX;
        private readonly int TextY;

        private Canvas assetSheet;
        private Sprite ghostDoorSprite;
        private Sprite emptySprite16;
        private Sprite emptyChar;
        private Sprite[] pacmanDeathSprites;
        private Dictionary<GhostType, Dictionary<Direction, Sprite[]>> ghostSprites;
        private Dictionary<Direction, Sprite> eyesSprites;
        private Sprite[] frightenedBlueSprites;
        private Sprite[] frightenedWhiteSprites;
        private Sprite lifeSprite;
        private Sprite[] giantPacmanSprites;

        // Caches (remplis à la demande)
        private readonly Dictionary<(int, Direction), Sprite> pacmanSprites = new Dictionary<(int, Direction), Sprite>();
        private readonly Dictionary<(int, int, MazeColor), Sprite> mazeTiles = new Dictionary<(int, int, MazeColor), Sprite>();
        private readonly Dictionary<MazeColor, Sprite> mazeLayers = new Dictionary<MazeColor, Sprite>();
        private readonly Dictionary<(char, ArcadeColor), Sprite> charSprites = new Dictionary<(char, ArcadeColor), Sprite>();
        private readonly Dictionary<ArcadeColor, Sprite> ptsSprites = new Dictionary<ArcadeColor, Sprite>();
        private readonly Dictionary<ArcadeColor, Sprite> namcoLogos = new Dictionary<ArcadeColor, Sprite>();
        private readonly Dictionary<int, Sprite> ghostPointsSprites = new Dictionary<int, Sprite>();
        private readonly Dictionary<int, Sprite> fruitSprites = new Dictionary<int, Sprite>();
        private readonly Dictionary<int, Sprite> fruitPointsSprites = new Dictionary<int, Sprite>();

        private readonly Dictionary<(int, int), Sprite> sheetSprites = new Dictionary<(int, int), Sprite>();

        /// <summary>
        /// Sprite 16x16 de la feuille aux coordonnées (x, y), mis en cache (intermissions).
        /// </summary>
        public Sprite GetSheetSprite(int x, int y)
        {
            Sprite sprite;
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

        private Canvas LoadEmbeddedCanvas(string resourcePath)
        {
            // Exemple de chemin : asset.asset.png (ressource embarquée dans PACMAN.Core)
            using (Stream stream = Assets.Open(resourcePath))
            {
                return PngDecoder.Decode(stream);
            }
        }

        // Un rectangle de la feuille (simple vue, sans copie de pixels)
        protected Sprite CropAsset(int x, int y, int width = 16, int height = 16)
        {
            return new Sprite(assetSheet, x, y, width, height);
        }

        /// <summary>Feuille de sprites de Pac-Man.</summary>
        public SpriteManager() : this(PacManVariant.Folder + "asset.png")
        {
        }

        /// <summary>Feuille de sprites de Pac-Man, dans la ressource donnée.</summary>
        public SpriteManager(string sheetResource) : this(sheetResource, 81, 0, 292)
        {
        }

        /// <param name="sheetResource">Ressource embarquée de la feuille, ex. "asset.pacman.asset.png".</param>
        /// <param name="spritesY">Ordonnée de la première ligne de sprites de personnages.</param>
        /// <param name="textX">Abscisse des blocs de texte.</param>
        /// <param name="textY">Ordonnée du premier bloc de texte.</param>
        protected SpriteManager(string sheetResource, int spritesY, int textX, int textY)
        {
            SpritesY = spritesY;
            TextX = textX;
            TextY = textY;

            assetSheet = LoadEmbeddedCanvas(sheetResource);
            assetSheet.MakeTransparent(0x000000);   // Le noir pur de la feuille devient transparent

            emptySprite16 = Sprite.Empty(SpriteSize, SpriteSize);
            emptyChar = Sprite.Empty(8, 8);

            // Porte du nid (8x8, étirée en 16x8 au dessin)
            ghostDoorSprite = CropAsset(144, 72, 8, 8);

            // Fantômes : 4 directions x 2 frames, une ligne de 16 px par fantôme (dans l'ordre de GhostType)
            ghostSprites = new Dictionary<GhostType, Dictionary<Direction, Sprite[]>>();
            Direction[] dirOrder = { Direction.Right, Direction.Left, Direction.Up, Direction.Down };

            foreach (GhostType type in Enum.GetValues(typeof(GhostType)))
            {
                var currentGhostDict = new Dictionary<Direction, Sprite[]>();
                int y = SpritesY + 64 + ((int)type * SpriteSize);

                for (int d = 0; d < dirOrder.Length; d++)
                {
                    Sprite[] frames = new Sprite[2];
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
            frightenedBlueSprites = new Sprite[2];
            frightenedWhiteSprites = new Sprite[2];
            for (int f = 0; f < 2; f++)
            {
                frightenedBlueSprites[f] = CropAsset(frightX + f * SpriteSize, SpritesY + 64);
                frightenedWhiteSprites[f] = CropAsset(frightX + (2 + f) * SpriteSize, SpritesY + 64);
            }

            // Yeux (Droite, Gauche, Haut, Bas)
            eyesSprites = new Dictionary<Direction, Sprite>();
            Direction[] eyeDirs = { Direction.Right, Direction.Left, Direction.Up, Direction.Down };
            for (int d = 0; d < eyeDirs.Length; d++)
            {
                eyesSprites.Add(eyeDirs[d], CropAsset(SpritesX + (8 + d) * SpriteSize, SpritesY + 80));
            }

            // Animation de mort de Pac-Man (12 frames alignées horizontalement)
            pacmanDeathSprites = new Sprite[12];
            for (int f = 0; f < 12; f++)
            {
                pacmanDeathSprites[f] = CropAsset(SpritesX + (3 + f) * SpriteSize, SpritesY);
            }
        }

        // ------------------------------------------------------------------
        // Personnages
        // ------------------------------------------------------------------

        public Sprite GetGhostSprite(GhostType type, Direction direction, int frame)
        {
            if (ghostSprites.ContainsKey(type) && ghostSprites[type].ContainsKey(direction))
            {
                return ghostSprites[type][direction][frame % 2];
            }
            return ghostSprites[GhostType.Blinky][Direction.Left][0]; // Repli de secours
        }

        public Sprite GetGhostFrightenedSprite(int frame, bool flashWhite = false)
        {
            if (flashWhite)
                return frightenedWhiteSprites[frame % 2];
            return frightenedBlueSprites[frame % 2];
        }

        public Sprite GetGhostEyesSprite(Direction direction)
        {
            if (eyesSprites.ContainsKey(direction))
                return eyesSprites[direction];
            return frightenedBlueSprites[0]; // Sécurité
        }

        /// <summary>
        /// Sprite de Pac-Man selon sa frame d'animation et sa direction.
        /// La frame 3 (bouche fermée) est partagée par toutes les directions.
        /// </summary>
        public virtual Sprite GetPacmanSprite(int animFrame, Direction direction)
        {
            Direction cacheDirection = (animFrame == 3) ? Direction.None : direction;
            var key = (animFrame, cacheDirection);

            Sprite sprite;
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

        public virtual Sprite GetPacmanDeathSprite(int frame)
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
        public Sprite GetMazeTile(int sheetColumn, int sheetRow, MazeColor color = MazeColor.Blue)
        {
            var key = (sheetColumn, sheetRow, color);

            Sprite tile;
            if (mazeTiles.TryGetValue(key, out tile))
                return tile;

            int size = GameConfig.TileSize;
            int row = sheetRow + ((int)color * 3);

            tile = CropAsset((sheetColumn * size) + sheetColumn, (row * size) + row, size, size);
            mazeTiles[key] = tile;
            return tile;
        }

        /// <summary>Gomme du labyrinthe donné (la couleur change d'un labyrinthe à l'autre dans Ms Pac-Man).</summary>
        public virtual Sprite GetDotSprite(MazeDefinition maze)
        {
            return GetDotSprite();
        }

        /// <summary>Super-gomme du labyrinthe donné.</summary>
        public virtual Sprite GetEnergizerSprite(MazeDefinition maze)
        {
            return GetEnergizerSprite();
        }

        /// <summary>Décalage horizontal du score d'un fruit (la feuille de Pac-Man dessine les grands scores 3 pixels plus à gauche).</summary>
        public virtual int GetFruitPointsOffsetX(int points)
        {
            return points >= 2000 ? -3 : 0;
        }

        /// <summary>Carré transparent de 16x16.</summary>
        protected Sprite EmptySprite { get { return emptySprite16; } }

        public virtual Sprite GetDotSprite()
        {
            return GetMazeTile(13, 2);
        }

        public virtual Sprite GetEnergizerSprite()
        {
            return GetMazeTile(15, 2);
        }

        /// <summary>
        /// Icône de la réserve de vies (HUD du bas), située en (128, 97) dans la feuille de sprites.
        /// </summary>
        public virtual Sprite GetLifeSprite()
        {
            if (lifeSprite == null)
            {
                lifeSprite = CropAsset(128, 97);
            }
            return lifeSprite;
        }
        public Sprite GetGhostDoorSprite()
        {
            return ghostDoorSprite;
        }

        /// <summary>
        /// Image pré-dessinée du labyrinthe (murs et porte du nid, sans les gommes), à l'échelle native 224x288.
        /// Construite une seule fois par couleur : les murs ne changent jamais.
        /// </summary>
        public virtual Sprite GetMazeLayer(LevelMap map, MazeColor color)
        {
            Sprite cached;
            if (mazeLayers.TryGetValue(color, out cached))
                return cached;

            int tile = GameConfig.TileSize;
            Canvas layer = new Canvas(GameConfig.MapWidth * tile, GameConfig.MapHeight * tile);

            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    int tileID = map.VisualGrid[r, c];
                    if (tileID != -1)
                    {
                        layer.Draw(GetMazeTile(tileID % 16, tileID / 16, color), c * tile, r * tile);
                    }

                    // Porte du nid : le même sprite 8x8 posé deux fois côte à côte (deux colonnes)
                    if (map.Grid[r, c] == TileType.GhostDoor && c == 13)
                    {
                        layer.Draw(ghostDoorSprite, c * tile, r * tile);
                        layer.Draw(ghostDoorSprite, (c + 1) * tile, r * tile);
                    }
                }
            }

            Sprite result = new Sprite(layer, 0, 0, layer.Width, layer.Height);
            mazeLayers[color] = result;
            return result;
        }

        // ------------------------------------------------------------------
        // Texte
        // ------------------------------------------------------------------

        /// <summary>
        /// Caractère 8x8 de la feuille de texte dans la couleur demandée.
        /// </summary>
        public Sprite GetArcadeCharacter(char c, ArcadeColor color = ArcadeColor.Yellow)
        {
            char upperC = char.ToUpper(c);
            var key = (upperC, color);

            Sprite sprite;
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
            sprite = CropAsset(TextX + col * 8, TextY + ((((int)color * 4) + rowOffset) * 8), 8, 8);
            charSprites[key] = sprite;
            return sprite;
        }

        // Le point n'existe pas dans la feuille : 2x2 pixels en bas à gauche de la tuile, de la couleur du texte
        private Sprite CreateDotGlyph(ArcadeColor color)
        {
            Sprite reference = GetArcadeCharacter('A', color);
            uint ink = 0xFFFFFFFF;
            bool found = false;
            for (int y = 0; y < reference.Height && !found; y++)
            {
                for (int x = 0; x < reference.Width; x++)
                {
                    uint p = reference.GetPixel(x, y);
                    if ((p >> 24) > 0) { ink = p; found = true; break; }
                }
            }

            Canvas dot = new Canvas(8, 8);
            for (int y = 5; y <= 6; y++)
                for (int x = 1; x <= 2; x++)
                    dot.SetPixel(x, y, ink);
            return new Sprite(dot, 0, 0, 8, 8);
        }

        /// <summary>
        /// Glyphe combiné "PTS" (17x8) à la fin de la 2ème ligne d'un bloc de couleur.
        /// </summary>
        public Sprite GetPtsSprite(ArcadeColor color = ArcadeColor.Yellow)
        {
            Sprite sprite;
            if (ptsSprites.TryGetValue(color, out sprite))
                return sprite;

            int localRow = 1;    // 2ème ligne du sous-bloc de couleur
            int startCol = 13;

            sprite = CropAsset(TextX + startCol * 8, TextY + ((((int)color * 4) + localRow) * 8), 17, 8);
            ptsSprites[color] = sprite;
            return sprite;
        }

        /// <summary>
        /// Logo "NAMCO" (50x8) sur la 4ème ligne d'un bloc de couleur.
        /// </summary>
        public Sprite GetNamcoLogo(ArcadeColor color = ArcadeColor.Red)
        {
            Sprite sprite;
            if (namcoLogos.TryGetValue(color, out sprite))
                return sprite;

            int localRow = 3;    // 4ème ligne du sous-bloc de couleur
            int startCol = 0;

            sprite = CropAsset(TextX + startCol * 8, TextY + ((((int)color * 4) + localRow) * 8), 50, 8);
            namcoLogos[color] = sprite;
            return sprite;
        }

        // ------------------------------------------------------------------
        // Scores et fruits
        // ------------------------------------------------------------------

        /// <summary>
        /// Score stylisé affiché quand un fantôme est mangé (200, 400, 800, 1600).
        /// </summary>
        public virtual Sprite GetGhostPointsSprite(int points)
        {
            Sprite sprite;
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
        /// <summary>Fruit de l'indicateur de niveau (bas de l'écran) ; par défaut le même que celui du jeu.</summary>
        public virtual Sprite GetLevelIndicatorSprite(int level)
        {
            return GetFruitSprite(level);
        }

        public virtual Sprite GetFruitSprite(int level)
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

            Sprite sprite;
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
        public virtual Sprite GetFruitPointsSprite(int points)
        {
            Sprite sprite;
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
        public Sprite GetGiantPacmanSprite(int animFrame)
        {
            if (giantPacmanSprites == null)
            {
                giantPacmanSprites = new Sprite[3];
                giantPacmanSprites[0] = CropAsset(32, 97, 32, 32);   // Bouche grande ouverte
                giantPacmanSprites[1] = CropAsset(64, 97, 32, 32);   // Bouche ouverte
                giantPacmanSprites[2] = CropAsset(96, 97, 32, 32);   // Bouche fermée
            }

            int[] order = { 0, 1, 2, 1 };
            return giantPacmanSprites[order[animFrame % 4]];
        }

        private readonly Dictionary<string, Sprite> wideSheetSprites = new Dictionary<string, Sprite>();

        public Sprite GetWideSheetSprite(int x, int y, int width, int height)
        {
            string key = x + "," + y + "," + width + "," + height;
            if (!wideSheetSprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = CropAsset(x, y, width, height);
                wideSheetSprites[key] = sprite;
            }
            return sprite;
        }
    }
}
