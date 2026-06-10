using System;
using System.IO;

namespace PacmanWinForms
{
    public class LevelMap
    {
        // 0 = Vide, 1 = Mur, 2 = Gomme, 3 = Super-Gomme, 4 = Porte du nid
        public int[,] Grid { get; private set; }

        // Stocke l'index de la tuile (0 à 47) ou -1 si vide
        public int[,] VisualGrid { get; private set; }

        public LevelMap()
        {
            // Initialisation des grilles vides aux dimensions d'origine
            Grid = new int[GameConfig.MapHeight, GameConfig.MapWidth];
            VisualGrid = new int[GameConfig.MapHeight, GameConfig.MapWidth];

            // Remplissage par défaut (sécurité)
            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    Grid[r, c] = 0;
                    VisualGrid[r, c] = -1;
                }
            }
        }

        /// <summary>
        /// Charge la carte complète (Physique + Visuel) depuis un unique fichier texte de 72 lignes
        /// </summary>
        public void LoadMapFromFile(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);

            // 1. Les 36 premières lignes = La physique (Grid)
            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                string[] tokens = lines[r].Split(',');
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    Grid[r, c] = int.Parse(tokens[c]);
                }
            }

            // 2. Les 36 lignes suivantes = Le visuel (VisualGrid)
            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                string[] tokens = lines[GameConfig.MapHeight + r].Split(',');
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    VisualGrid[r, c] = int.Parse(tokens[c]);
                }
            }
        }

        /// <summary>
        /// Exporte la carte complète (Physique + Visuel) dans un fichier texte (72 lignes au total)
        /// </summary>
        public void SaveMapToFile(string filePath)
        {
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                // 1. Écriture de la grille physique (36 lignes)
                for (int r = 0; r < GameConfig.MapHeight; r++)
                {
                    string line = "";
                    for (int c = 0; c < GameConfig.MapWidth; c++)
                    {
                        line += Grid[r, c] + (c == GameConfig.MapWidth - 1 ? "" : ",");
                    }
                    sw.WriteLine(line);
                }

                // 2. Écriture de la grille visuelle (36 lignes)
                for (int r = 0; r < GameConfig.MapHeight; r++)
                {
                    string line = "";
                    for (int c = 0; c < GameConfig.MapWidth; c++)
                    {
                        line += VisualGrid[r, c] + (c == GameConfig.MapWidth - 1 ? "" : ",");
                    }
                    sw.WriteLine(line);
                }
            }
        }
    }
}