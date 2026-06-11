using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;

namespace PacmanWinForms
{
    public partial class MainForm : Form
    {
        private SpriteManager spriteManager;
        private Timer gameTimer;
        private Timer deathAnimationTimer;
        private Stopwatch gameStopwatch = new Stopwatch();
        private double accumulatedTimeMs = 0;

        // NOTRE MOTEUR DE JEU UNIQUE
        private Game game;

        public MainForm()
        {
            this.Text = "Pac-Man Arcade (C#)";
            this.Size = new Size(GameConfig.ScreenWidth + 16, GameConfig.ScreenHeight + 39);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;

            this.KeyDown += new KeyEventHandler(OnKeyDown);

            spriteManager = new SpriteManager();

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

            deathAnimationTimer = new Timer();
            deathAnimationTimer.Interval = 70;
            deathAnimationTimer.Tick += DeathAnimationTimer_Tick;

            gameTimer = new Timer();
            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;

            gameStopwatch.Start();
            gameTimer.Start();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // On transfère la touche au "Cerveau"
            game.HandleInput(e.KeyCode);
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

                // INTERCEPTION DES CHANGEMENTS D'ÉTAT DU MOTEUR
                if (game.State == GameState.Dying)
                {
                    HandleDeathTransition();
                    return; // On sort immédiatement de la boucle
                }
                else if (game.State == GameState.Victory)
                {
                    gameTimer.Stop();
                    this.Refresh();
                    System.Threading.Thread.Sleep(500);
                    MessageBox.Show($"Félicitations ! Vous avez nettoyé le labyrinthe !\nScore final : {game.Score}", "Victoire !");
                    Application.Exit();
                    return;
                }
            }

            UpdateGameTitleScore();
            this.Invalidate();
        }

        private void HandleDeathTransition()
        {
            gameTimer.Stop();
            UpdateGameTitleScore();
            this.Invalidate();

            // Pause historique : On fige le thread graphique 1 seconde avant l'animation
            System.Threading.Thread.Sleep(1000);
            deathAnimationTimer.Start();
        }

        private void DeathAnimationTimer_Tick(object sender, EventArgs e)
        {
            game.Pacman.DeathAnimationFrame++;
            this.Invalidate();

            if (game.Pacman.DeathAnimationFrame >= 12)
            {
                deathAnimationTimer.Stop();
                System.Threading.Thread.Sleep(500); // Pause historique après disparition

                game.PrepareNextLifeOrGameOver();

                if (game.State == GameState.GameOver)
                {
                    MessageBox.Show($"GAME OVER\nScore final : {game.Score}", "Fin de partie");
                    Application.Exit();
                }
                else
                {
                    game.ResetPositions();
                    game.SetPacmanState(PacmanState.Alive);
                    UpdateGameTitleScore();
                    gameTimer.Start();
                }
            }
        }

        private void UpdateGameTitleScore()
        {
            this.Text = $"Pac-Man Arcade (C#) - SCORE: {game.Score} - VIES: {game.Lives} - Gommes: {game.DotsRemaining}";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int colorRowOffset = 3; // Bleu d'origine

            // 1. DESSIN DU LABYRINTHE (via game.Map)
            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    int screenX = c * GameConfig.ScaledTileSize;
                    int screenY = r * GameConfig.ScaledTileSize;
                    int size = GameConfig.ScaledTileSize;

                    if (game.Map.VisualGrid[r, c] != -1)
                    {
                        int tileID = game.Map.VisualGrid[r, c];
                        int sheetCol = tileID % 16;
                        int sheetRow = (tileID / 16) + colorRowOffset;

                        using (Bitmap tileSprite = spriteManager.GetMazeTile(sheetCol, sheetRow))
                        {
                            g.DrawImage(tileSprite, screenX, screenY, size, size);
                        }
                    }

                    int cellPhysic = game.Map.Grid[r, c];
                    if (cellPhysic == 2)
                    {
                        using (Bitmap dotSprite = spriteManager.GetMazeTile(13, 2 + colorRowOffset))
                        {
                            g.DrawImage(dotSprite, screenX, screenY, size, size);
                        }
                    }
                    else if (cellPhysic == 3)
                    {
                        if ((Environment.TickCount / 200) % 2 == 0)
                        {
                            using (Bitmap energizerSprite = spriteManager.GetMazeTile(15, 2 + colorRowOffset))
                            {
                                g.DrawImage(energizerSprite, screenX, screenY, size, size);
                            }
                        }
                    }

                    if (game.Map.Grid[r, c] == 4 && c == 13)
                    {
                        Bitmap door = spriteManager.GetGhostDoorSprite();
                        g.DrawImage(door, screenX, screenY, 16 * GameConfig.Zoom, 8 * GameConfig.Zoom);
                    }
                }
            }

            // 2. Dessiner Pac-Man et les Fantômes uniquement s'ils doivent être visibles
            if (game.ShowCharacters)
            {
                // --- BLOC DESSIN DE PAC-MAN ---
                if (game.Pacman != null)
                {
                    int screenX = (int)((game.Pacman.X - 4) * GameConfig.Zoom);
                    int screenY = (int)((game.Pacman.Y - 4) * GameConfig.Zoom);
                    int spriteSize = 16 * GameConfig.Zoom;

                    if (game.Pacman.State == PacmanState.Alive)
                    {
                        int framePourDessin = (game.State == GameState.Ready) ? 3 : game.Pacman.AnimFrame;

                        Bitmap pacmanSprite = spriteManager.GetPacmanSprite(framePourDessin, game.Pacman.CurrentDirection);
                        g.DrawImage(pacmanSprite, screenX, screenY, spriteSize, spriteSize);
                    }
                    else if (game.Pacman.State == PacmanState.Dying)
                    {
                        Bitmap deathSprite = spriteManager.GetPacmanDeathSprite(game.Pacman.DeathAnimationFrame);
                        g.DrawImage(deathSprite, screenX, screenY, spriteSize, spriteSize);
                    }
                }

                // --- BLOC DESSIN DES FANTÔMES ---
                if (game.Pacman.State == PacmanState.Alive)
                {
                    foreach (Ghost ghost in game.Ghosts)
                    {
                        Bitmap ghostSprite;

                        if (ghost.CurrentState == GhostState.Frightened)
                        {
                            int virtualTicksRemaining = (int)(game.FrightenedMsRemaining / 16.67f);
                            const int TicksPerFlashCycle = 16;
                            int totalFlashTicks = game.CurrentSpecs.FlashesCount * TicksPerFlashCycle;

                            bool shouldFlashWhite = game.IsFrightenedMode &&
                                (virtualTicksRemaining < totalFlashTicks) &&
                                ((virtualTicksRemaining / 8) % 2 == 0);

                            ghostSprite = spriteManager.GetGhostFrightenedSprite(game.GhostAnimationFrame, shouldFlashWhite);
                        }
                        else if (ghost.CurrentState == GhostState.Eaten)
                        {
                            ghostSprite = spriteManager.GetGhostEyesSprite(ghost.CurrentDirection);
                        }
                        else
                        {
                            string ghostName = ghost.GetType().Name;
                            ghostSprite = spriteManager.GetGhostSprite(ghostName, ghost.CurrentDirection, game.GhostAnimationFrame);
                        }

                        int bx = (int)((ghost.X - 4) * GameConfig.Zoom);
                        int by = (int)((ghost.Y - 4) * GameConfig.Zoom);

                        if (ghost.CurrentState == GhostState.InHouse) by += 1;
                        int spriteScaledSize = 16 * GameConfig.Zoom;

                        g.DrawImage(ghostSprite, bx, by, spriteScaledSize, spriteScaledSize);
                    }
                }
            }

            // 3. TEXTE "READY!" (Reste affiché pendant TOUTE la phase Ready, personnages là ou pas)
            if (game.State == GameState.Ready)
            {
                using (Font arcadeFont = new Font("Courier New", 14, FontStyle.Bold))
                using (Brush yellowBrush = new SolidBrush(Color.Yellow))
                {
                    string readyText = "READY!";
                    int textX = (11 * GameConfig.ScaledTileSize) + 4;
                    int textY = 20 * GameConfig.ScaledTileSize;

                    g.DrawString(readyText, arcadeFont, yellowBrush, textX, textY);
                }
            }
        }
    }
}