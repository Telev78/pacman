using System.IO;

namespace PacmanWinForms
{
    /// <summary>
    /// Accès unique aux ressources embarquées dans PACMAN.Core (image, sons, labyrinthe, démo).
    /// Les chemins sont relatifs au dossier du projet, avec des points à la place des séparateurs :
    /// "asset.sounds.start.wav", "asset.asset.png", "asset.maps.original_maze.txt", "asset.demo.txt".
    /// </summary>
    public static class Assets
    {
        private const string Prefix = "PacmanWinForms.";

        /// <summary>Ouvre une ressource, ou renvoie null si elle n'existe pas.</summary>
        public static Stream TryOpen(string path)
        {
            return typeof(Assets).Assembly.GetManifestResourceStream(Prefix + path);
        }

        /// <summary>Ouvre une ressource, ou lève FileNotFoundException si elle n'existe pas.</summary>
        public static Stream Open(string path)
        {
            Stream stream = TryOpen(path);
            if (stream == null)
            {
                throw new FileNotFoundException("Ressource embarquée introuvable : " + Prefix + path);
            }
            return stream;
        }
    }
}
