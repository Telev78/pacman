using System;
using System.IO;

namespace PacmanWinForms
{
    /// <summary>
    /// Décodeur WAV minimal, sans dépendance : PCM 16 bits, mono ou stéréo, à n'importe quelle fréquence.
    /// Renvoie des échantillons flottants stéréo entrelacés (gauche, droite, gauche, droite...)
    /// à la fréquence demandée (rééchantillonnage linéaire si le fichier est à une autre fréquence).
    /// </summary>
    public static class WavDecoder
    {
        public static float[] Decode(Stream stream, int targetSampleRate)
        {
            byte[] file;
            using (MemoryStream copy = new MemoryStream())
            {
                stream.CopyTo(copy);
                file = copy.ToArray();
            }

            if (file.Length < 12 || file[0] != 'R' || file[1] != 'I' || file[2] != 'F' || file[3] != 'F'
                || file[8] != 'W' || file[9] != 'A' || file[10] != 'V' || file[11] != 'E')
            {
                throw new InvalidDataException("ce n'est pas un fichier WAV");
            }

            int formatTag = 0, channels = 0, sampleRate = 0, bits = 0;
            int dataStart = -1, dataLength = 0;

            int pos = 12;
            while (pos + 8 <= file.Length)
            {
                string id = "" + (char)file[pos] + (char)file[pos + 1] + (char)file[pos + 2] + (char)file[pos + 3];
                int length = BitConverter.ToInt32(file, pos + 4);
                int body = pos + 8;

                if (id == "fmt " && length >= 16)
                {
                    formatTag = BitConverter.ToUInt16(file, body);
                    channels = BitConverter.ToUInt16(file, body + 2);
                    sampleRate = BitConverter.ToInt32(file, body + 4);
                    bits = BitConverter.ToUInt16(file, body + 14);
                }
                else if (id == "data")
                {
                    dataStart = body;
                    // Certains éditeurs écrivent une taille fausse : on borne à ce qui reste dans le fichier
                    dataLength = (length < 0 || body + length > file.Length) ? file.Length - body : length;
                    break;
                }

                if (length < 0) break;
                pos = body + length + (length & 1);   // Les chunks sont alignés sur 2 octets
            }

            if (formatTag != 1) throw new InvalidDataException("seul le WAV PCM est géré");
            if (bits != 16) throw new InvalidDataException("seuls 16 bits par échantillon sont gérés (trouvé : " + bits + ")");
            if (channels != 1 && channels != 2) throw new InvalidDataException("nombre de canaux non géré : " + channels);
            if (sampleRate <= 0) throw new InvalidDataException("fréquence invalide");
            if (dataStart < 0) throw new InvalidDataException("pas de données audio");

            // 1. Octets -> flottants stéréo à la fréquence du fichier
            int frames = dataLength / (2 * channels);
            float[] stereo = new float[frames * 2];
            for (int i = 0; i < frames; i++)
            {
                int p = dataStart + i * 2 * channels;
                float left = BitConverter.ToInt16(file, p) / 32768f;
                float right = (channels == 2) ? BitConverter.ToInt16(file, p + 2) / 32768f : left;
                stereo[i * 2] = left;
                stereo[i * 2 + 1] = right;
            }

            if (sampleRate == targetSampleRate) return stereo;

            // 2. Rééchantillonnage linéaire vers la fréquence de sortie
            int outFrames = (int)((long)frames * targetSampleRate / sampleRate);
            float[] result = new float[outFrames * 2];
            double step = (double)sampleRate / targetSampleRate;
            for (int i = 0; i < outFrames; i++)
            {
                double src = i * step;
                int i0 = (int)src;
                int i1 = Math.Min(i0 + 1, frames - 1);
                float t = (float)(src - i0);
                for (int c = 0; c < 2; c++)
                {
                    float a = stereo[i0 * 2 + c];
                    float b = stereo[i1 * 2 + c];
                    result[i * 2 + c] = a + (b - a) * t;
                }
            }
            return result;
        }
    }
}
