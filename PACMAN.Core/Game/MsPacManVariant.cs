namespace PacmanWinForms
{
    /// <summary>
    /// Ms Pac-Man. Étape 3 : labyrinthes et sprites de Ms Pac-Man ; les règles (fantômes, fruit, niveaux),
    /// les sons, l'attract mode et les intermissions sont encore ceux de Pac-Man (étapes 4 à 6).
    /// </summary>
    public sealed class MsPacManVariant : GameVariant
    {
        public const string Folder = "asset.mspacman.";

        // Les six labyrinthes : tuiles de la gomme par jeu de tuiles (la super-gomme est la suivante, la porte du nid est
        // dans le visuel). Les labyrinthes 5 et 6 ont la même disposition que 3 et 4, avec d'autres couleurs.
        private static readonly MazeDefinition[] mazes =
        {
            new MazeDefinition("Ms Pac-Man 1", Folder + "maps.maze1.txt", 0, 30),
            new MazeDefinition("Ms Pac-Man 2", Folder + "maps.maze2.txt", 1, 32),
            new MazeDefinition("Ms Pac-Man 3", Folder + "maps.maze3.txt", 2, 34),
            new MazeDefinition("Ms Pac-Man 4", Folder + "maps.maze4.txt", 3, 28),
            new MazeDefinition("Ms Pac-Man 5", Folder + "maps.maze5.txt", 4, 34),
            new MazeDefinition("Ms Pac-Man 6", Folder + "maps.maze6.txt", 5, 28)
        };

        public override string Id { get { return "mspacman"; } }
        public override string DisplayName { get { return "MS PAC-MAN"; } }
        public override string AssetFolder { get { return Folder; } }


        /// <summary>
        /// Labyrinthe de chaque niveau : 1-2 → 1, 3-5 → 2, 6-9 → 3, 10-13 → 4, puis par blocs de 4 niveaux
        /// les variantes de couleur de 3 (labyrinthe 5) et de 4 (labyrinthe 6), en alternance.
        /// </summary>
        public static int MazeIndexForLevel(int level)
        {
            if (level <= 2) return 0;
            if (level <= 5) return 1;
            if (level <= 9) return 2;
            if (level <= 13) return 3;
            return (((level - 14) / 4) % 2 == 0) ? 4 : 5;
        }

        public override MazeDefinition GetMaze(int level)
        {
            return mazes[MazeIndexForLevel(level)];
        }

        // Même départ dans tous les labyrinthes
        public override int PacmanStartX { get { return MazeLayout.PacmanStartX; } }
        public override int PacmanStartY { get { return MazeLayout.PacmanStartY; } }

        // Étape 4 : fantômes errants, fruit mobile ; vitesses, vagues et sorties du nid comme dans Pac-Man
        public override Ghost[] CreateGhosts()
        {
            // Pas de bug de débordement vers le haut ; Sue (orange) se comporte comme Clyde ;
            // en Scatter, les quatre fantômes errent au hasard (sauf Blinky en Cruise Elroy)
            Blinky blinky = new Blinky();
            Pinky pinky = new Pinky(false);
            Inky inky = new Inky(blinky, false);
            Clyde sue = new Clyde();

            Ghost[] ghosts = { blinky, pinky, inky, sue };
            foreach (Ghost ghost in ghosts) ghost.WandersInScatter = true;
            return ghosts;
        }

        public override LevelSpecs GetSpecs(int level)
        {
            return GameConfig.GetSpecsForLevel(level);
        }

        public override FruitManager CreateFruit(int? seed)
        {
            return new MsFruitManager(seed);
        }

        // Étape 6 : intermissions de Ms Pac-Man
        public override int IntermissionAfterLevel(int level)
        {
            return 0;
        }

        public override IIntermission CreateIntermission(int number, int level, SoundDirector sound)
        {
            return null;
        }

        public override SpriteManager CreateSprites()
        {
            return new MsPacManSprites(SheetResource);
        }

        // Étape 6 : attract mode et écran start de Ms Pac-Man (en attendant, ceux de Pac-Man avec les sprites de Ms Pac-Man)
        public override IAttractScreen CreateAttractScreen(ScreenManager manager)
        {
            return new AttractScreen(manager);
        }

        public override IStartScreen CreateStartScreen(ScreenManager manager)
        {
            return new StartScreen(manager);
        }
    }
}
