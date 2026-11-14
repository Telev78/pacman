using System;
using System.Collections.Generic;
using System.IO;

namespace PacmanWinForms
{
    /// <summary>
    /// Moteur audio sans périphérique : décode les sons du jeu, les mélange, et fournit les échantillons
    /// à qui les demande via Read(). C'est l'hôte (NAudio sous WinForms, SDL ensuite) qui possède la sortie
    /// sonore réelle : il appelle Read() depuis son thread audio pour remplir ses tampons.
    /// </summary>
    public sealed class MixerAudioEngine : IAudioEngine
    {
        /// <summary>Fréquence de sortie : celle des WAV du jeu (48 kHz), donc aucun rééchantillonnage.</summary>
        public const int SampleRate = 48000;
        public const int Channels = 2;

        // Nom du fichier WAV (dans asset\sounds) de chaque son
        private static readonly Dictionary<SoundId, string> FileNames = new Dictionary<SoundId, string>
        {
            { SoundId.Credit, "credit" },
            { SoundId.Death0, "death_0" },
            { SoundId.Death1, "death_1" },
            { SoundId.EatDot0, "eat_dot_0" },
            { SoundId.EatDot1, "eat_dot_1" },
            { SoundId.EatFruit, "eat_fruit" },
            { SoundId.EatGhost, "eat_ghost" },
            { SoundId.Extend, "extend" },
            { SoundId.Eyes, "eyes" },
            { SoundId.EyesFirstLoop, "eyes_firstloop" },
            { SoundId.Fright, "fright" },
            { SoundId.FrightFirstLoop, "fright_firstloop" },
            { SoundId.Intermission, "intermission" },
            { SoundId.Siren0, "siren0" },
            { SoundId.Siren0FirstLoop, "siren0_firstloop" },
            { SoundId.Siren1, "siren1" },
            { SoundId.Siren1FirstLoop, "siren1_firstloop" },
            { SoundId.Siren2, "siren2" },
            { SoundId.Siren2FirstLoop, "siren2_firstloop" },
            { SoundId.Siren3, "siren3" },
            { SoundId.Siren3FirstLoop, "siren3_firstloop" },
            { SoundId.Siren4, "siren4" },
            { SoundId.Siren4FirstLoop, "siren4_firstloop" },
            { SoundId.Start, "start" }
        };

        private readonly Dictionary<SoundId, SoundData> sounds = new Dictionary<SoundId, SoundData>();
        private readonly List<SoundVoice> voices = new List<SoundVoice>();
        private readonly object sync = new object();

        private volatile float volume = 1f;
        private volatile bool outputAvailable = false;

        /// <summary>Volume général, de 0 (muet) à 1 (maximum).</summary>
        public float Volume
        {
            get { return volume; }
            set { volume = Math.Max(0f, Math.Min(1f, value)); }
        }

        /// <summary>
        /// À mettre à true par l'hôte quand un périphérique de sortie lit réellement les échantillons.
        /// Sinon (pas de carte son), les sons demandés sont ignorés au lieu de s'accumuler.
        /// </summary>
        public bool OutputAvailable
        {
            get { return outputAvailable; }
            set { outputAvailable = value; }
        }

        /// <summary>
        /// Moteur sans aucun son : l'hôte appelle ensuite LoadSoundPack (fait par ScreenManager) pour le jeu choisi.
        /// </summary>
        public MixerAudioEngine()
        {
        }

        /// <summary>
        /// Décode tous les sons d'un jeu en mémoire (une fois par choix de jeu) et remplace les précédents.
        /// À appeler depuis le thread du jeu.
        /// </summary>
        public void LoadSoundPack(string resourceFolder)
        {
            Dictionary<SoundId, SoundData> loaded = new Dictionary<SoundId, SoundData>();
            foreach (KeyValuePair<SoundId, string> entry in FileNames)
            {
                loaded[entry.Key] = LoadSound(entry.Key, resourceFolder, entry.Value);
            }

            StopAll();
            sounds.Clear();
            foreach (KeyValuePair<SoundId, SoundData> entry in loaded)
            {
                sounds[entry.Key] = entry.Value;
            }
        }

        // ------------------------------------------------------------------
        // Sortie : appelée par le thread audio de l'hôte
        // ------------------------------------------------------------------

        /// <summary>
        /// Remplit le tampon d'échantillons stéréo entrelacés (silence s'il n'y a rien à jouer).
        /// Renvoie toujours buffer.Length : le flux audio ne s'arrête jamais.
        /// </summary>
        public int Read(Span<float> buffer)
        {
            buffer.Clear();

            lock (sync)
            {
                foreach (SoundVoice voice in voices)
                {
                    voice.MixInto(buffer);
                }
                voices.RemoveAll(v => v.IsFinished);
            }

            float gain = volume;
            if (gain != 1f)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] *= gain;
                }
            }

            return buffer.Length;
        }

        // ------------------------------------------------------------------
        // Commandes : appelées par le thread du jeu
        // ------------------------------------------------------------------

        /// <summary>
        /// Joue un son depuis le début (relance le son s'il jouait déjà). Peut boucler.
        /// </summary>
        public void Play(SoundId id, bool loop = false)
        {
            StartVoice(new SoundVoice(sounds[id], loop, null, false));
        }

        /// <summary>
        /// Joue un premier son (intro) puis enchaîne sans trou sur un second, éventuellement en boucle.
        /// </summary>
        public void PlaySequence(SoundId first, SoundId next, bool loopNext = true)
        {
            StartVoice(new SoundVoice(sounds[first], false, sounds[next], loopNext));
        }

        /// <summary>
        /// Coupe un son, qu'il soit en cours ou l'enchaînement d'une séquence.
        /// </summary>
        public void Stop(SoundId id)
        {
            lock (sync)
            {
                foreach (SoundVoice voice in voices)
                {
                    if (voice.FirstId == id || voice.ChainedId == id)
                    {
                        voice.Stop();
                    }
                }
                voices.RemoveAll(v => v.IsFinished);
            }
        }

        public void StopAll()
        {
            lock (sync)
            {
                foreach (SoundVoice voice in voices)
                {
                    voice.Stop();
                }
                voices.Clear();
            }
        }

        private void StartVoice(SoundVoice voice)
        {
            if (!outputAvailable) return;

            lock (sync)
            {
                // Rejouer un son le relance : on coupe la voix précédente qui jouait le même son
                foreach (SoundVoice existing in voices)
                {
                    if (existing.FirstId == voice.FirstId)
                    {
                        existing.Stop();
                    }
                }
                voices.RemoveAll(v => v.IsFinished);

                voices.Add(voice);
            }
        }

        // ------------------------------------------------------------------
        // Décodage des WAV embarqués
        // ------------------------------------------------------------------

        private static SoundData LoadSound(SoundId id, string resourceFolder, string fileName)
        {
            string resourceName = resourceFolder + fileName + ".wav";

            using (Stream stream = Assets.TryOpen(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("Ressource audio introuvable : " + resourceName);
                }

                try
                {
                    return new SoundData(id, WavDecoder.Decode(stream, SampleRate));
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException("Impossible de décoder " + resourceName + " (" + ex.Message + "). Le fichier doit être un WAV PCM 16 bits.", ex);
                }
            }
        }

        public void Dispose()
        {
            outputAvailable = false;
            StopAll();
        }
    }
}
