using System.Drawing;
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
            new MazeDefinition("Ms Pac-Man 1", Folder + "maps.maze1.txt", 0, 30,
                new Point(9, 14), new Point(18, 14), new Point(9, 26), new Point(18, 26)),
            new MazeDefinition("Ms Pac-Man 2", Folder + "maps.maze2.txt", 1, 32),
            new MazeDefinition("Ms Pac-Man 3", Folder + "maps.maze3.txt", 2, 34,
                new Point(9, 14), new Point(18, 14), new Point(9, 26), new Point(18, 26)),
            new MazeDefinition("Ms Pac-Man 4", Folder + "maps.maze4.txt", 3, 28,
                new Point(12, 14), new Point(15, 14), new Point(12, 23), new Point(15, 23)),
            new MazeDefinition("Ms Pac-Man 5", Folder + "maps.maze5.txt", 4, 34,
                new Point(9, 14), new Point(18, 14), new Point(9, 26), new Point(18, 26)),
            new MazeDefinition("Ms Pac-Man 6", Folder + "maps.maze6.txt", 5, 28,
                new Point(12, 14), new Point(15, 14), new Point(12, 23), new Point(15, 23))
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
            // en Scatter, Blinky et Pinky errent au hasard (sauf Blinky en Cruise Elroy)
            Blinky blinky = new Blinky();
            Pinky pinky = new Pinky(false);
            Inky inky = new Inky(blinky, false);
            Clyde sue = new Clyde();

            Ghost[] ghosts = { blinky, pinky, inky, sue };

            // Pendant l'unique phase Scatter, seuls Blinky et Pinky errent au hasard ;
            // Inky et Sue gardent leur coin (ils sortent souvent trop tard du nid pour y aller)
            blinky.WandersInScatter = true;
            pinky.WandersInScatter = true;

            // À la sortie du nid, chaque fantôme choisit sa direction d'après sa cible
            foreach (Ghost g in ghosts) g.ExitsTowardTarget = true;
            return ghosts;
        }

        // Une seule phase Scatter de 7 s au début du niveau, puis Chase jusqu'à la fin du niveau
        private const int ScatterSeconds = 7;

        public override int GetWaveDurationFrames(int level, int waveIndex)
        {
            return waveIndex == 0 ? ScatterSeconds * GameConfig.FramesPerSecond : int.MaxValue;
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
            // Acte 1 après le niveau 2, acte 2 après le 5, acte 3 après les niveaux 9, 13, 17...
            if (level == 2) return 1;
            if (level == 5) return 2;
            if (level == 9 || level == 13 || level == 17) return 3;
            return 0;
        }

        public override IIntermission CreateIntermission(int number, int level, SoundDirector sound)
        {
            return number >= 1 && number <= 3 ? new MsIntermissionScene(number, level, sound) : null;
        }

        // Mort de Ms Pac-Man, mesurée sur l'arcade : après le gel, elle reste 30 frames bouche vers le bas, puis fait
        // 10 quarts de tour (900°) en 7,5 frames chacun, reste bouche vers le haut ~62 frames et disparaît.
        // Le son de mort (1,14 s) démarre avec la rotation.
        private const int DeathFreezeAndStart = 61;   // Gel de 60 frames + première frame de l'animation
        private const int DeathHoldFrames = 30;
        private const int DeathQuarterTurns = 10;

        public override int DeathAnimationFrames { get { return 159; } }
        public override int DeathEndPauseFrames { get { return 7; } }
        public override int DeathSoundDelayFrames { get { return DeathFreezeAndStart + DeathHoldFrames - 1; } }

        public override int DeathFrameAt(int t)
        {
            if (t >= DeathAnimationFrames) return 11;   // Disparue
            if (t < DeathHoldFrames) return 0;
            int turns = (2 * (t - DeathHoldFrames + 1) + 14) / 15;   // Un quart de tour toutes les 7,5 frames
            return System.Math.Min(turns, DeathQuarterTurns);
        }

        public override SpriteManager CreateSprites()
        {
            return new MsPacManSprites(SheetResource);
        }

        // Étape 6 : attract mode et écran start de Ms Pac-Man
        public override IAttractScreen CreateAttractScreen(ScreenManager manager)
        {
            return new MsAttractScreen(manager);
        }

        public override IStartScreen CreateStartScreen(ScreenManager manager)
        {
            return new MsStartScreen(manager);
        }
    }
}
