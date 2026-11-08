using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public class MainForm : Form
    {
        private readonly GameRenderer renderer;
        private readonly ScreenManager screens;
        private Timer gameTimer;
        private Stopwatch gameStopwatch = new Stopwatch();
        private double accumulatedTimeMs = 0;

        // Touches de service déjà enfoncées (pour ignorer la répétition automatique du clavier)
        private readonly HashSet<Keys> heldServiceKeys = new HashSet<Keys>();

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
            this.KeyUp += new KeyEventHandler(OnKeyUp);

            // Si l'un des deux échoue (ressource manquante...), l'erreur est affichée par Program.Main
            renderer = new GameRenderer();
            screens = new ScreenManager();
            UpdateTitle();

            gameTimer = new Timer();
            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;

            gameStopwatch.Start();
            gameTimer.Start();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            InputKey input;
            if (!TryMapKey(e.KeyCode, out input))
                return;

            // Les touches de service (crédit, start) ne se répètent pas quand on les maintient
            if (input == InputKey.Coin || input == InputKey.Start1 || input == InputKey.Start2 || input == InputKey.Record)
            {
                if (!heldServiceKeys.Add(e.KeyCode))
                    return;
            }

            screens.OnInput(input);
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            heldServiceKeys.Remove(e.KeyCode);
        }

        private static bool TryMapKey(Keys key, out InputKey input)
        {
            switch (key)
            {
                case Keys.Up: input = InputKey.Up; return true;
                case Keys.Down: input = InputKey.Down; return true;
                case Keys.Left: input = InputKey.Left; return true;
                case Keys.Right: input = InputKey.Right; return true;

                case Keys.D5:
                case Keys.NumPad5: input = InputKey.Coin; return true;

                case Keys.D1:
                case Keys.NumPad1: input = InputKey.Start1; return true;

                case Keys.D2:
                case Keys.NumPad2: input = InputKey.Start2; return true;
#if DEBUG
                case Keys.F9: input = InputKey.Record; return true;
#endif
                default: input = InputKey.Coin; return false;
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
                screens.Update();
                accumulatedTimeMs -= TargetFrameTime;
            }

            UpdateTitle();
            Invalidate();
        }

        private void UpdateTitle()
        {
            PlayScreen play = screens.Current as PlayScreen;

            if (play != null && play.Game != null)
            {
                Game game = play.Game;
                this.Text = $"Pac-Man Arcade (C#) - J{game.ActivePlayer} - SCORE: {game.Score} - VIES: {game.Lives} - Gommes: {game.DotsRemaining}";
            }
            else if (screens.Current is DemoScreen demo && demo.IsRecording)
            {
                this.Text = "Pac-Man Arcade (C#) - ENREGISTREMENT DE LA DÉMO";
            }
            else
            {
                this.Text = $"Pac-Man Arcade (C#) - CRÉDITS: {screens.Credits}";
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            renderer.Render(screens.Current, e.Graphics);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (gameTimer != null) gameTimer.Stop();
            if (screens != null) screens.Dispose();
            if (renderer != null) renderer.Dispose();

            base.OnFormClosed(e);
        }
    }
}