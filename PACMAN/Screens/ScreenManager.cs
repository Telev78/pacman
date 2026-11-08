using System;

namespace PacmanWinForms
{
    /// <summary>
    /// Enchaîne les écrans de l'application et gère ce qui les traverse : crédits, high score, son.
    /// </summary>
    public sealed class ScreenManager : IDisposable
    {
        public const int MaxCredits = 99;

        private readonly SoundDirector sound = new SoundDirector();
        private IScreen current;

        public int Credits { get; private set; }
        public int HighScore { get; private set; }

        public SoundDirector Sound { get { return sound; } }
        public IScreen Current { get { return current; } }

        public ScreenManager()
        {
            ShowAttract();
        }

        public void Update()
        {
            current.Update();
        }

        public void OnInput(InputKey key)
        {
            // Un crédit peut être inséré à tout moment, quel que soit l'écran
            if (key == InputKey.Coin)
            {
                InsertCoin();
                return;
            }

            if (key == InputKey.Record)
            {
                // Réservé au développement : depuis l'attract mode ou l'écran start
                if (current is AttractScreen || current is StartScreen)
                    SwitchTo(new DemoScreen(this, recordMode: true));
                return;
            }

            current.OnInput(key);
        }

        private void InsertCoin()
        {
            if (Credits < MaxCredits) Credits++;

            // En attract mode, un crédit mène directement à l'écran start
            if (current is AttractScreen || (current is DemoScreen demo && !demo.IsRecording))
            {
                ShowStart();
            }

            sound.PlayCredit();
        }

        public void ShowDemo()
        {
            SwitchTo(new DemoScreen(this, recordMode: false));
        }

        public void ShowAttract()
        {
            SwitchTo(new AttractScreen(this));
        }

        public void ShowStart()
        {
            SwitchTo(new StartScreen(this));
        }

        // Lance une partie 1 joueur (coûte un crédit)
        public void StartGame()
        {
            if (Credits < 1) return;

            Credits--;
            SwitchTo(new PlayScreen(this));
        }

        // Appelé par l'écran de jeu quand le GAME OVER est terminé
        public void OnGameEnded(int finalHighScore)
        {
            HighScore = Math.Max(HighScore, finalHighScore);

            if (Credits > 0) ShowStart();
            else ShowAttract();
        }

        private void SwitchTo(IScreen screen)
        {
            sound.StopAll();
            sound.Muted = false;
            current = screen;
            current.Enter();
        }

        public void Dispose()
        {
            sound.CleanUp();
        }
    }
}