using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static PacmanWinForms.SpriteManager;

namespace PacmanWinForms
{
    public partial class MainForm : Form
    {
        private SpriteManager spriteManager;
        private Timer gameTimer;
        private Stopwatch gameStopwatch = new Stopwatch();
        private double accumulatedTimeMs = 0;
        private Bitmap backBuffer; // Image native 224x288, agrandie une seule fois à l'écran

        // NOTRE MOTEUR DE JEU UNIQUE
        private Game game;

        public MainForm()
        {
            try
            {
                this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
                this.ShowIcon = true;
            }
            catch { /* Sécurité */ }
            this.Text = "Pac-Man Arcade (C#)";
            this.Size = new Size(GameConfig.ScreenWidth + 16, GameConfig.ScreenHeight + 39);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyDown += new KeyEventHandler(OnKeyDown);

            spriteManager = new SpriteManager();
            backBuffer = new Bitmap(GameConfig.MapWidth * GameConfig.TileSize, GameConfig.MapHeight * GameConfig.TileSize);

            try
            {
                // Instanciation de la logique pure
                game = new Game();
                UpdateGameTitleScore();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erreur Critique", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            gameTimer = new Timer();
            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;

            gameStopwatch.Start();
            gameTimer.Start();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            Direction direction = KeyToDirection(e.KeyCode);
            if (direction != Direction.None)
            {
                game.HandleInput(direction);
            }
        }

        private static Direction KeyToDirection(Keys key)
        {
            switch (key)
            {
                case Keys.Up: return Direction.Up;
                case Keys.Down: return Direction.Down;
                case Keys.Left: return Direction.Left;
                case Keys.Right: return Direction.Right;
                default: return Direction.None;
            }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            long elapsedMs = gameStopwatch.ElapsedMilliseconds;
            gameStopwatch.Restart();

            if (elapsedMs > 100) elapsedMs = 100;
            accumulatedTimeMs += elapsedMs;

            const double TargetFrameTime = 16.67;

            while (accumulatedTimeMs >= TargetFrameTime)
            {
                game.Update();
                accumulatedTimeMs -= TargetFrameTime;
            }

            UpdateGameTitleScore();
            Invalidate();
        }

        private void UpdateGameTitleScore()
        {
            this.Text = $"Pac-Man Arcade (C#) - SCORE: {game.Score} - VIES: {game.Lives} - Gommes: {game.DotsRemaining}";
        }

        // Dessine un sprite à l'échelle 1:1 (évite toute mise à l'échelle liée à la résolution de l'image)
        private static void DrawSprite(Graphics g, Bitmap sprite, int x, int y)
        {
            g.DrawImage(sprite, new Rectangle(x, y, sprite.Width, sprite.Height),
                0, 0, sprite.Width, sprite.Height, GraphicsUnit.Pixel);
        }

        private void DrawArcadeString(Graphics g, string text, int startTileX, int tileY, ArcadeColor colorSet)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ') continue; // On ne dessine rien pour un espace

                Bitmap charSprite = spriteManager.GetArcadeCharacter(c, colorSet);
                DrawSprite(g, charSprite, (startTileX + i) * GameConfig.TileSize, tileY * GameConfig.TileSize);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 1. On dessine tout le jeu à la résolution de l'arcade
            using (Graphics g = Graphics.FromImage(backBuffer))
            {
                g.Clear(Color.Black);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                RenderGame(g);
            }

            // 2. Puis on agrandit l'image d'un seul coup à l'écran
            Graphics screen = e.Graphics;
            screen.InterpolationMode = InterpolationMode.NearestNeighbor;
            screen.PixelOffsetMode = PixelOffsetMode.Half;
            screen.DrawImage(backBuffer, 0, 0, GameConfig.ScreenWidth, GameConfig.ScreenHeight);
        }

        private void RenderGame(Graphics g)
        {
            int tile = GameConfig.TileSize;

            // 1. LABYRINTHE (image pré-dessinée) + GOMMES
            MazeColor mazeColor = game.IsMazeFlashWhite ? MazeColor.White : MazeColor.Blue;
            DrawSprite(g, spriteManager.GetMazeLayer(game.Map, mazeColor), 0, 0);

            Bitmap dotSprite = spriteManager.GetDotSprite();
            Bitmap energizerSprite = spriteManager.GetEnergizerSprite();
            bool energizerVisible = (Environment.TickCount / 200) % 2 == 0;

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

            // 2. PAC-MAN ET FANTÔMES (uniquement s'ils doivent être visibles)
            if (game.ShowCharacters && game.State != GameState.GameOver)
            {
                // Pac-Man disparaît pendant le freeze du point de fantôme mangé
                if (game.Pacman != null && game.State != GameState.GhostEaten)
                {
                    int pacX = (int)(game.Pacman.X - 4);
                    int pacY = (int)(game.Pacman.Y - 4);

                    if (game.Pacman.State == PacmanState.Alive)
                    {
                        int framePourDessin = (game.State == GameState.Ready) ? 3 : game.Pacman.AnimFrame;
                        DrawSprite(g, spriteManager.GetPacmanSprite(framePourDessin, game.Pacman.CurrentDirection), pacX, pacY);
                    }
                    else if (game.Pacman.State == PacmanState.Dying)
                    {
                        DrawSprite(g, spriteManager.GetPacmanDeathSprite(game.Pacman.DeathAnimationFrame), pacX, pacY);
                    }
                }

                if (game.Pacman.State == PacmanState.Alive && game.ShowGhosts)
                {
                    foreach (Ghost ghost in game.Ghosts)
                    {
                        // Le fantôme qui vient d'être mangé n'est pas dessiné (son score prend sa place)
                        if (game.State == GameState.GhostEaten && ghost == game.JustEatenGhost)
                        {
                            continue;
                        }

                        Bitmap ghostSprite;

                        if (ghost.Phase == GhostPhase.Eaten)
                        {
                            ghostSprite = spriteManager.GetGhostEyesSprite(ghost.CurrentDirection);
                        }
                        else if (ghost.IsFrightened)
                        {
                            int virtualTicksRemaining = game.FrightenedFramesRemaining;
                            const int TicksPerFlashCycle = 16;
                            int totalFlashTicks = game.CurrentSpecs.FlashesCount * TicksPerFlashCycle;

                            bool shouldFlashWhite = game.IsFrightenedMode &&
                                (virtualTicksRemaining < totalFlashTicks) &&
                                ((virtualTicksRemaining / 8) % 2 == 0);

                            ghostSprite = spriteManager.GetGhostFrightenedSprite(game.GhostAnimationFrame, shouldFlashWhite);
                        }
                        else
                        {
                            string ghostName = ghost.GetType().Name;
                            ghostSprite = spriteManager.GetGhostSprite(ghostName, ghost.CurrentDirection, game.GhostAnimationFrame);
                        }

                        DrawSprite(g, ghostSprite, (int)(ghost.X - 4), (int)(ghost.Y - 4));
                    }
                }
            }

            // 3. TEXTE "READY!"
            if (game.State == GameState.Ready)
            {
                DrawArcadeString(g, "READY!", 11, 20, ArcadeColor.Yellow);
            }

            // 4. TEXTE "GAME OVER"
            if (game.State == GameState.GameOver)
            {
                DrawArcadeString(g, "GAME  OVER", 9, 20, ArcadeColor.Red);
            }

            // 5. SCORE DU FANTÔME MANGÉ
            if (game.State == GameState.GhostEaten)
            {
                Bitmap pointsSprite = spriteManager.GetGhostPointsSprite(game.EatenGhostScoreValue);
                DrawSprite(g, pointsSprite, game.EatenGhostScoreX, game.EatenGhostScoreY);
            }

            // 6. FRUIT BONUS
            if (game.Fruit.IsActive)
            {
                DrawSprite(g, spriteManager.GetFruitSprite(game.CurrentLevel), game.Fruit.X, game.Fruit.Y);
            }

            // 7. SCORE DU FRUIT MANGÉ
            if (game.Fruit.IsScoreVisible)
            {
                Bitmap pointsSprite = spriteManager.GetFruitPointsSprite(game.Fruit.Value);
                int posX = game.Fruit.X;

                // À partir de 2000, le sprite commence 3 pixels plus à gauche :
                // on décale pour garder la bordure droite alignée avec celle de 1000
                if (game.Fruit.Value >= 2000)
                {
                    posX -= 3;
                }

                DrawSprite(g, pointsSprite, posX, game.Fruit.Y);
            }

            // =====================================================================
            // HUD RÉTRO
            // =====================================================================

            // --- LIGNE 0 : textes fixes et clignotement du "1UP" ---
            bool show1Up = true;
            if (game.State == GameState.Playing)
            {
                show1Up = (Environment.TickCount / 220) % 2 == 0;
            }

            if (show1Up)
            {
                DrawArcadeString(g, "1UP", 3, 0, ArcadeColor.White);
            }

            DrawArcadeString(g, "HIGH SCORE", 9, 0, ArcadeColor.White);

            // --- LIGNE 1 : scores alignés à droite ---
            string scoreStr = (game.Score == 0) ? "00" : game.Score.ToString();
            int scoreStartX = 6 - scoreStr.Length + 1; // Aligné à droite sur la colonne 6
            DrawArcadeString(g, scoreStr, scoreStartX, 1, ArcadeColor.White);

            // Le high score reste vide tant que personne n'a marqué de points
            if (game.HighScore > 0)
            {
                string highScoreStr = game.HighScore.ToString();
                int highScoreStartX = 16 - highScoreStr.Length + 1; // Aligné à droite sur la colonne 16
                DrawArcadeString(g, highScoreStr, highScoreStartX, 1, ArcadeColor.White);
            }

            // --- LIGNE 34 : RÉSERVE DE VIES (en bas à gauche) ---
            // Tant que Pac-Man est caché on montre tout le stock, puis le stock diminue de 1 quand il apparaît.
            // Lors d'un changement de niveau, Pac-Man n'est pas mort : on maintient le compteur pour éviter un +1 / -1.
            bool subtractLifeIcon = game.ShowCharacters || game.IsNextLevelTransition;
            int livesToDraw = subtractLifeIcon ? game.Lives - 1 : game.Lives;
            int maxVisibleLives = Math.Min(livesToDraw, 5);

            Bitmap lifeSprite = spriteManager.GetPacmanSprite(1, Direction.Left);
            for (int i = 0; i < maxVisibleLives; i++)
            {
                DrawSprite(g, lifeSprite, (2 + (i * 2)) * tile, 34 * tile);
            }

            // --- LIGNE 34 : INDICATEUR DE NIVEAU (fruits en bas à droite, 7 derniers niveaux) ---
            int fruitsToDraw = Math.Min(game.CurrentLevel, 7);

            for (int i = 0; i < fruitsToDraw; i++)
            {
                // i = 0 : niveau actuel (tout à droite), puis on recule de 2 cases par niveau précédent
                int targetLevel = game.CurrentLevel - i;
                int tileX = 26 - (i * 2);

                DrawSprite(g, spriteManager.GetFruitSprite(targetLevel), tileX * tile, 34 * tile);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (gameTimer != null) gameTimer.Stop();
            if (game != null) game.Shutdown();
            if (backBuffer != null) backBuffer.Dispose();

            base.OnFormClosed(e);
        }
    }
}