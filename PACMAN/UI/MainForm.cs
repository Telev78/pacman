using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public class MainForm : Form
    {
        private readonly GameRenderer renderer;
        private Timer gameTimer;
        private Stopwatch gameStopwatch = new Stopwatch();
        private double accumulatedTimeMs = 0;

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
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ClientSize = new Size(DisplayConfig.ScreenWidth, DisplayConfig.ScreenHeight);
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyDown += new KeyEventHandler(OnKeyDown);

            // Si l'un des deux échoue (ressource manquante...), l'erreur est affichée par Program.Main
            renderer = new GameRenderer();
            game = new Game();
            UpdateGameTitleScore();

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

        protected override void OnPaint(PaintEventArgs e)
        {
            renderer.Render(game, e.Graphics);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (gameTimer != null) gameTimer.Stop();
            if (game != null) game.Shutdown();
            if (renderer != null) renderer.Dispose();

            base.OnFormClosed(e);
        }
    }
}