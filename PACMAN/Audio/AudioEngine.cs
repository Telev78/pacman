using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace PacmanWinForms
{
    /// <summary>
    /// Moteur audio de l'hôte Windows : le mélange est fait par MixerAudioEngine (dans PACMAN.Core),
    /// NAudio (WaveOut) ne sert qu'à envoyer les échantillons à la carte son.
    /// </summary>
    public sealed class AudioEngine : IAudioEngine
    {
        private readonly MixerAudioEngine mixer = new MixerAudioEngine();
        private IWavePlayer player;

        /// <summary>Volume général, de 0 (muet) à 1 (maximum).</summary>
        public float Volume
        {
            get { return mixer.Volume; }
            set { mixer.Volume = value; }
        }

        public AudioEngine()
        {
            mixer.Volume = Settings.Volume / 100f;

            try
            {
                WaveOut waveOut = new WaveOut();
                waveOut.BufferMilliseconds = 30;
                waveOut.Init(new SampleToWaveProvider(new MixerSampleProvider(mixer)));
                waveOut.Play();

                player = waveOut;
                mixer.OutputAvailable = true;
            }
            catch (Exception ex)
            {
                // Pas de périphérique audio : le jeu continue en silence
                System.Diagnostics.Debug.WriteLine("Audio indisponible : " + ex.Message);
            }
        }

        public void Play(SoundId id, bool loop = false) { mixer.Play(id, loop); }

        public void PlaySequence(SoundId first, SoundId next, bool loopNext = true) { mixer.PlaySequence(first, next, loopNext); }

        public void Stop(SoundId id) { mixer.Stop(id); }

        public void StopAll() { mixer.StopAll(); }

        public void Dispose()
        {
            mixer.OutputAvailable = false;

            if (player != null)
            {
                player.Stop();
                player.Dispose();
                player = null;
            }

            mixer.Dispose();
        }

        // Adaptateur : NAudio vient chercher ses échantillons dans le moteur de PACMAN.Core
        private sealed class MixerSampleProvider : ISampleProvider
        {
            private readonly MixerAudioEngine engine;

            public MixerSampleProvider(MixerAudioEngine engine)
            {
                this.engine = engine;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(MixerAudioEngine.SampleRate, MixerAudioEngine.Channels);
            }

            public WaveFormat WaveFormat { get; private set; }

            public int Read(Span<float> buffer)
            {
                return engine.Read(buffer);
            }
        }
    }
}
