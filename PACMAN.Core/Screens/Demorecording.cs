using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PacmanWinForms
{
    /// <summary>
    /// Suite de changements de direction (numéro de frame, direction) qui rejoue la démo de l'attract mode.
    /// Format du fichier texte : une ligne par entrée, "frame direction" (ex. : "312 Left"). Les lignes vides
    /// et celles qui commencent par # sont ignorées.
    /// </summary>
    public sealed class DemoRecording
    {
        public const string FileName = "demo.txt";

        public struct Entry
        {
            public int Frame;
            public Direction Direction;
        }

        private readonly List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries { get { return entries; } }

        public void Add(int frame, Direction direction)
        {
            Entry e;
            e.Frame = frame;
            e.Direction = direction;
            entries.Add(e);
        }

        /// <summary>
        /// Charge la démo embarquée dans l'exécutable. Renvoie null si elle n'existe pas (pas encore enregistrée).
        /// </summary>
        public static DemoRecording LoadEmbedded(string resourceName)
        {
            using (Stream stream = Assets.TryOpen(resourceName))
            {
                if (stream == null) return null;

                using (StreamReader reader = new StreamReader(stream))
                {
                    return Parse(reader);
                }
            }
        }

        public static DemoRecording Parse(TextReader reader)
        {
            DemoRecording recording = new DemoRecording();
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int frame;
                Direction direction;

                if (parts.Length == 2
                    && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out frame)
                    && Enum.TryParse(parts[1], true, out direction)
                    && direction != Direction.None)
                {
                    recording.Add(frame, direction);
                }
            }

            return recording;
        }

        public void Save(string path)
        {
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                writer.WriteLine("# Démo de l'attract mode : frame direction");
                foreach (Entry e in entries)
                {
                    writer.WriteLine(e.Frame.ToString(CultureInfo.InvariantCulture) + " " + e.Direction);
                }
            }
        }
    }
}