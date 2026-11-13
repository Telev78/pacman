using System;
using System.Globalization;
using System.IO;

namespace PacmanWinForms
{
    public enum GameVersion { Jap, Usa }

    /// <summary>
    /// Réglages utilisateur, lus dans PACMAN.ini (à côté de l'exécutable).
    /// Valeurs invalides ou fichier absent : valeurs par défaut. Le high score n'est jamais enregistré.
    /// </summary>
    public static class Settings
    {
        public const int MinZoom = 1;
        public const int MaxZoom = 8;

        public static int Zoom { get; private set; } = 3;              // Taille de la fenêtre : 224x288 x Zoom
        public static bool StartFullscreen { get; private set; } = false;
        public static int Volume { get; private set; } = 100;          // 0 à 100 %

        private static string FilePath
        {
            get { return Path.Combine(AppContext.BaseDirectory, "PACMAN.ini"); }
        }

        public static GameVersion Version { get; private set; } = GameVersion.Jap;

        public static int Lives { get; private set; } = 3;            // Vies au départ : 1 à 5
        public static int BonusLife { get; private set; } = 10000;    // Seuil de la vie bonus : 10000, 15000, 20000 ; 0 = aucune

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    Save();
                    return;
                }

                foreach (string raw in File.ReadAllLines(FilePath))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;

                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();

                    if (key.Equals("Zoom", StringComparison.OrdinalIgnoreCase))
                    {
                        int zoom;
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out zoom))
                            Zoom = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));
                    }
                    else if (key.Equals("Fullscreen", StringComparison.OrdinalIgnoreCase))
                    {
                        bool full;
                        if (bool.TryParse(value, out full))
                            StartFullscreen = full;
                    }
                    else if (key.Equals("Volume", StringComparison.OrdinalIgnoreCase))
                    {
                        int volume;
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out volume))
                            Volume = Math.Max(0, Math.Min(100, volume));
                    }
                    else if (key.Equals("Version", StringComparison.OrdinalIgnoreCase))
                    {
                        if (value.Equals("USA", StringComparison.OrdinalIgnoreCase)) Version = GameVersion.Usa;
                        else if (value.Equals("JAP", StringComparison.OrdinalIgnoreCase)) Version = GameVersion.Jap;
                    }
                    else if (key.Equals("Lives", StringComparison.OrdinalIgnoreCase))
                    {
                        int lives;
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out lives))
                            Lives = Math.Max(1, Math.Min(5, lives));
                    }
                    else if (key.Equals("BonusLife", StringComparison.OrdinalIgnoreCase))
                    {
                        if (value.Equals("NONE", StringComparison.OrdinalIgnoreCase)) BonusLife = 0;
                        else if (value == "10000") BonusLife = 10000;
                        else if (value == "15000") BonusLife = 15000;
                        else if (value == "20000") BonusLife = 20000;
                    }
                }
            }
            catch { /* Fichier illisible ou dossier en lecture seule : on garde les valeurs par défaut */ }
        }

        public static void SetVolume(int volume)
        {
            Volume = Math.Max(0, Math.Min(100, volume));
            Save();
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(FilePath,
                    "; Réglages de Pac-Man\r\n" +
                    "; Zoom : taille de la fenêtre, de 1 à 8 (1 = 224x288, 3 = 672x864)\r\n" +
                    "Zoom=" + Zoom.ToString(CultureInfo.InvariantCulture) + "\r\n" +
                    "; Fullscreen : true pour démarrer en plein écran (Alt+Entrée bascule en jeu)\r\n" +
                    "Fullscreen=" + (StartFullscreen ? "true" : "false") + "\r\n" +
                    "; Volume : de 0 à 100 (touches F11 / F12 en jeu)\r\n" +
                    "Volume=" + Volume.ToString(CultureInfo.InvariantCulture) + "\r\n" +
                    "; Version : JAP (Namco, noms japonais) ou USA (Midway, noms américains)\r\n" +
                    "Version=" + (Version == GameVersion.Usa ? "USA" : "JAP") + "\r\n" +
                    "; Lives : nombre de vies au départ, de 1 à 5\r\n" +
                    "Lives=" + Lives.ToString(CultureInfo.InvariantCulture) + "\r\n" +
                    "; BonusLife : score donnant une vie bonus : 10000, 15000, 20000 ou NONE\r\n" +
                    "BonusLife=" + (BonusLife == 0 ? "NONE" : BonusLife.ToString(CultureInfo.InvariantCulture)) + "\r\n");
            }
            catch { /* Dossier en lecture seule : le réglage vaut pour cette session seulement */ }
        }
    }
}