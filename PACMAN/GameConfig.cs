namespace PacmanWinForms
{
    public class LevelSpecs
    {
        // Vitesses de Pac-Man
        public float PacmanNormal { get; set; }
        public float PacmanDots { get; set; }          // En mangeant des gommes

        // Vitesses des fantômes
        public float GhostNormal { get; set; }
        public float GhostTunnel { get; set; }          // Dans les tunnels latéraux

        // Vitesses en mode Frightened (Super-Gomme)
        public float FrightenedPacman { get; set; }
        public float FrightenedPacmanDots { get; set; }
        public float FrightenedGhost{ get; set; }

        // Paramètres de configuration du mode Frightened
        public int FrightenedTimeSec { get; set; }     // Durée de la vulnérabilité en secondes
        public int FlashesCount { get; set; }          // Nombre de clignotements bleus/blancs avant la fin
    }

    public static class GameConfig
    {
        public const int TileSize = 8;                  // Taille d'une case d'origine (8x8 pixels)
        public const int MapWidth = 28;                 // 28 colonnes
        public const int MapHeight = 36;                // 36 lignes
        public const int BonusLifeThreshold = 10000;    // Vie bonus décernée à 10 000 points
        public const int Zoom = 3;                      // Multiplicateur pour écran moderne

        // Tailles calculées pour l'affichage de la fenêtre
        public const int ScaledTileSize = TileSize * Zoom; // 24 pixels
        public const int ScreenWidth = MapWidth * ScaledTileSize;  // 672 pixels
        public const int ScreenHeight = MapHeight * ScaledTileSize; // 864 pixels

        public static LevelSpecs GetSpecsForLevel(int level)
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

            return specs;
        }

        /// <summary>
        /// Renvoie la durée d'une vague spécifique (0 à 7) pour un niveau donné, en millisecondes.
        /// Index pairs (0, 2, 4, 6) = Scatter | Index impairs (1, 3, 5, 7) = Chase
        /// </summary>
        public static double GetWaveDurationMs(int level, int waveIndex)
        {
            // Vague 7 (Chase 4) : Infinie
            if (waveIndex >= 7) return double.PositiveInfinity;

            if (level == 1)
            {
                switch (waveIndex)
                {
                    case 0: return 7000;  // Scatter 1
                    case 1: return 20000; // Chase 1
                    case 2: return 7000;  // Scatter 2
                    case 3: return 20000; // Chase 2
                    case 4: return 5000;  // Scatter 3
                    case 5: return 20000; // Chase 3
                    case 6: return 5000;  // Scatter 4
                }
            }
            else if (level >= 2 && level <= 4)
            {
                switch (waveIndex)
                {
                    case 0: return 7000;
                    case 1: return 20000;
                    case 2: return 7000;
                    case 3: return 20000;
                    case 4: return 5000;
                    case 5: return 1033000; // Chase 3 très long (~17 min)
                    case 6: return 1;       // Scatter 4 dure 1 seule frame à l'arcade !
                }
            }
            else // Niveau 5+
            {
                switch (waveIndex)
                {
                    case 0: return 5000;
                    case 1: return 20000;
                    case 2: return 5000;
                    case 3: return 20000;
                    case 4: return 5000;
                    case 5: return 1037000;
                    case 6: return 1;
                }
            }

            return 0;
        }
    }


}
