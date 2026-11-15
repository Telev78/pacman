using System;

namespace PacmanWinForms
{
    /// <summary>
    /// Enchaîne les écrans de l'application et gère ce qui les traverse : crédits, high score, son.
    /// </summary>
    public sealed class ScreenManager : IDisposable
    {
        public const int MaxCredits = 99;

        private readonly IAudioEngine audio;
        private readonly SoundDirector sound;
        private string loadedSoundFolder;
        private IScreen current;

        public int Credits { get; private set; }
        public int HighScore { get; private set; }

        /// <summary>
        /// Appelé quand le jeu veut informer l'utilisateur (titre, message). L'hôte le branche sur une boîte
        /// de dialogue ou autre ; sans hôte, le message est simplement ignoré.
        /// </summary>
        public Action<string, string> NotifyHandler { get; set; }

        public void Notify(string title, string message)
        {
            Action<string, string> handler = NotifyHandler;
            if (handler != null) handler(title, message);
        }

        public SoundDirector Sound { get { return sound; } }

        /// <summary>Le jeu choisi (Pac-Man, Ms Pac-Man) : labyrinthes, fantômes, départ.</summary>
        public GameVariant Variant { get; private set; }
        public IScreen Current { get { return current; } }

        /// <summary>Appelé quand un jeu est choisi (menu ou démarrage direct) : l'hôte y branche les sprites.</summary>
        public event Action<GameVariant> VariantChanged;

        /// <param name="variant">Jeu à lancer directement ; null pour afficher d'abord le menu de choix.</param>
        public ScreenManager(IAudioEngine audio, GameVariant variant = null)
        {
            this.audio = audio;
            sound = new SoundDirector(audio);

            if (variant != null) SelectVariant(variant);
            else ShowMenu();
        }

        public void ShowMenu()
        {
            // Quitter un jeu, c'est changer de borne : crédits et high score repartent de zéro
            Credits = 0;
            HighScore = 0;

            SwitchTo(new GameSelectScreen(this));
        }

        /// <summary>Choisit le jeu (charge ses sons, prévient l'hôte) et démarre son attract mode.</summary>
        public void SelectVariant(GameVariant variant)
        {
            ApplyVariant(variant);
            ShowAttract();
        }

        /// <summary>Menu de test : liste toutes les intermissions de tous les jeux.</summary>
        public void ShowIntermissionMenu()
        {
            SwitchTo(new IntermissionMenuScreen(this));
        }

        /// <summary>Joue une intermission seule (test), puis revient au menu des intermissions.</summary>
        public void PlayIntermissionTest(GameVariant variant, int number, int level)
        {
            ApplyVariant(variant);
            SwitchTo(new IntermissionTestScreen(this, number, level));
        }

        private void ApplyVariant(GameVariant variant)
        {
            if (loadedSoundFolder != variant.SoundFolder)
            {
                audio.LoadSoundPack(variant.SoundFolder);
                loadedSoundFolder = variant.SoundFolder;
            }

            Variant = variant;
            Action<GameVariant> handler = VariantChanged;
            if (handler != null) handler(variant);
        }

        public void Update()
        {
            current.Update();
        }

        public void OnInput(InputKey key)
        {
            // Le menu gère ses touches lui-même (pas de crédit ni de son avant le choix du jeu)
            if (current is GameSelectScreen)
            {
                current.OnInput(key);
                return;
            }

            if (key == InputKey.Menu)
            {
                if (current is IntermissionTestScreen) ShowIntermissionMenu();
                else if (current is IntermissionMenuScreen) ShowMenu();
                else if (current is IAttractScreen || current is IStartScreen || (current is DemoScreen d && !d.IsRecording))
                    ShowMenu();
                return;
            }

            // Un crédit peut être inséré à tout moment, quel que soit l'écran
            if (key == InputKey.Coin)
            {
                InsertCoin();
                return;
            }

            if (key == InputKey.Record)
            {
                // Réservé au développement : depuis l'attract mode ou l'écran start
                if (current is IAttractScreen || current is IStartScreen)
                    SwitchTo(new DemoScreen(this, recordMode: true));
                return;
            }

            current.OnInput(key);
        }

        private void InsertCoin()
        {
            if (Credits < MaxCredits) Credits++;

            // En attract mode, un crédit mène directement à l'écran start
            if (current is IAttractScreen || (current is DemoScreen demo && !demo.IsRecording))
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
            SwitchTo(Variant.CreateAttractScreen(this));
        }

        public void ShowStart()
        {
            SwitchTo(Variant.CreateStartScreen(this));
        }

        // Lance une partie (1 crédit par joueur)
        public void StartGame(int players)
        {
            if (Credits < players) return;

            Credits -= players;
            SwitchTo(new PlayScreen(this, players));
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