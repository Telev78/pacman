using System;
using System.Runtime.InteropServices;
using PacmanWinForms;
using SDL3;

namespace PacmanSdl
{
    /// <summary>
    /// Sortie sonore SDL : SDL demande régulièrement de nouveaux échantillons (thread audio),
    /// on les prend dans le MixerAudioEngine de PACMAN.Core.
    /// </summary>
    internal sealed class SdlAudioOutput : IDisposable
    {
        private readonly MixerAudioEngine mixer;
        private readonly SDL.AudioStreamCallback callback;   // Gardé dans un champ : le ramasse-miettes ne doit pas le libérer
        private readonly float[] buffer = new float[4096];
        private IntPtr stream = IntPtr.Zero;

        public SdlAudioOutput(MixerAudioEngine mixer)
        {
            this.mixer = mixer;
            this.callback = Feed;
        }

        /// <summary>Ouvre le périphérique par défaut. Renvoie false s'il n'y a pas de sortie audio.</summary>
        public bool Start()
        {
            SDL.AudioSpec spec = new SDL.AudioSpec
            {
                Format = BitConverter.IsLittleEndian ? SDL.AudioFormat.AudioF32LE : SDL.AudioFormat.AudioF32BE,
                Channels = MixerAudioEngine.Channels,
                Freq = MixerAudioEngine.SampleRate
            };

            stream = SDL.OpenAudioDeviceStream(SDL.AudioDeviceDefaultPlayback, in spec, callback, IntPtr.Zero);
            if (stream == IntPtr.Zero)
            {
                Console.Error.WriteLine("Audio indisponible : " + SDL.GetError());
                return false;
            }

            SDL.ResumeAudioStreamDevice(stream);
            return true;
        }

        private void Feed(IntPtr userdata, IntPtr audioStream, int additionalAmount, int totalAmount)
        {
            try
            {
                int remainingBytes = additionalAmount;

                while (remainingBytes > 0)
                {
                    int count = Math.Min(remainingBytes / sizeof(float), buffer.Length);
                    count -= count % MixerAudioEngine.Channels;   // Toujours des paires gauche/droite complètes
                    if (count <= 0) break;

                    Span<float> chunk = buffer.AsSpan(0, count);
                    mixer.Read(chunk);

                    ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(chunk);
                    SDL.PutAudioStreamData(audioStream, bytes, bytes.Length);
                    remainingBytes -= bytes.Length;
                }
            }
            catch (Exception ex)
            {
                // Une exception ne doit jamais remonter dans le code natif de SDL
                Console.Error.WriteLine("Erreur audio : " + ex.Message);
            }
        }

        public void Dispose()
        {
            if (stream != IntPtr.Zero)
            {
                SDL.DestroyAudioStream(stream);   // Ferme aussi le périphérique ouvert avec le flux
                stream = IntPtr.Zero;
            }
        }
    }
}
