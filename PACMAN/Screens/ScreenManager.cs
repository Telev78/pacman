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

            current.OnInput(key);
        }

        private void InsertCoin()
        {
            if (Credits < MaxCredits) Credits++;

            // En attract mode, un crédit mène directement à l'écran start
            if (current is AttractScreen)
            {
                ShowStart();
            }

            sound.PlayCredit();
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
            current = screen;
            current.Enter();
        }

        public void Dispose()
        {
            sound.CleanUp();
        }
    }
}