using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using static PacmanWinForms.SpriteManager;

namespace PacmanWinForms
{
    /// <summary>
    /// Dessine le jeu. Tout est dessiné à la résolution de l'arcade (224x288) dans une image intermédiaire,
    /// qui est ensuite agrandie d'un seul coup à l'écran.
    /// </summary>
    public sealed class GameRenderer : IDisposable
    {
        // Durées de clignotement, en frames de jeu à 60 Hz
        private const int OneUpBlinkFrames = 13;       // ~220 ms par phase

        // Clignotement bleu / blanc des fantômes effrayés
        private const int FlashCycleFrames = 16;
        private const int FlashHalfCycleFrames = 8;

        // Le score d'un fruit de 2000 points ou plus est dessiné 3 pixels plus à gauche dans la feuille :
        // on décale pour garder la bordure droite alignée avec celle de 1000
        private const int WideFruitScoreOffset = 3;
        private const int WideFruitScoreMinValue = 2000;

        private readonly SpriteManager sprites = new SpriteManager();
        private readonly Bitmap backBuffer = new Bitmap(DisplayConfig.NativeWidth, DisplayConfig.NativeHeight);

        // Les écrans utilisent les sprites et les outils de dessin du moteur
        public SpriteManager Sprites { get { return sprites; } }

        /// <summary>
        /// Dessine un écran complet sur la surface cible, agrandi à la taille de l'écran.
        /// </summary>
        public void Render(IScreen screen, Graphics target, Rectangle destination)
        {
            // 1. On dessine tout à la résolution de l'arcade
            using (Graphics g = Graphics.FromImage(backBuffer))
            {
                g.Clear(Color.Black);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                screen.Draw(this, g);
            }

            // 2. Puis on agrandit l'image d'un seul coup à l'écran
            target.InterpolationMode = InterpolationMode.NearestNeighbor;
            target.PixelOffsetMode = PixelOffsetMode.Half;
            target.DrawImage(backBuffer, destination);
        }

        /// <summary>
        /// Dessine une partie en cours (labyrinthe, personnages, messages, bonus, HUD).
        /// </summary>
        public void DrawGame(Graphics g, Game game)
        {
            DrawMaze(g, game);

            // En démo, "GAME  OVER" est sous les personnages (ils passent par-dessus, comme à l'arcade)
            if (game.IsDemo)
            {
                DrawGameOverLabel(g);
            }

            DrawCharacters(g, game);
            DrawMessages(g, game);
            DrawBonuses(g, game);
            DrawHud(g, game);
        }

        public void Dispose()
        {
            backBuffer.Dispose();
            sprites.Dispose();
        }

        // ------------------------------------------------------------------
        // Labyrinthe et gommes
        // ------------------------------------------------------------------

        private void DrawMaze(Graphics g, Game game)
        {
            int tile = GameConfig.TileSize;

            // Image pré-dessinée des murs (blanche pendant le clignotement de fin de niveau)
            MazeColor color = game.IsMazeFlashWhite ? MazeColor.White : MazeColor.Blue;
            DrawSprite(g, sprites.GetMazeLayer(game.Map, color), 0, 0);

            // Les super-gommes clignotent pendant le jeu actif, et restent visibles le reste du temps
            bool energizerVisible = game.State != GameState.Playing
                        || IsEnergizerOn(game.FrameCounter);

            Bitmap dotSprite = sprites.GetDotSprite();
            Bitmap energizerSprite = sprites.GetEnergizerSprite();

            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    TileType cell = game.Map.Grid[r, c];

                    if (cell == TileType.Dot)
                    {
                        DrawSprite(g, dotSprite, c * tile, r * tile);
                    }
                    else if (cell == TileType.Energizer && energizerVisible)
                    {
                        DrawSprite(g, energizerSprite, c * tile, r * tile);
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Pac-Man et fantômes
        // ------------------------------------------------------------------

        private void DrawCharacters(Graphics g, Game game)
        {
            if (!game.ShowCharacters || game.State == GameState.GameOver)
                return;

            DrawPacman(g, game);

            if (game.Pacman.State == PacmanState.Alive && game.ShowGhosts)
            {
                DrawGhosts(g, game);
            }
        }

        private void DrawPacman(Graphics g, Game game)
        {
            // Pac-Man disparaît pendant le gel du score d'un fantôme mangé
            if (game.State == GameState.GhostEaten)
                return;

            Pacman pacman = game.Pacman;
            int x = (int)(pacman.X - 4);
            int y = (int)(pacman.Y - 4);

            if (pacman.State == PacmanState.Alive)
            {
                // Bouche fermée pendant le "READY!"
                int frame = (game.State == GameState.Ready) ? 3 : pacman.AnimFrame;
                DrawSprite(g, sprites.GetPacmanSprite(frame, pacman.CurrentDirection), x, y);
            }
            else if (pacman.State == PacmanState.Dying)
            {
                DrawSprite(g, sprites.GetPacmanDeathSprite(pacman.DeathAnimationFrame), x, y);
            }
        }

        private void DrawGhosts(Graphics g, Game game)
        {
            foreach (Ghost ghost in game.Ghosts)
            {
                // Le fantôme qui vient d'être mangé n'est pas dessiné (son score prend sa place)
                if (game.State == GameState.GhostEaten && ghost == game.JustEatenGhost)
                    continue;

                DrawSprite(g, GetGhostSprite(game, ghost), (int)(ghost.X - 4), (int)(ghost.Y - 4));
            }
        }

        private Bitmap GetGhostSprite(Game game, Ghost ghost)
        {
            if (ghost.Phase == GhostPhase.Eaten)
            {
                return sprites.GetGhostEyesSprite(ghost.CurrentDirection);
            }

            if (ghost.IsFrightened)
            {
                // Dans la dernière partie de la panique, les fantômes clignotent entre bleu et blanc
                int framesRemaining = game.FrightenedFramesRemaining;
                int flashFrames = game.CurrentSpecs.FlashesCount * FlashCycleFrames;

                bool flashWhite = game.IsFrightenedMode
                                  && framesRemaining < flashFrames
                                  && (framesRemaining / FlashHalfCycleFrames) % 2 == 0;

                return sprites.GetGhostFrightenedSprite(game.GhostAnimationFrame, flashWhite);
            }

            return sprites.GetGhostSprite(ghost.Type, ghost.CurrentDirection, game.GhostAnimationFrame);
        }

        // ------------------------------------------------------------------
        // Messages, scores flottants et fruit
        // ------------------------------------------------------------------

        private void DrawMessages(Graphics g, Game game)
        {
            if (game.ShowPlayerLabel)
            {
                string label = (game.ActivePlayer == 1) ? ArcadeTexts.PlayerOne : ArcadeTexts.PlayerTwo;
                DrawText(g, label, HudLayout.PlayerLabelColumn, HudLayout.PlayerLabelRow, ArcadeColor.Cyan);
            }
            if (game.IsDemo)
            {
                // Déjà dessiné sous les personnages (voir DrawGame)
            }
            else if (game.State == GameState.Ready)
            {
                DrawText(g, "READY!", HudLayout.ReadyColumn, HudLayout.MessageRow, ArcadeColor.Yellow);
            }
            else if (game.State == GameState.GameOver)
            {
                DrawGameOverLabel(g);
            }
        }

        private void DrawGameOverLabel(Graphics g)
        {
            DrawText(g, "GAME  OVER", HudLayout.GameOverColumn, HudLayout.MessageRow, ArcadeColor.Red);
        }

        private void DrawBonuses(Graphics g, Game game)
        {
            // Score du fantôme mangé, à l'endroit où il a été capturé
            if (game.State == GameState.GhostEaten)
            {
                Bitmap points = sprites.GetGhostPointsSprite(game.EatenGhostScoreValue);
                DrawSprite(g, points, game.EatenGhostScoreX, game.EatenGhostScoreY);
            }

            // Fruit bonus
            if (game.Fruit.IsActive)
            {
                DrawSprite(g, sprites.GetFruitSprite(game.CurrentLevel), game.Fruit.X, game.Fruit.Y);
            }

            // Score du fruit mangé
            if (game.Fruit.IsScoreVisible)
            {
                Bitmap points = sprites.GetFruitPointsSprite(game.Fruit.Value);
                int x = game.Fruit.X;

                if (game.Fruit.Value >= WideFruitScoreMinValue)
                {
                    x -= WideFruitScoreOffset;
                }

                DrawSprite(g, points, x, game.Fruit.Y);
            }
        }

        // ------------------------------------------------------------------
        // HUD
        // ------------------------------------------------------------------

        private void DrawHud(Graphics g, Game game)
        {
            DrawScores(g, game);
            if (game.IsDemo) return;   // En démo : pas de vies ni de niveau (les crédits sont dessinés par DemoScreen)
            DrawLives(g, game);
            DrawLevelIndicator(g, game.CurrentLevel);
        }

        private void DrawScores(Graphics g, Game game)
        {
            // Pendant le jeu actif, le "UP" du joueur dont c'est le tour clignote ; l'autre reste fixe
            bool animate = !game.IsDemo && game.State == GameState.Playing;
            bool blinkOn = (game.FrameCounter / OneUpBlinkFrames) % 2 == 0;

            bool show1Up = !animate || game.ActivePlayer != 1 || blinkOn;
            bool show2Up = game.IsDemo
                           || (game.PlayerCount == 2 && (!animate || game.ActivePlayer != 2 || blinkOn));

            DrawTopHud(g, game.GetScore(1), game.GetScore(2), game.HighScore,
                show1Up, show2Up, showScore2: game.PlayerCount == 2);
        }

        /// <summary>
        /// Bandeau supérieur (attract mode et écran start) : seul le score du joueur 1 est affiché.
        /// </summary>
        public void DrawTopHud(Graphics g, int score, int highScore, bool show1Up, bool show2Up)
        {
            DrawTopHud(g, score, 0, highScore, show1Up, show2Up, showScore2: false);
        }

        /// <summary>
        /// Bandeau supérieur : 1UP, HIGH SCORE, 2UP et les scores.
        /// </summary>
        public void DrawTopHud(Graphics g, int score1, int score2, int highScore,
            bool show1Up, bool show2Up, bool showScore2)
        {
            if (show1Up)
            {
                DrawText(g, "1UP", HudLayout.OneUpColumn, HudLayout.TopRow, ArcadeColor.White);
            }

            DrawText(g, "HIGH SCORE", HudLayout.HighScoreLabelColumn, HudLayout.TopRow, ArcadeColor.White);

            if (show2Up)
            {
                DrawText(g, "2UP", HudLayout.TwoUpColumn, HudLayout.TopRow, ArcadeColor.White);
            }

            // Score du joueur 1 : "00" s'il est à zéro, aligné à droite
            string score1Text = (score1 == 0) ? "00" : score1.ToString();
            DrawText(g, score1Text, HudLayout.ScoreRightColumn - score1Text.Length + 1, HudLayout.ScoreRow, ArcadeColor.White);

            // Score du joueur 2, seulement dans une partie à 2 joueurs
            if (showScore2)
            {
                string score2Text = (score2 == 0) ? "00" : score2.ToString();
                DrawText(g, score2Text, HudLayout.Score2RightColumn - score2Text.Length + 1, HudLayout.ScoreRow, ArcadeColor.White);
            }

            // Le high score reste vide tant que personne n'a marqué de points
            if (highScore > 0)
            {
                string highScoreText = highScore.ToString();
                DrawText(g, highScoreText, HudLayout.HighScoreRightColumn - highScoreText.Length + 1, HudLayout.ScoreRow, ArcadeColor.White);
            }
        }

        /// <summary>
        /// Compteur de crédits, tout en bas de l'écran (hors partie).
        /// </summary>
        public void DrawCredits(Graphics g, int credits)
        {
            DrawText(g, ArcadeTexts.Credit, HudLayout.CreditLabelColumn, HudLayout.CreditRow, ArcadeColor.White);

            string value = credits.ToString();
            DrawText(g, value, HudLayout.CreditValueRightColumn - value.Length + 1, HudLayout.CreditRow, ArcadeColor.White);
        }

        private void DrawLives(Graphics g, Game game)
        {
            int tile = GameConfig.TileSize;

            // Tant que Pac-Man est caché on montre tout le stock, puis le stock diminue de 1 quand il apparaît.
            // Lors d'un changement de niveau, Pac-Man n'est pas mort : on maintient le compteur pour éviter un +1 / -1.
            bool subtractLifeIcon = game.ShowCharacters || game.IsNextLevelTransition;
            int livesToDraw = subtractLifeIcon ? game.Lives - 1 : game.Lives;
            int visibleLives = Math.Min(livesToDraw, HudLayout.MaxVisibleLives);

            Bitmap lifeSprite = sprites.GetLifeSprite();
            for (int i = 0; i < visibleLives; i++)
            {
                int column = HudLayout.LivesFirstColumn + i * HudLayout.LivesSpacing;
                DrawSprite(g, lifeSprite, column * tile, HudLayout.BottomRow * tile);
            }
        }

        public void DrawLevelIndicator(Graphics g, int level)
        {
            int tile = GameConfig.TileSize;
            int fruitsToDraw = Math.Min(level, HudLayout.MaxLevelIndicators);
            int firstLevel = level - fruitsToDraw + 1;   // Le plus ancien niveau affiché

            for (int i = 0; i < fruitsToDraw; i++)
            {
                // i = 0 : le plus ancien, tout à droite ; le niveau actuel est le plus à gauche
                int column = HudLayout.LevelIndicatorRightColumn - i * HudLayout.LevelIndicatorSpacing;
                DrawSprite(g, sprites.GetFruitSprite(firstLevel + i), column * tile, HudLayout.BottomRow * tile);
            }
        }

        // ------------------------------------------------------------------
        // Outils de dessin
        // ------------------------------------------------------------------

        // Dessine un sprite à l'échelle 1:1 (évite toute mise à l'échelle liée à la résolution de l'image)
        public static void DrawSprite(Graphics g, Bitmap sprite, int x, int y)
        {
            g.DrawImage(sprite, new Rectangle(x, y, sprite.Width, sprite.Height),
                0, 0, sprite.Width, sprite.Height, GraphicsUnit.Pixel);
        }

        // Dessine un texte lettre par lettre avec la police de l'arcade (une lettre = une tuile)
        public void DrawText(Graphics g, string text, int startTileX, int tileY, ArcadeColor color)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ') continue; // On ne dessine rien pour un espace

                Bitmap letter = sprites.GetArcadeCharacter(c, color);
                DrawSprite(g, letter, (startTileX + i) * GameConfig.TileSize, tileY * GameConfig.TileSize);
            }
        }

        /// <summary>
        /// Phase du clignotement des super-gommes (allumée / éteinte), mesurée sur l'arcade :
        /// environ 10,7 frames par phase, soit une période de 21,3 frames.
        /// </summary>
        public static bool IsEnergizerOn(int frame)
        {
            return (frame * 3 / 32) % 2 == 0;
        }
    }
}