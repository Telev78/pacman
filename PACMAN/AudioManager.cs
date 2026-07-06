using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media;

namespace PacmanWinForms
{
    public class AudioManager
    {
        private readonly Dictionary<string, string> tempAudioFiles = new Dictionary<string, string>();
        private readonly Dictionary<string, MediaPlayer> soundPlayers = new Dictionary<string, MediaPlayer>();

        // Dictionnaire pour stocker la suite logique : Key = Son Actuel, Value = (Prochain Son, Doit-il boucler ?)
        private readonly Dictionary<MediaPlayer, (string nextSoundKey, bool loopNext)> soundSequences =
            new Dictionary<MediaPlayer, (string, bool)>();

        public AudioManager()
        {
            // 1. Extraction en masse de la liste d'assets d'après ta capture d'écran
            string[] soundsToExtract = new string[]
            {
                "credit", "death_0", "death_1", "eat_dot_0", "eat_dot_1", "eat_fruit",
                "eat_ghost", "extend", "eyes", "eyes_firstloop", "fright", "fright_firstloop",
                "intermission", "siren0", "siren0_firstloop", "siren1", "siren1_firstloop",
                "siren2", "siren2_firstloop", "siren3", "siren3_firstloop", "siren4", "siren4_firstloop", "start"
            };

            foreach (string sound in soundsToExtract)
            {
                ExtractResourceToTemp(sound, $"asset.sounds.{sound}.wav");
                //InitializePlayer(sound);
            }
        }

        private void ExtractResourceToTemp(string soundKey, string resourcePath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string fullResourceName = $"PacmanWinForms.{resourcePath}";

            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Ressource audio introuvable : {fullResourceName}");
                }

                string tempPath = Path.Combine(Path.GetTempPath(), $"pacman_{soundKey}.wav");

                using (FileStream fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }

                tempAudioFiles[soundKey] = tempPath;
            }
        }

        private void InitializePlayer(string soundKey)
        {
            if (tempAudioFiles.TryGetValue(soundKey, out string filePath))
            {
                MediaPlayer player = new MediaPlayer();

                // 1. Activer le scrubbing permet de figer le lecteur sur la frame 0
                player.ScrubbingEnabled = true;

                // 2. Ouvrir l'URI
                player.Open(new Uri(filePath));

                // 3. Forcer la mise en pause IMMÉDIATE sur le thread
                player.Pause();

                // 4. Mettre la position à zéro au cas où des millisecondes auraient fui
                player.Position = TimeSpan.Zero;

                // On s'abonne à la fin du morceau
                player.MediaEnded += OnMediaEnded;

                soundPlayers[soundKey] = player;
            }
        }

        // --- GESTION DE LA LECTURE ---

        /// <summary>
        /// Joue un son immédiatement. Peut être joué en boucle.
        /// </summary>
        public void Play(string soundKey, bool loop = false)
        {
            if (!soundPlayers.ContainsKey(soundKey))
            {
                InitializePlayer(soundKey);
            }

            if (soundPlayers.TryGetValue(soundKey, out MediaPlayer player))
            {
                // Nettoyer d'anciennes séquences sur ce player s'il y en avait
                soundSequences.Remove(player);

                if (loop)
                {
                    // Configuration de secours : se relancer soi-même
                    soundSequences[player] = (soundKey, true);
                }

                player.Stop();
                player.Play();
            }
        }

        /// <summary>
        /// Joue un premier son (ex: intro/firstloop) et enchaîne automatiquement sur un second.
        /// </summary>
        public void PlaySequence(string firstSoundKey, string nextSoundKey, bool loopNext = true)
        {
            if (!soundPlayers.ContainsKey(firstSoundKey))
            {
                InitializePlayer(firstSoundKey);
            }

            if (!soundPlayers.ContainsKey(nextSoundKey))
            {
                InitializePlayer(nextSoundKey);
            }

            if (soundPlayers.TryGetValue(firstSoundKey, out MediaPlayer firstPlayer))
            {
                // On enregistre ce qui doit se passer à la fin du premier player
                soundSequences[firstPlayer] = (nextSoundKey, loopNext);

                firstPlayer.Stop();
                firstPlayer.Play();
            }
        }

        public void Stop(string soundKey)
        {
            if (soundPlayers.TryGetValue(soundKey, out MediaPlayer player))
            {
                player.Stop();
                soundSequences.Remove(player);
            }
        }

        public void StopAll()
        {
            foreach (var player in soundPlayers.Values)
            {
                player.Stop();
            }
            soundSequences.Clear();
        }

        // --- GESTIONNAIRE D'ÉVÉNEMENTS (FIN DE PISTE) ---

        private void OnMediaEnded(object sender, EventArgs e)
        {
            if (sender is MediaPlayer currentPlayer)
            {
                // Est-ce que ce player a une suite ou une boucle enregistrée ?
                if (soundSequences.TryGetValue(currentPlayer, out var sequence))
                {
                    // Si c'est une simple boucle sur lui-même
                    if (soundPlayers.TryGetValue(sequence.nextSoundKey, out MediaPlayer nextPlayer) && nextPlayer == currentPlayer)
                    {
                        currentPlayer.Position = TimeSpan.Zero;
                        currentPlayer.Play();
                    }
                    else
                    {
                        // C'est un enchaînement ! On arrête le premier proprement
                        currentPlayer.Stop();
                        soundSequences.Remove(currentPlayer);

                        // Et on lance le suivant avec ses propres paramètres de boucle
                        Play(sequence.nextSoundKey, sequence.loopNext);
                    }
                }
            }
        }

        // --- NETTOYAGE DES FICHIERS ---

        public void CleanUp()
        {
            foreach (var player in soundPlayers.Values)
            {
                player.MediaEnded -= OnMediaEnded;
                player.Close();
            }

            foreach (var filePath in tempAudioFiles.Values)
            {
                try
                {
                    if (File.Exists(filePath)) File.Delete(filePath);
                }
                catch { /* Fichier verrouillé temporairement par l'OS, ignoré */ }
            }
        }
    }
}