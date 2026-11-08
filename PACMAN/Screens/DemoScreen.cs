using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PacmanWinForms
{
    /// <summary>
    /// La démo de l'attract mode : une partie jouée par une suite de touches enregistrée, sans le son.
    /// En mode enregistrement (touche F9), le même écran fait jouer une vraie partie et note les touches.
    /// </summary>
    public sealed class DemoScreen : IScreen
    {
        private readonly ScreenManager manager;
        private readonly bool recordMode;

        private Game game;
        private DemoRecording recording;
        private int nextEntry;
        private int frame;      // Nombre de mises à jour du jeu déjà faites

        public bool IsRecording { get { return recordMode; } }

        public DemoScreen(ScreenManager manager, bool recordMode)
        {
            this.manager = manager;
            this.recordMode = recordMode;
        }

        public void Enter()
        {
            frame = 0;
            nextEntry = 0;

            if (recordMode)
            {
                // On enregistre avec le son : il n'influence pas la logique du jeu
                recording = new DemoRecording();
            }
            else
            {
                recording = DemoRecording.LoadEmbedded();

                if (recording == null)
                {
                    // Pas encore de démo enregistrée : on retourne au début de l'attract mode
                    manager.ShowAttract();
                    return;
                }

                manager.Sound.Muted = true;
            }

            game = new Game(manager.Sound, manager.HighScore, isDemo: true);
        }

        public void Update()
        {
            if (game == null) return;

            if (!recordMode)
            {
                // Les touches enregistrées à la frame N sont appliquées juste avant la (N+1)e mise à jour
                while (nextEntry < recording.Entries.Count && recording.Entries[nextEntry].Frame <= frame)
                {
                    game.HandleInput(recording.Entries[nextEntry].Direction);
                    nextEntry++;
                }
            }

            game.Update();
            frame++;

            if (game.IsFinished)
            {
                if (recordMode) SaveRecording();

                manager.ShowAttract();
            }
        }

        public void OnInput(InputKey key)
        {
            if (!recordMode || game == null) return;

            Direction direction = key.ToDirection();
            if (direction != Direction.None)
            {
                game.HandleInput(direction);
                recording.Add(frame, direction);
            }
        }

        public void Draw(GameRenderer renderer, Graphics g)
        {
            if (game == null) return;

            renderer.DrawGame(g, game);
            renderer.DrawCredits(g, manager.Credits);
        }

        private void SaveRecording()
        {
            string path = Path.Combine(AppContext.BaseDirectory, DemoRecording.FileName);
            string message;

            try
            {
                recording.Save(path);
                message = "Démo enregistrée (" + recording.Entries.Count + " touches) :\n" + path
                          + "\n\nCopie ce fichier dans asset\\demo.txt du projet, puis recompile.";
            }
            catch (Exception ex)
            {
                message = "Impossible d'enregistrer la démo : " + ex.Message;
            }

            // Dans une autre tâche, pour ne pas bloquer la boucle de jeu pendant que le message est affiché
            Task.Run(() => MessageBox.Show(message, "Enregistrement de la démo"));
        }
    }
}