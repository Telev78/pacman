using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace PacmanWinForms
{
    /// <summary>
    /// Moteur audio : décode tous les sons au démarrage, puis les mélange et les joue via NAudio.
    /// </summary>
    public sealed class AudioEngine : IAudioEngine
    {
        public const int SampleRate = 44100;
        public const int Channels = 2;
        public static readonly WaveFormat OutputFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);

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

        private IWavePlayer player;
        private MixingSampleProvider mixer;
        private bool isAvailable = false;

        private VolumeSampleProvider volumeStage;

        /// <summary>Volume général, de 0 (muet) à 1 (maximum).</summary>
        public float Volume
        {
            get { return volumeStage != null ? volumeStage.Volume : 0f; }
            set { if (volumeStage != null) volumeStage.Volume = Math.Max(0f, Math.Min(1f, value)); }
        }

        public AudioEngine()
        {
            // 1. Décodage de tous les sons en mémoire (une seule fois)
            foreach (KeyValuePair<SoundId, string> entry in FileNames)
            {
                sounds[entry.Key] = LoadSound(entry.Key, entry.Value);
            }

            // 2. Sortie audio : mélangeur -> WaveOut
            try
            {
                mixer = new MixingSampleProvider(OutputFormat);
                mixer.ReadFully = true;

                volumeStage = new VolumeSampleProvider(mixer);
                volumeStage.Volume = Settings.Volume / 100f;

                WaveOut waveOut = new WaveOut();
                waveOut.BufferMilliseconds = 30;
                waveOut.Init(new SampleToWaveProvider(volumeStage));   // volumeStage et non plus mixer
                waveOut.Play();

                player = waveOut;
                isAvailable = true;
            }
            catch (Exception ex)
            {
                // Pas de périphérique audio : le jeu continue en silence
                System.Diagnostics.Debug.WriteLine("Audio indisponible : " + ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // Lecture
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
            if (!isAvailable) return;

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
            if (!isAvailable) return;

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
            if (!isAvailable) return;

            lock (sync)
            {
                voices.RemoveAll(v => v.IsFinished);

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

            mixer.AddMixerInput(voice);
        }

        // ------------------------------------------------------------------
        // Décodage des WAV embarqués
        // ------------------------------------------------------------------

        private static SoundData LoadSound(SoundId id, string fileName)
        {
            string resourceName = $"asset.sounds.{fileName}.wav";

            using (Stream stream = Assets.TryOpen(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Ressource audio introuvable : {resourceName}");
                }

                try
                {
                    using (WaveFileReader reader = new WaveFileReader(stream))
                    {
                        ISampleProvider provider = reader.ToSampleProvider();

                        // On convertit tout vers le format de sortie : stéréo, 44,1 kHz
                        if (provider.WaveFormat.Channels == 1)
                        {
                            provider = new MonoToStereoSampleProvider(provider);
                        }
                        else if (provider.WaveFormat.Channels != 2)
                        {
                            throw new InvalidDataException("nombre de canaux non géré : " + provider.WaveFormat.Channels);
                        }

                        if (provider.WaveFormat.SampleRate != SampleRate)
                        {
                            provider = new WdlResamplingSampleProvider(provider, SampleRate);
                        }

                        return new SoundData(id, ReadAll(provider));
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Impossible de décoder {resourceName} ({ex.Message}). Le fichier doit être un WAV PCM.", ex);
                }
            }
        }

        private static float[] ReadAll(ISampleProvider provider)
        {
            List<float> samples = new List<float>();
            float[] buffer = new float[SampleRate * Channels]; // 1 seconde

            int read;
            while ((read = provider.Read(buffer.AsSpan())) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    samples.Add(buffer[i]);
                }
            }

            return samples.ToArray();
        }

        public void Dispose()
        {
            lock (sync)
            {
                isAvailable = false;
                voices.Clear();
            }

            if (player != null)
            {
                player.Stop();
                player.Dispose();
                player = null;
            }
        }
    }
}