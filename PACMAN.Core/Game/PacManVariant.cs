namespace PacmanWinForms
{
    /// <summary>Le jeu d'origine : un seul labyrinthe, quatre fantômes classiques.</summary>
    public class PacManVariant : GameVariant
    {
        public override string Id { get { return "pacman"; } }
        public override string DisplayName { get { return "PAC-MAN"; } }

        public override MazeDefinition GetMaze(int level)
        {
            return MazeDefinition.Original;
        }

        public override int PacmanStartX { get { return MazeLayout.PacmanStartX; } }
        public override int PacmanStartY { get { return MazeLayout.PacmanStartY; } }

        public const string Folder = "asset.pacman.";

        public override string AssetFolder { get { return Folder; } }

        public override LevelSpecs GetSpecs(int level)
        {
            return GameConfig.GetSpecsForLevel(level);
        }

        // Intermissions de l'arcade : après les niveaux 2, 5, 9, 13 et 17
        public override int IntermissionAfterLevel(int level)
        {
            switch (level)
            {
                case 2: return 1;
                case 5: return 2;
                case 9:
                case 13:
                case 17: return 3;
                default: return 0;
            }
        }

        public override IIntermission CreateIntermission(int number, int level, SoundDirector sound)
        {
            switch (number)
            {
                case 1: return new Intermission1Scene(sound, level);
                case 2: return new Intermission2Scene(sound, level);
                case 3: return new Intermission3Scene(sound, level);
                default: return null;
            }
        }

        public override FruitManager CreateFruit(int? seed)
        {
            return new FruitManager(seed);
        }

        public override SpriteManager CreateSprites()
        {
            return new SpriteManager(SheetResource);
        }

        public override IAttractScreen CreateAttractScreen(ScreenManager manager)
        {
            return new AttractScreen(manager);
        }

        public override IStartScreen CreateStartScreen(ScreenManager manager)
        {
            return new StartScreen(manager);
        }

        public override Ghost[] CreateGhosts()
        {
            Blinky blinky = new Blinky();
            Pinky pinky = new Pinky();
            Inky inky = new Inky(blinky);
            Clyde clyde = new Clyde();
            return new Ghost[] { blinky, pinky, inky, clyde };
        }
    }
}
