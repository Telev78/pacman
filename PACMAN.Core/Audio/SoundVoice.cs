using System;

namespace PacmanWinForms
{
    /// <summary>
    /// Un son décodé en mémoire (échantillons flottants stéréo entrelacés, à la fréquence de sortie du moteur).
    /// </summary>
    internal sealed class SoundData
    {
        public SoundId Id { get; private set; }
        public float[] Samples { get; private set; }

        public SoundData(SoundId id, float[] samples)
        {
            Id = id;
            Samples = samples;
        }
    }

    /// <summary>
    /// Une "voix" en cours de lecture : joue un son (une fois ou en boucle),
    /// puis éventuellement enchaîne sur un second son sans aucun trou.
    /// MixInto() est appelée par le thread audio ; Stop() par le thread du jeu.
    /// </summary>
    internal sealed class SoundVoice
    {
        private SoundData current;
        private SoundData next;
        private bool loopCurrent;
        private bool loopNext;
        private int position = 0;

        private volatile bool stopRequested = false;
        private volatile bool ended = false;

        public SoundId FirstId { get; private set; }
        public SoundId? ChainedId { get; private set; }

        public bool IsFinished { get { return stopRequested || ended; } }

        public SoundVoice(SoundData first, bool loopFirst, SoundData chained, bool loopChained)
        {
            current = first;
            loopCurrent = loopFirst;
            next = chained;
            loopNext = loopChained;

            FirstId = first.Id;
            if (chained != null) ChainedId = chained.Id;
        }

        public void Stop()
        {
            stopRequested = true;
        }

        /// <summary>
        /// Ajoute les prochains échantillons de cette voix à ceux déjà présents dans le tampon.
        /// </summary>
        public void MixInto(Span<float> buffer)
        {
            int written = 0;

            while (written < buffer.Length && !stopRequested)
            {
                float[] data = current.Samples;
                int available = data.Length - position;

                if (available <= 0)
                {
                    // Fin du son courant : on enchaîne, on boucle, ou on s'arrête
                    if (next != null)
                    {
                        current = next;
                        loopCurrent = loopNext;
                        next = null;
                        position = 0;
                        continue;
                    }

                    if (loopCurrent && data.Length > 0)
                    {
                        position = 0;
                        continue;
                    }

                    break;
                }

                int toCopy = Math.Min(available, buffer.Length - written);
                for (int i = 0; i < toCopy; i++)
                {
                    buffer[written + i] += data[position + i];
                }
                position += toCopy;
                written += toCopy;
            }

            if (written < buffer.Length)
            {
                ended = true;   // Plus rien à jouer : le moteur retirera cette voix
            }
        }
    }
}
