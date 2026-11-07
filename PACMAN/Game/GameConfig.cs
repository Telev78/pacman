using System;

namespace PacmanWinForms
{
    public static class GameConfig
    {
        public const int TileSize = 8;                  // Taille d'une case d'origine (8x8 pixels)
        public const int MapWidth = 28;                 // 28 colonnes
        public const int MapHeight = 36;                // 36 lignes
        public const int BonusLifeThreshold = 10000;    // Vie bonus décernée à 10 000 points
        public const int FramesPerSecond = 60;

        // Les niveaux 21 et plus partagent les mêmes paramètres
        private const int MaxSpecsLevel = 21;
        private static readonly LevelSpecs[] SpecsByLevel = BuildAllSpecs();

        /// <summary>
        /// Paramètres d'un niveau. L'objet renvoyé est partagé : ne pas le modifier.
        /// </summary>
        public static LevelSpecs GetSpecsForLevel(int level)
        {
            return SpecsByLevel[Math.Max(1, Math.Min(level, MaxSpecsLevel))];
        }

        private static LevelSpecs[] BuildAllSpecs()
        {
            LevelSpecs[] all = new LevelSpecs[MaxSpecsLevel + 1];
            for (int level = 1; level <= MaxSpecsLevel; level++)
            {
                all[level] = BuildSpecs(level);
            }
            return all;
        }

        private static LevelSpecs BuildSpecs(int level)
        {
            LevelSpecs specs = new LevelSpecs();

            // Le modificateur magique : Convertit le référentiel de l'arcade (100% = 1.25 px/frame)
            // pour que l'exécution à 60 FPS constants sur PC donne l'exacte vitesse d'origine.
            const float BaseMultiplier = 1.25f;

            // --- PARTIE 1 : LES VITESSES PHYSIQUES (Groupées par paliers de niveaux) ---
            if (level == 1)
            {
                specs.PacmanNormal = 0.80f * BaseMultiplier; // Donne pile 1.00 pixel/frame
                specs.PacmanDots = 0.71f * BaseMultiplier;
                specs.GhostNormal = 0.75f * BaseMultiplier;
                specs.GhostTunnel = 0.40f * BaseMultiplier;
                specs.FrightenedPacman = 0.90f * BaseMultiplier;
                specs.FrightenedPacmanDots = 0.79f * BaseMultiplier;
                specs.FrightenedGhost = 0.50f * BaseMultiplier;
            }
            else if (level >= 2 && level <= 4)
            {
                specs.PacmanNormal = 0.90f * BaseMultiplier;
                specs.PacmanDots = 0.79f * BaseMultiplier;
                specs.GhostNormal = 0.85f * BaseMultiplier;
                specs.GhostTunnel = 0.45f * BaseMultiplier;
                specs.FrightenedPacman = 0.95f * BaseMultiplier;
                specs.FrightenedPacmanDots = 0.83f * BaseMultiplier;
                specs.FrightenedGhost = 0.55f * BaseMultiplier;
            }
            else if (level >= 5 && level <= 20)
            {
                specs.PacmanNormal = 1.00f * BaseMultiplier; // Vitesse maximale théorique (1.25 px/frame)
                specs.PacmanDots = 0.87f * BaseMultiplier;
                specs.GhostNormal = 0.95f * BaseMultiplier;
                specs.GhostTunnel = 0.50f * BaseMultiplier;
                specs.FrightenedPacman = 1.00f * BaseMultiplier;
                specs.FrightenedPacmanDots = 0.87f * BaseMultiplier;
                specs.FrightenedGhost = 0.60f * BaseMultiplier;
            }
            else // Niveau 21+ (Le "Kill Screen" et les niveaux infinis)
            {
                // Pac-Man fatigue légèrement et redescend à 90% (comme aux niveaux 2-4)
                specs.PacmanNormal = 0.90f * BaseMultiplier;
                specs.PacmanDots = 0.79f * BaseMultiplier;
                // Les fantômes restent à leur agressivité maximale
                specs.GhostNormal = 0.95f * BaseMultiplier;
                specs.GhostTunnel = 0.50f * BaseMultiplier;
                // Valeurs par défaut pour la sécurité (Le mode Frightened n'est plus actif à ce stade)
                specs.FrightenedPacman = 0.90f * BaseMultiplier;
                specs.FrightenedPacmanDots = 0.79f * BaseMultiplier;
                specs.FrightenedGhost = 0.60f * BaseMultiplier;
            }

            // --- PARTIE 2 : CONFIGURATION DU COMPORTEMENT DES SUPER-GOMMES ---
            // Les durées exactes en secondes et le nombre de flashs définis par la ROM originale.
            switch (level)
            {
                case 1: specs.FrightenedTimeSec = 6; specs.FlashesCount = 5; break;
                case 2: specs.FrightenedTimeSec = 5; specs.FlashesCount = 5; break;
                case 3: specs.FrightenedTimeSec = 4; specs.FlashesCount = 5; break;
                case 4: specs.FrightenedTimeSec = 3; specs.FlashesCount = 5; break;
                case 5: specs.FrightenedTimeSec = 2; specs.FlashesCount = 5; break;
                case 6: specs.FrightenedTimeSec = 5; specs.FlashesCount = 5; break;
                case 7: specs.FrightenedTimeSec = 2; specs.FlashesCount = 5; break;
                case 8: specs.FrightenedTimeSec = 2; specs.FlashesCount = 5; break;
                case 9: specs.FrightenedTimeSec = 1; specs.FlashesCount = 3; break;
                case 10: specs.FrightenedTimeSec = 5; specs.FlashesCount = 5; break;
                case 11: specs.FrightenedTimeSec = 2; specs.FlashesCount = 5; break;
                case 12: specs.FrightenedTimeSec = 1; specs.FlashesCount = 3; break;
                case 13: specs.FrightenedTimeSec = 1; specs.FlashesCount = 3; break;
                case 14: specs.FrightenedTimeSec = 3; specs.FlashesCount = 5; break;
                case 15: specs.FrightenedTimeSec = 1; specs.FlashesCount = 3; break;
                case 16: specs.FrightenedTimeSec = 1; specs.FlashesCount = 3; break;
                case 18: specs.FrightenedTimeSec = 1; specs.FlashesCount = 3; break;

                // Niveaux 17, 19, 20 et 21+ : Les fantômes ne deviennent PAS vulnérables.
                // Ils changent simplement instantanément de direction (demi-tour réglementaire).
                default: specs.FrightenedTimeSec = 0; specs.FlashesCount = 0; break;
            }

            // --- PARTIE 3 : CRUISE ELROY ---
            if (level == 1) { specs.Elroy1Speed = 0.80f * BaseMultiplier; specs.Elroy2Speed = 0.85f * BaseMultiplier; }
            else if (level <= 4) { specs.Elroy1Speed = 0.90f * BaseMultiplier; specs.Elroy2Speed = 0.95f * BaseMultiplier; }
            else { specs.Elroy1Speed = 1.00f * BaseMultiplier; specs.Elroy2Speed = 1.05f * BaseMultiplier; }

            int elroyDots;
            if (level == 1) elroyDots = 20;
            else if (level == 2) elroyDots = 30;
            else if (level <= 5) elroyDots = 40;
            else if (level <= 8) elroyDots = 50;
            else if (level <= 11) elroyDots = 60;
            else if (level <= 14) elroyDots = 80;
            else if (level <= 18) elroyDots = 100;
            else elroyDots = 120;

            specs.Elroy1Dots = elroyDots;
            specs.Elroy2Dots = elroyDots / 2;

            return specs;
        }

        // Durées des vagues en secondes. Index pairs = Scatter, index impairs = Chase.
        // 0 = une seule frame (Scatter 4 des niveaux 2+ ne dure qu'une frame à l'arcade).
        // La vague 7 (Chase 4) est infinie et n'apparaît pas dans les tables.
        private static readonly int[] WaveSecondsLevel1 = { 7, 20, 7, 20, 5, 20, 5 };
        private static readonly int[] WaveSecondsLevel2To4 = { 7, 20, 7, 20, 5, 1033, 0 };
        private static readonly int[] WaveSecondsLevel5Plus = { 5, 20, 5, 20, 5, 1037, 0 };

        /// <summary>
        /// Renvoie la durée d'une vague (0 à 7) pour un niveau donné, en frames à 60 Hz.
        /// </summary>
        public static int GetWaveDurationFrames(int level, int waveIndex)
        {
            // Vague 7 (Chase 4) : infinie
            if (waveIndex >= 7) return int.MaxValue;

            int[] seconds;
            if (level == 1) seconds = WaveSecondsLevel1;
            else if (level <= 4) seconds = WaveSecondsLevel2To4;
            else seconds = WaveSecondsLevel5Plus;

            int s = seconds[waveIndex];
            return (s == 0) ? 1 : s * FramesPerSecond;
        }
    }
}