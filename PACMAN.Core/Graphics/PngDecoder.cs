using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace PacmanWinForms
{
    /// <summary>
    /// Décodeur PNG minimal, sans dépendance : suffit pour la feuille de sprites du jeu.
    /// Gère 8 bits par canal, sans entrelacement, en niveaux de gris (avec ou sans alpha),
    /// RVB, RVBA et palette (avec transparence tRNS). Les chunks de couleur (gAMA, sRGB...) sont ignorés,
    /// comme le faisait GDI+.
    /// </summary>
    public static class PngDecoder
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        public static Canvas Decode(Stream stream)
        {
            byte[] file = ReadAll(stream);

            if (file.Length < Signature.Length) throw new InvalidDataException("Fichier PNG trop court.");
            for (int i = 0; i < Signature.Length; i++)
            {
                if (file[i] != Signature[i]) throw new InvalidDataException("Ce fichier n'est pas un PNG.");
            }

            int width = 0, height = 0, bitDepth = 0, colorType = 0, interlace = 0;
            bool hasHeader = false;
            byte[] palette = null;
            byte[] paletteAlpha = null;
            MemoryStream compressed = new MemoryStream();

            int pos = Signature.Length;
            while (pos + 8 <= file.Length)
            {
                int length = ReadInt32(file, pos);
                string type = Encoding.ASCII.GetString(file, pos + 4, 4);
                int dataStart = pos + 8;

                if (length < 0 || dataStart + length > file.Length)
                    throw new InvalidDataException("PNG tronqué ou corrompu (chunk " + type + ").");

                if (type == "IHDR")
                {
                    width = ReadInt32(file, dataStart);
                    height = ReadInt32(file, dataStart + 4);
                    bitDepth = file[dataStart + 8];
                    colorType = file[dataStart + 9];
                    interlace = file[dataStart + 12];
                    hasHeader = true;
                }
                else if (type == "PLTE")
                {
                    palette = new byte[length];
                    Array.Copy(file, dataStart, palette, 0, length);
                }
                else if (type == "tRNS")
                {
                    paletteAlpha = new byte[length];
                    Array.Copy(file, dataStart, paletteAlpha, 0, length);
                }
                else if (type == "IDAT")
                {
                    compressed.Write(file, dataStart, length);
                }
                else if (type == "IEND")
                {
                    break;
                }

                pos = dataStart + length + 4;   // + 4 octets de CRC
            }

            if (!hasHeader) throw new InvalidDataException("PNG sans en-tête (IHDR).");
            if (width <= 0 || height <= 0) throw new InvalidDataException("Dimensions PNG invalides.");
            if (bitDepth != 8) throw new NotSupportedException("PNG : seuls 8 bits par canal sont gérés (trouvé : " + bitDepth + ").");
            if (interlace != 0) throw new NotSupportedException("PNG : les images entrelacées ne sont pas gérées.");

            int channels;
            switch (colorType)
            {
                case 0: channels = 1; break;   // Niveaux de gris
                case 2: channels = 3; break;   // RVB
                case 3: channels = 1; break;   // Palette
                case 4: channels = 2; break;   // Niveaux de gris + alpha
                case 6: channels = 4; break;   // RVBA
                default: throw new NotSupportedException("PNG : type de couleur non géré (" + colorType + ").");
            }
            if (colorType == 3 && palette == null) throw new InvalidDataException("PNG à palette sans chunk PLTE.");

            int stride = width * channels;                  // Octets par ligne (sans l'octet de filtre)
            byte[] raw = new byte[(stride + 1) * height];   // Chaque ligne commence par son octet de filtre

            compressed.Position = 0;
            using (ZLibStream zlib = new ZLibStream(compressed, CompressionMode.Decompress))
            {
                int total = 0;
                while (total < raw.Length)
                {
                    int read = zlib.Read(raw, total, raw.Length - total);
                    if (read <= 0) throw new InvalidDataException("Données d'image PNG incomplètes.");
                    total += read;
                }
            }

            Canvas canvas = new Canvas(width, height);
            byte[] previous = new byte[stride];   // La ligne "au-dessus" de la première est toute noire (zéros)
            byte[] current = new byte[stride];

            for (int y = 0; y < height; y++)
            {
                int rowStart = y * (stride + 1);
                int filter = raw[rowStart];
                int src = rowStart + 1;

                for (int x = 0; x < stride; x++)
                {
                    int a = x >= channels ? current[x - channels] : 0;    // Octet à gauche
                    int b = previous[x];                                   // Octet au-dessus
                    int c = x >= channels ? previous[x - channels] : 0;   // Octet au-dessus à gauche
                    int value = raw[src + x];

                    switch (filter)
                    {
                        case 0: break;
                        case 1: value += a; break;
                        case 2: value += b; break;
                        case 3: value += (a + b) / 2; break;
                        case 4: value += Paeth(a, b, c); break;
                        default: throw new InvalidDataException("PNG : filtre inconnu (" + filter + ").");
                    }

                    current[x] = (byte)(value & 0xFF);
                }

                for (int x = 0; x < width; x++)
                {
                    canvas.Pixels[y * width + x] = ToArgb(current, x * channels, colorType, palette, paletteAlpha);
                }

                byte[] swap = previous;
                previous = current;
                current = swap;
            }

            return canvas;
        }

        private static uint ToArgb(byte[] row, int i, int colorType, byte[] palette, byte[] paletteAlpha)
        {
            uint r, g, b, a;

            switch (colorType)
            {
                case 6:
                    r = row[i]; g = row[i + 1]; b = row[i + 2]; a = row[i + 3];
                    break;
                case 2:
                    r = row[i]; g = row[i + 1]; b = row[i + 2]; a = 255;
                    break;
                case 0:
                    r = g = b = row[i]; a = 255;
                    break;
                case 4:
                    r = g = b = row[i]; a = row[i + 1];
                    break;
                default:   // 3 : palette
                    int index = row[i];
                    int p = index * 3;
                    if (p + 2 >= palette.Length) throw new InvalidDataException("PNG : index de palette invalide.");
                    r = palette[p]; g = palette[p + 1]; b = palette[p + 2];
                    a = (paletteAlpha != null && index < paletteAlpha.Length) ? paletteAlpha[index] : 255u;
                    break;
            }

            return (a << 24) | (r << 16) | (g << 8) | b;
        }

        private static int Paeth(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);

            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }

        private static int ReadInt32(byte[] data, int i)
        {
            return (data[i] << 24) | (data[i + 1] << 16) | (data[i + 2] << 8) | data[i + 3];
        }

        private static byte[] ReadAll(Stream stream)
        {
            using (MemoryStream copy = new MemoryStream())
            {
                stream.CopyTo(copy);
                return copy.ToArray();
            }
        }
    }
}
