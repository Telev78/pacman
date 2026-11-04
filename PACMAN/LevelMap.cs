using System.Collections.Generic;
using System.IO;

namespace PacmanWinForms
{
    public class LevelMap
    {
        // 0 = Vide, 1 = Mur, 2 = Gomme, 3 = Super-Gomme, 4 = Porte du nid
        public int[,] Grid { get; private set; }

        // Index de la tuile visuelle dans maze.png, ou -1 si vide
        public int[,] VisualGrid { get; private set; }

        public LevelMap()
        {
            Grid = new int[GameConfig.MapHeight, GameConfig.MapWidth];
            VisualGrid = new int[GameConfig.MapHeight, GameConfig.MapWidth];

            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    VisualGrid[r, c] = -1;
                }
            }
        }

        /// <summary>
        /// Charge la carte (72 lignes) : 36 lignes de physique, puis 36 lignes de visuel.
        /// </summary>
        public void LoadMapFromStream(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                List<string> lines = new List<string>();
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }

                for (int r = 0; r < GameConfig.MapHeight; r++)
                {
                    string[] physics = lines[r].Split(',');
                    string[] visuals = lines[GameConfig.MapHeight + r].Split(',');

                    for (int c = 0; c < GameConfig.MapWidth; c++)
                    {
                        Grid[r, c] = int.Parse(physics[c]);
                        VisualGrid[r, c] = int.Parse(visuals[c]);
                    }
                }
            }
        }
    }
}