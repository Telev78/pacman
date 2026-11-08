using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PacmanWinForms
{
    public class Game
    {
        // Propriétés accessibles en lecture seule par le MainForm pour le rendu
        public LevelMap Map { get; private set; }
        public Pacman Pacman { get; private set; }
        private readonly List<Ghost> ghostList = new List<Ghost>();
        public IReadOnlyList<Ghost> Ghosts { get { return ghostList; } }

        public int DotsRemaining { get; private set; }
        public int Lives { get; private set; }
        public int CurrentLevel { get; private set; }

        public bool IsFrightenedMode { get; private set; }
        public int FrightenedFramesRemaining { get; private set; }
        public LevelSpecs CurrentSpecs { get; private set; }

        public int GhostAnimationFrame { get; private set; }
        public GameState State { get; private set; }
        public bool ShowCharacters { get; private set; } = true;
        public bool IsNextLevelTransition { get; private set; } = false;
        public int EatenGhostScoreX { get; private set; }
        public int EatenGhostScoreY { get; private set; }
        public int EatenGhostScoreValue { get; private set; }
        public Ghost JustEatenGhost { get; private set; } // Permet de savoir quel fantôme masquer

        // Variables privées de logique
        private int frameCounter = 0;
        private int readyTimer = 0;
        private int gameOverTimer = 0;
        private int ghostsEatenInCurrentFrightenedMode = 0;
        private int ghostEatenFreezeTimer = 0;

        private GhostHouse ghostHouse;
        private WaveScheduler waves = new WaveScheduler();
        private bool elroySuspended = false;

        public FruitManager Fruit { get; private set; }

        private SoundDirector sound;

        private readonly ScoreKeeper scoreKeeper;
        public int Score { get { return scoreKeeper.Score; } }
        public int HighScore { get { return scoreKeeper.HighScore; } }

        // Séquences de mort et de fin de niveau (en frames à 60 Hz)
        private const int DeathFreezeFrames = 60;       // 1 s de gel avant l'animation
        private const int DeathFramesPerSprite = 12;    // 200 ms par sprite
        private const int DeathSpriteCount = 12;
        private const int DeathEndPauseFrames = 30;     // 0,5 s après l'animation
        private const int VictoryFreezeFrames = 60;     // 1 s de gel avant le clignotement
        private const int VictoryFlashFrames = 14;      // Durée d'une couleur (blanc ou bleu)
        private const int VictoryFlashCount = 4;        // Nombre de clignotements
        private int dyingTimer = 0;
        private int victoryTimer = 0;

        public bool ShowGhosts { get; private set; } = true;
        public bool IsMazeFlashWhite { get; private set; } = false;
        public int FrameCounter { get { return frameCounter; } }

        // La partie est complètement terminée (le GAME OVER a fini de s'afficher)
        public bool IsFinished { get; private set; } = false;

        // Affiche "PLAYER ONE" au-dessus du nid au tout début d'une partie
        public bool ShowPlayerLabel { get; private set; } = false;

        private const int DemoSeed = 1980;
        private const int DemoEndPauseFrames = 60;   // 1 s après l'animation de mort
        public bool IsDemo { get; private set; }

        /// <summary>
        /// Le mode de comportement global imposé par le timer des vagues (Scatter ou Chase).
        /// </summary>
        public GhostMode GlobalGhostMode { get { return waves.CurrentMode; } }

        public Game(SoundDirector sound, int highScore, bool isDemo = false)
        {
            this.sound = sound;
            IsDemo = isDemo;
            scoreKeeper = new ScoreKeeper(highScore);

            Map = new LevelMap();
            Fruit = new FruitManager(isDemo ? DemoSeed : (int?)null);
            Pacman = new Pacman();
            ghostHouse = new GhostHouse(ghostList);

            Lives = 3;
            CurrentLevel = 1;

            InitializeGameValue();
        }

        private void InitializeGameValue()
        {
            sound.StopAll();

            Assembly assembly = Assembly.GetExecutingAssembly();
            // Note la conversion des dossiers en points : asset.maps.original_maze.txt
            string resourceName = "PacmanWinForms.asset.maps.original_maze.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    // On charge la carte directement depuis le flux de l'exécutable !
                    Map.LoadMapFromStream(stream);

                    CountInitialPellets();
                    ResetPositions();
                    ShowPlayerLabel = !IsNextLevelTransition && !IsDemo;
                }
                else
                {
                    throw new FileNotFoundException("Fichier de carte embarqué introuvable : " + resourceName);
                }
            }
            if (!IsNextLevelTransition)
                sound.PlayStartJingle();
        }

        private void CountInitialPellets()
        {
            DotsRemaining = 0;
            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    TileType tile = Map.Grid[r, c];
                    if (tile == TileType.Dot || tile == TileType.Energizer)
                    {
                        DotsRemaining++;
                    }
                }
            }

            Fruit.StartLevel(DotsRemaining);
        }

        private void ResetPositions(bool afterDeath = false)
        {
            Ghost.ResetRandom();
            sound.ResetBackground();

            this.CurrentSpecs = GameConfig.GetSpecsForLevel(this.CurrentLevel);

            ghostList.Clear();

            Blinky blinky = new Blinky();
            Pinky pinky = new Pinky();
            Inky inky = new Inky(blinky);
            Clyde clyde = new Clyde();

            ghostList.Add(blinky);
            ghostList.Add(pinky);
            ghostList.Add(inky);
            ghostList.Add(clyde);

            SpawnPacman();

            Fruit.ResetForNewLife();

            ghostHouse.Reset(CurrentLevel, afterDeath);

            elroySuspended = afterDeath;

            waves.Reset();

            State = GameState.Ready;
            readyTimer = 240;
            ShowCharacters = false;
            ShowPlayerLabel = false;

            ShowGhosts = true;
            IsMazeFlashWhite = false;

            // Le mode effrayé ne doit pas survivre à une mort ni à un changement de niveau
            IsFrightenedMode = false;
            FrightenedFramesRemaining = 0;
            ghostsEatenInCurrentFrightenedMode = 0;
        }

        private void SpawnPacman()
        {
            Pacman.X = MazeLayout.PacmanStartX;
            Pacman.Y = MazeLayout.PacmanStartY;
            Pacman.CurrentDirection = Direction.Left;
            Pacman.NextDirection = Direction.None;
            Pacman.State = PacmanState.Alive;
            Pacman.Stall(0);
        }

        public void HandleInput(Direction direction)
        {
            if (direction != Direction.None)
            {
                Pacman.NextDirection = direction;
            }
        }

        // Logique fixe à 60 Hz
        public void Update()
        {
            // GESTION DU READY
            if (State == GameState.Ready)
            {
                readyTimer--;
                if (readyTimer <= 120)
                {
                    ShowCharacters = true;
                    ShowPlayerLabel = false;
                }
                if (readyTimer <= 0)
                {
                    State = GameState.Playing;
                    IsNextLevelTransition = false;
                    ghostHouse.ReleaseByCounter();
                }
                return;
            }

            // GESTION DU GEL FANTÔME MANGÉ (Spécification Arcade : 60 frames = 1 seconde)
            if (State == GameState.GhostEaten)
            {
                ghostEatenFreezeTimer--;
                if (ghostEatenFreezeTimer <= 0)
                {
                    State = GameState.Playing; // Reprise du jeu normale
                }
                return; // Fige toute la physique et le mouvement du labyrinthe
            }

            // GESTION DU GAME OVER (Compte à rebours à 60 Hz)
            if (State == GameState.GameOver)
            {
                gameOverTimer--;
                if (gameOverTimer <= 0)
                {
                    IsFinished = true;
                }
                return;
            }

            // SÉQUENCE DE MORT
            if (State == GameState.Dying)
            {
                UpdateDying();
                return;
            }

            // SÉQUENCE DE FIN DE NIVEAU
            if (State == GameState.Victory)
            {
                UpdateVictory();
                return;
            }

            if (State != GameState.Playing) return;

            if (waves.Update(CurrentLevel, IsFrightenedMode))
            {
                OnWaveChanged();
            }

            // --- FRUIT BONUS ---
            int fruitPoints = Fruit.Update(CurrentLevel, DotsRemaining, Pacman);
            if (fruitPoints > 0)
            {
                sound.PlayFruitEaten();
                AddScore(fruitPoints);
            }

            // TIMING DU MODE FRIGHTENED
            if (IsFrightenedMode)
            {
                FrightenedFramesRemaining--;
                if (FrightenedFramesRemaining <= 0)
                {
                    FrightenedFramesRemaining = 0;
                    EndFrightenedMode();
                }
            }

            sound.UpdateBackground(CurrentLevel, DotsRemaining,
                eyesRunning: Ghosts.Any(g => g.Phase == GhostPhase.Eaten),
                frightenedGhosts: Ghosts.Any(g => g.IsFrightened));

            ghostHouse.Update(CurrentLevel);

            // VITESSE ET MOUVEMENT DE PAC-MAN
            Pacman.Move(Map, CurrentSpecs, IsFrightenedMode);


            // VITESSE ET MOUVEMENT DES FANTÔMES
            // Elroy est suspendu après une mort, tant que Clyde n'est pas ressorti du nid
            if (elroySuspended)
            {
                Ghost clyde = Ghosts.FirstOrDefault(g => g is Clyde);
                // Suspension d'Elroy
                if (clyde == null || (clyde.Phase != GhostPhase.InHouse && clyde.Phase != GhostPhase.Exiting))
                    elroySuspended = false;
            }

            foreach (Ghost ghost in Ghosts)
            {
                if (ghost is Blinky blinky)
                    blinky.UpdateElroyStatus(DotsRemaining, CurrentSpecs, !elroySuspended);

                ghost.Move(Map, Pacman, CurrentSpecs, GlobalGhostMode);
            }

            // ANIMATIONS ET COLLISIONS
            frameCounter++;
            if (frameCounter % 6 == 0)
            {
                GhostAnimationFrame = (GhostAnimationFrame + 1) % 2;
            }

            CheckPelletCollision();

            // Si la dernière gomme vient d'être mangée, le niveau est gagné, même si un fantôme touche Pac-Man dans la même frame
            if (State == GameState.Victory) return;

            CheckGhostCollisions();


        }

        private void CheckPelletCollision()
        {
            int tileX = Pacman.TileX;
            int tileY = Pacman.TileY;

            if (tileX >= 0 && tileX < GameConfig.MapWidth && tileY >= 0 && tileY < GameConfig.MapHeight)
            {
                TileType currentTile = Map.Grid[tileY, tileX];

                if (currentTile == TileType.Dot) // Normal
                {
                    sound.PlayDot();
                    Map.Grid[tileY, tileX] = TileType.Empty;
                    AddScore(10);
                    Pacman.Stall(Pacman.DotStallFrames);
                    DotsRemaining--;
                    ghostHouse.OnDotEaten();
                    CheckLevelClear();
                }
                else if (currentTile == TileType.Energizer) // Super gomme
                {
                    Map.Grid[tileY, tileX] = TileType.Empty;
                    AddScore(50);
                    Pacman.Stall(Pacman.EnergizerStallFrames);
                    DotsRemaining--;
                    ghostHouse.OnDotEaten();
                    ghostsEatenInCurrentFrightenedMode = 0;

                    if (CurrentSpecs.FrightenedTimeSec > 0)
                    {
                        IsFrightenedMode = true;
                        FrightenedFramesRemaining = CurrentSpecs.FrightenedTimeSec * GameConfig.FramesPerSecond;

                        foreach (Ghost ghost in Ghosts)
                        {
                            if (ghost.Phase == GhostPhase.Eaten)
                                continue;

                            ghost.IsFrightened = true;
                            ghost.RequestReverse(); // Sans effet si le fantôme n'est pas Active
                        }
                    }
                    else
                    {
                        foreach (Ghost ghost in Ghosts) { ghost.RequestReverse(); }
                    }

                    CheckLevelClear();
                }
            }
        }

        private void CheckGhostCollisions()
        {
            // 1. Détermination de la tuile de Pac-Man (basée sur son centre)
            int pacmanTileX = Pacman.TileX;
            int pacmanTileY = Pacman.TileY;

            foreach (Ghost ghost in Ghosts)
            {
                // 2. Détermination de la tuile du fantôme
                int ghostTileX = ghost.TileX;
                int ghostTileY = ghost.TileY;

                // 3. Règle Arcade : Collision STRICTEMENT sur la même case
                if (pacmanTileX == ghostTileX && pacmanTileY == ghostTileY)
                {
                    if (ghost.IsFrightened)
                    {
                        sound.PlayGhostEaten();

                        // Passage immédiat en mode yeux (il commencera à courir APRÈS le freeze)
                        ghost.Phase = GhostPhase.Eaten;

                        // Calcul du score exponentiel (200, 400, 800, 1600)
                        ghostsEatenInCurrentFrightenedMode++;
                        int points = 200 * (int)Math.Pow(2, ghostsEatenInCurrentFrightenedMode - 1);
                        if (points > 1600) points = 1600; // Sécurité au cas où

                        AddScore(points);

                        // Sauvegarde des infos pour le dessin stationnaire du score
                        JustEatenGhost = ghost;
                        EatenGhostScoreX = (int)((ghost.X - 4));
                        EatenGhostScoreY = (int)((ghost.Y - 4));
                        EatenGhostScoreValue = points;

                        // Déclenchement du mode Freeze Arcade (1 seconde)
                        State = GameState.GhostEaten;
                        ghostEatenFreezeTimer = 60;
                        ghost.IsFrightened = false;

                        break; // On sort immédiatement de la boucle de détection
                    }
                    else if (ghost.Phase == GhostPhase.Active)
                    {
                        sound.PlayDeath(); // Coupe les sirènes et lance le son de mort dès le contact
                        State = GameState.Dying;
                        dyingTimer = 0;
                        break; // Inutile de vérifier les autres fantômes
                    }
                }
            }
        }

        private void CheckLevelClear()
        {
            if (DotsRemaining <= 0)
            {
                State = GameState.Victory;
                victoryTimer = 0;
                sound.StopAll(); // Silence à la fin du niveau, comme à l'arcade
            }
        }

        private void PrepareNextLifeOrGameOver()
        {
            Lives--;
            if (Lives <= 0)
            {
                State = GameState.GameOver;
                gameOverTimer = 180; // 180 frames = exactement 3 secondes à 60 Hz
                Fruit.ResetForNewLife(); // Le fruit et son score disparaissent de l'écran
            }
            else
            {
                // S'il reste des vies, on réinitialise directement les positions pour la vie suivante (Ready)
                ResetPositions(afterDeath: true);
                readyTimer = 120;        // 120 frames = ~2 secondes à 60 Hz
            }
        }

        /// <summary>
        /// Passe au niveau suivant en incrémentant le niveau et en réinitialisant la carte.
        /// </summary>
        private void StartNextLevel()
        {
            CurrentLevel++;
            IsNextLevelTransition = true;

            InitializeGameValue();
            readyTimer = 120;        // 120 frames = ~2 secondes à 60 Hz
        }

        /// <summary>
        /// Ajoute des points au score et vérifie l'attribution de la vie bonus arcade.
        /// </summary>
        private void AddScore(int points)
        {
            if (IsDemo) return;   // En démo, le score reste à 00 et le high score ne bouge pas

            if (scoreKeeper.Add(points))
            {
                Lives++;
                sound.PlayExtraLife();
            }
        }

        private void EndFrightenedMode()
        {
            IsFrightenedMode = false;
            foreach (Ghost ghost in Ghosts)
            {
                ghost.IsFrightened = false;
            }
        }

        private void OnWaveChanged()
        {
            foreach (Ghost ghost in Ghosts)
            {
                if (ghost.Phase == GhostPhase.Active)
                {
                    ghost.Mode = GlobalGhostMode;
                }

                // Demi-tour des fantômes actifs ; dans le nid, la demande est gardée pour la sortie
                ghost.RequestReverse();
            }
        }

        private void UpdateDying()
        {
            dyingTimer++;

            // 1. Gel : tout est figé et les fantômes restent visibles
            if (dyingTimer <= DeathFreezeFrames) return;

            // 2. Animation de mort (les fantômes disparaissent car Pac-Man n'est plus "Alive")
            int t = dyingTimer - DeathFreezeFrames - 1;
            if (t == 0)
            {
                Pacman.State = PacmanState.Dying;
            }

            int animationFrames = DeathSpriteCount * DeathFramesPerSprite;
            Pacman.DeathAnimationFrame = Math.Min(t / DeathFramesPerSprite, DeathSpriteCount - 1);

            if (IsDemo)
            {
                if (t >= animationFrames + DemoEndPauseFrames) IsFinished = true;
                return;
            }

            // 3. Pause finale, puis vie suivante ou game over
            if (t >= animationFrames + DeathEndPauseFrames)
            {
                PrepareNextLifeOrGameOver();
                Pacman.State = PacmanState.Alive;
            }
        }
        
        private void UpdateVictory()
        {
            if (IsDemo) { IsFinished = true; return; }

            victoryTimer++;

            // 1. Gel : tout est figé, fantômes visibles
            if (victoryTimer <= VictoryFreezeFrames) return;

            // 2. Les fantômes disparaissent et le labyrinthe clignote
            ShowGhosts = false;
            Fruit.ResetForNewLife();

            int t = victoryTimer - VictoryFreezeFrames - 1;
            IsMazeFlashWhite = (t / VictoryFlashFrames) % 2 == 0;

            // 3. Fin des clignotements : niveau suivant
            if (t >= VictoryFlashFrames * 2 * VictoryFlashCount)
            {
                StartNextLevel();
            }
        }
    }
}