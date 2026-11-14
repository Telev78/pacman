using System.Collections.Generic;

namespace PacmanWinForms
{
    /// <summary>
    /// Feuille de sprites de Ms Pac-Man. Les fantômes, yeux, fantômes effrayés et la police ont la même disposition
    /// que dans Pac-Man (décalée d'un pixel vers le bas, et la police commence à x = 199) : ils sont gérés par la classe de base.
    /// Ce qui change : Ms Pac-Man (avec son nœud), les fruits, les scores, les labyrinthes (10 colonnes par jeu de tuiles).
    /// </summary>
    public sealed class MsPacManSprites : SpriteManager
    {
        private const int SheetSpritesY = 82;     // Première ligne de personnages
        private const int TextX = 199;            // Blocs de texte
        private const int TextY = 82;

        // Jeux de tuiles : un par labyrinthe (0 à 5), tous les 99 pixels ; la version blanche (clignotement) est 45 pixels plus bas
        private const int TileSetStartX = 1;
        private const int TileSetSpacingX = 99;
        private const int TileSetColoredY = 1;
        private const int TileSetWhiteY = 46;
        private const int TileColumns = 10;
        private const int TileStride = 9;         // 8 pixels + 1 pixel de séparation

        // Scores des fantômes mangés : texte de 7 pixels de haut sur une ligne de la feuille
        private const int GhostPointsY = 214;
        private const int GhostPointsHeight = 7;

        // Scores des fruits : une case de 16x16 par valeur
        private const int FruitPointsY = 98;
        private static readonly int[] FruitPointValues = { 100, 200, 500, 700, 1000, 2000, 5000 };

        private readonly Dictionary<(int, bool), Sprite> mazeLayers = new Dictionary<(int, bool), Sprite>();
        private readonly Dictionary<(int, int), Sprite> pacmanSprites = new Dictionary<(int, int), Sprite>();
        private readonly Dictionary<int, Sprite> ghostPoints = new Dictionary<int, Sprite>();
        private readonly Dictionary<int, Sprite> fruitSprites = new Dictionary<int, Sprite>();
        private readonly Dictionary<int, Sprite> indicatorSprites = new Dictionary<int, Sprite>();
        private readonly Dictionary<int, Sprite> fruitPoints = new Dictionary<int, Sprite>();
        private Sprite lifeSprite;

        public MsPacManSprites(string sheetResource) : base(sheetResource, SheetSpritesY, TextX, TextY)
        {
        }

        // ------------------------------------------------------------------
        // Ms Pac-Man
        // ------------------------------------------------------------------

        // Lignes de la feuille : droite, gauche, haut, bas ; colonnes : 0 = bouche la plus ouverte, 1 = mi-ouverte, 2 = fermée
        private static int RowOf(Direction direction)
        {
            switch (direction)
            {
                case Direction.Right: return 0;
                case Direction.Up: return 2;
                case Direction.Down: return 3;
                default: return 1;   // Gauche (et départ)
            }
        }

        public override Sprite GetPacmanSprite(int animFrame, Direction direction)
        {
            int column = (animFrame == 3) ? 2 : (animFrame == 1 ? 1 : 0);
            return PacmanCell(RowOf(direction), column);
        }

        private Sprite PacmanCell(int row, int column)
        {
            var key = (row, column);
            Sprite sprite;
            if (!pacmanSprites.TryGetValue(key, out sprite))
            {
                sprite = CropAsset(column * SpriteSize, SheetSpritesY + row * SpriteSize);
                pacmanSprites[key] = sprite;
            }
            return sprite;
        }

        /// <summary>
        /// Mort : Ms Pac-Man tourne sur elle-même (droite, haut, gauche, bas, ...) puis se fige, bouche fermée.
        /// (Approximation de l'arcade ; la feuille n'a pas de sprites de mort dédiés.)
        /// </summary>
        public override Sprite GetPacmanDeathSprite(int frame)
        {
            int[] spinRows = { 0, 2, 1, 3 };   // Sens inverse des aiguilles d'une montre
            if (frame >= 11) return PacmanCell(3, 2);
            return PacmanCell(spinRows[frame % 4], 1);
        }

        /// <summary>Icône de la réserve de vies : Ms Pac-Man vers la gauche.</summary>
        public override Sprite GetLifeSprite()
        {
            if (lifeSprite == null) lifeSprite = PacmanCell(1, 1);
            return lifeSprite;
        }

        // ------------------------------------------------------------------
        // Labyrinthes
        // ------------------------------------------------------------------

        private Sprite MazeTile(int tileSet, int tileId, bool white)
        {
            int x = TileSetStartX + tileSet * TileSetSpacingX + (tileId % TileColumns) * TileStride;
            int y = (white ? TileSetWhiteY : TileSetColoredY) + (tileId / TileColumns) * TileStride;
            return CropAsset(x, y, GameConfig.TileSize, GameConfig.TileSize);
        }

        public override Sprite GetMazeLayer(LevelMap map, MazeColor color)
        {
            MazeDefinition maze = map.Definition;
            bool white = (color == MazeColor.White);
            var key = (maze.TileSetIndex, white);

            Sprite cached;
            if (mazeLayers.TryGetValue(key, out cached)) return cached;

            int tile = GameConfig.TileSize;
            Canvas layer = new Canvas(GameConfig.MapWidth * tile, GameConfig.MapHeight * tile);

            // La porte du nid fait partie des tuiles du labyrinthe
            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    int id = map.VisualGrid[r, c];
                    if (id >= 0) layer.Draw(MazeTile(maze.TileSetIndex, id, white), c * tile, r * tile);
                }
            }

            Sprite result = new Sprite(layer, 0, 0, layer.Width, layer.Height);
            mazeLayers[key] = result;
            return result;
        }

        public override Sprite GetDotSprite(MazeDefinition maze)
        {
            return MazeTile(maze.TileSetIndex, maze.DotTileId, false);
        }

        public override Sprite GetEnergizerSprite(MazeDefinition maze)
        {
            return MazeTile(maze.TileSetIndex, maze.DotTileId + 1, false);
        }

        // Sans labyrinthe (attract mode) : gommes du premier labyrinthe
        public override Sprite GetDotSprite()
        {
            return MazeTile(0, 30, false);
        }

        public override Sprite GetEnergizerSprite()
        {
            return MazeTile(0, 31, false);
        }

        // ------------------------------------------------------------------
        // Scores et fruits
        // ------------------------------------------------------------------

        /// <summary>200, 400, 800 ou 1600 : le texte (7 pixels de haut) est centré dans une case de 16x16, comme dans Pac-Man.</summary>
        public override Sprite GetGhostPointsSprite(int points)
        {
            Sprite sprite;
            if (ghostPoints.TryGetValue(points, out sprite)) return sprite;

            int index;
            switch (points)
            {
                case 200: index = 0; break;
                case 400: index = 1; break;
                case 800: index = 2; break;
                case 1600: index = 3; break;
                default: return EmptySprite;
            }

            Canvas cell = new Canvas(SpriteSize, SpriteSize);
            cell.Draw(CropAsset(index * SpriteSize, GhostPointsY, SpriteSize, GhostPointsHeight), 0, (SpriteSize - GhostPointsHeight) / 2);

            sprite = new Sprite(cell, 0, 0, SpriteSize, SpriteSize);
            ghostPoints[points] = sprite;
            return sprite;
        }

        /// <summary>Fruit du niveau : cerise, fraise, orange, bretzel, pomme, poire, banane aux niveaux 1 à 7, puis tirage fixe par niveau.</summary>
        public override Sprite GetFruitSprite(int level)
        {
            int index = MsFruitManager.FruitIndexForLevel(level);

            Sprite sprite;
            if (fruitSprites.TryGetValue(index, out sprite)) return sprite;

            // La poire et la banane ont une version "en jeu" (ligne suivante de la feuille)
            int y = (index >= 5) ? SheetSpritesY + 2 * SpriteSize : SheetSpritesY;
            sprite = CropAsset(48 + index * SpriteSize, y);
            fruitSprites[index] = sprite;
            return sprite;
        }

        /// <summary>Indicateur de niveau : les sept fruits à la suite sur la première ligne de la feuille.</summary>
        public override Sprite GetLevelIndicatorSprite(int level)
        {
            int index = MsFruitManager.FruitIndexForLevel(level);

            Sprite sprite;
            if (indicatorSprites.TryGetValue(index, out sprite)) return sprite;

            sprite = CropAsset(48 + index * SpriteSize, SheetSpritesY);
            indicatorSprites[index] = sprite;
            return sprite;
        }

        public override Sprite GetFruitPointsSprite(int points)
        {
            Sprite sprite;
            if (fruitPoints.TryGetValue(points, out sprite)) return sprite;

            int index = System.Array.IndexOf(FruitPointValues, points);
            if (index < 0) return EmptySprite;   // Valeurs propres à Pac-Man (300, 3000) : pas de sprite dans Ms Pac-Man

            sprite = CropAsset(48 + index * SpriteSize, FruitPointsY);
            fruitPoints[points] = sprite;
            return sprite;
        }

        // Les scores de fruit de Ms Pac-Man tiennent tous dans leur case : pas de décalage
        public override int GetFruitPointsOffsetX(int points)
        {
            return 0;
        }
    }
}
