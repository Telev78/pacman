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

        private bool fullscreen;

        private int volumeShownFrames = 0;   // Compte à rebours de l'indicateur de volume

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

            this.Text = "Pac-Man";
            this.MaximizeBox = false;
            this.ResizeRedraw = true;
            this.BackColor = Color.Black;
            this.StartPosition = FormStartPosition.CenterScreen;
            SetWindowed();
            if (Settings.StartFullscreen) SetFullscreen();
            this.MaximizeBox = false;
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyDown += new KeyEventHandler(OnKeyDown);
            this.KeyUp += new KeyEventHandler(OnKeyUp);

            // Si l'un des deux échoue (ressource manquante...), l'erreur est affichée par Program.Main
            renderer = new GameRenderer();
            screens = new ScreenManager(new AudioEngine());
            UpdateTitle();

            gameTimer = new Timer();
            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;

            gameStopwatch.Start();
            gameTimer.Start();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Alt && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ToggleFullscreen();
                return;
            }
            if (e.KeyCode == Keys.F11 || e.KeyCode == Keys.F12)
            {
                e.SuppressKeyPress = true;
                int volume = Settings.Volume + (e.KeyCode == Keys.F12 ? 10 : -10);
                Settings.SetVolume(volume);
                screens.Sound.Volume = Settings.Volume / 100f;
                volumeShownFrames = 120;   // ≈ 2 secondes
                return;
            }

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
            if (volumeShownFrames > 0) volumeShownFrames--;
            Invalidate();
        }

        private void UpdateTitle()
        {
#if DEBUG
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
#endif
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            renderer.Render(screens.Current, e.Graphics, ComputeDestination());
            if (volumeShownFrames > 0)
            {
                Rectangle dest = ComputeDestination();
                string text = "VOLUME " + Settings.Volume + "%";
                using (Font font = new Font("Consolas", Math.Max(10, dest.Height / 40f), FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    SizeF size = e.Graphics.MeasureString(text, font);
                    float x = dest.Left + 8, y = dest.Bottom - size.Height - 8;
                    e.Graphics.FillRectangle(Brushes.Black, x - 4, y - 2, size.Width + 8, size.Height + 4);
                    e.Graphics.DrawString(text, font, Brushes.White, x, y);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (gameTimer != null) gameTimer.Stop();
            if (screens != null) screens.Dispose();
            if (renderer != null) renderer.Dispose();

            base.OnFormClosed(e);
        }

        private void SetWindowed()
        {
            fullscreen = false;
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.ClientSize = new Size(DisplayConfig.NativeWidth * Settings.Zoom, DisplayConfig.NativeHeight * Settings.Zoom);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.CenterToScreen();
            Cursor.Show();
        }

        private void SetFullscreen()
        {
            fullscreen = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Normal;   // Nécessaire pour reprendre les limites de l'écran
            this.Bounds = Screen.FromControl(this).Bounds;
            Cursor.Hide();
        }

        private void ToggleFullscreen()
        {
            if (fullscreen) SetWindowed(); else SetFullscreen();
        }

        // Plus grand multiple entier qui tient dans la fenêtre, centré (bandes noires autour) ;
        // si même x1 ne tient pas, on réduit proportionnellement
        private Rectangle ComputeDestination()
        {
            int w = this.ClientSize.Width, h = this.ClientSize.Height;
            int scale = Math.Min(w / DisplayConfig.NativeWidth, h / DisplayConfig.NativeHeight);

            int dw, dh;
            if (scale >= 1)
            {
                dw = DisplayConfig.NativeWidth * scale;
                dh = DisplayConfig.NativeHeight * scale;
            }
            else
            {
                double f = Math.Min((double)w / DisplayConfig.NativeWidth, (double)h / DisplayConfig.NativeHeight);
                dw = (int)(DisplayConfig.NativeWidth * f);
                dh = (int)(DisplayConfig.NativeHeight * f);
            }
            return new Rectangle((w - dw) / 2, (h - dh) / 2, dw, dh);
        }
    }
}