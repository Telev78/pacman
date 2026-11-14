namespace PacmanWinForms
{
    /// <summary>
    /// Tout ce qui change d'un jeu à l'autre (Pac-Man, Ms Pac-Man) : labyrinthes, position de départ,
    /// fantômes. Le moteur (Game, Pacman, Ghost, ScreenManager...) ne connaît que cette classe.
    /// Les sprites, sons, textes, fruits et intermissions y seront ajoutés dans les étapes suivantes.
    /// </summary>
    public abstract class GameVariant
    {
        /// <summary>Identifiant stable, utilisé dans PACMAN.ini et en ligne de commande ("pacman", "mspacman").</summary>
        public abstract string Id { get; }

        /// <summary>Nom affiché dans le menu.</summary>
        public abstract string DisplayName { get; }

        /// <summary>Labyrinthe à jouer à ce niveau (1, 2, 3...).</summary>
        public abstract MazeDefinition GetMaze(int level);

        /// <summary>Position de départ de Pac-Man, en pixels (identique pour tous les labyrinthes d'un même jeu).</summary>
        public abstract int PacmanStartX { get; }
        public abstract int PacmanStartY { get; }

        /// <summary>Crée les quatre fantômes d'une vie, dans l'ordre Blinky, Pinky, Inky, Clyde (ou Sue).</summary>
        public abstract Ghost[] CreateGhosts();

        /// <summary>Dossier des ressources embarquées de ce jeu, ex. "asset.pacman." (points, se termine par un point).</summary>
        public abstract string AssetFolder { get; }

        public string SheetResource { get { return AssetFolder + "asset.png"; } }
        public virtual string SoundFolder { get { return AssetFolder + "sounds."; } }
        public virtual string DemoResource { get { return AssetFolder + "demo.txt"; } }

        /// <summary>Paramètres de vitesse, durées et fantômes d'un niveau. L'objet renvoyé est partagé : ne pas le modifier.</summary>
        public abstract LevelSpecs GetSpecs(int level);

        /// <summary>Numéro (1 à 3) de l'intermission jouée après ce niveau, 0 s'il n'y en a pas.</summary>
        public abstract int IntermissionAfterLevel(int level);

        /// <summary>Crée la scène d'une intermission, ou null si elle n'existe pas.</summary>
        public abstract IIntermission CreateIntermission(int number, int level, SoundDirector sound);

        /// <summary>Crée le gestionnaire de fruit bonus (seed fixe pour la démo).</summary>
        public abstract FruitManager CreateFruit(int? seed);

        /// <summary>Découpe de la feuille de sprites de ce jeu (images, police, labyrinthes).</summary>
        public abstract SpriteManager CreateSprites();

        /// <summary>Écran de présentation du jeu (attract mode).</summary>
        public abstract IAttractScreen CreateAttractScreen(ScreenManager manager);

        /// <summary>Écran "PUSH START BUTTON".</summary>
        public abstract IStartScreen CreateStartScreen(ScreenManager manager);
    }
}
