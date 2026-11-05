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
        public List<Ghost> Ghosts { get; private set; }

        public int Score { get; private set; }
        public int DotsRemaining { get; private set; }
        public int Lives { get; private set; }
        public int CurrentLevel { get; private set; }

        public bool IsFrightenedMode { get; private set; }
        public int FrightenedMsRemaining { get; private set; }
        public LevelSpecs CurrentSpecs { get; private set; }

        public int GhostAnimationFrame { get; private set; }
        public GameState State { get; private set; }
        public bool ShowCharacters { get; private set; } = true;
        public int HighScore { get; private set; } = 0;
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
        private bool isBonusLifeAwarded = false;
        private bool useGlobalDotCounter = false;
        private int globalDotCounter = 0;
        private int inactivityTimerMs = 0;
        // --- ÉTAT ET LOGIQUE DES FRUITS ---
        public bool IsFruitActive { get; private set; } = false;
        public int FruitX { get; private set; }
        public int FruitY { get; private set; }
        public int FruitTimer { get; private set; } = 0;

        public bool IsFruitScoreActive { get; private set; } = false;
        public int FruitScoreTimer { get; private set; } = 0;
        public int CurrentFruitValue { get; private set; } = 100;

        private int initialDotsCount = 0;
        private bool fruit70Spawned = false;
        private bool fruit170Spawned = false;

        // Suivi des vagues Scatter/Chase
        private int currentWaveIndex = 0;       // Index de la vague actuelle (0 à 7)
        private double waveTimerMs = 0;         // Temps accumulé dans la vague actuelle

        private AudioManager audioManager;
        private bool wakaToggle = false;
        private int currentSirenIndex = -1; // -1 pour forcer le premier chargement au démarrage
        private bool elroySuspended = false;

        /// <summary>
        /// Le mode de comportement global imposé par le timer des vagues (Scatter ou Chase).
        /// </summary>
        public GhostMode GlobalGhostMode { get; private set; } = GhostMode.Scatter;

        public Game()
        {
            Map = new LevelMap();
            Pacman = new Pacman();
            Ghosts = new List<Ghost>();

            audioManager = new AudioManager();
            
            Score = 0;
            Lives = 3;
            CurrentLevel = 1;
            State = GameState.Playing;

            InitializeGameValue();
        }

        private void InitializeGameValue()
        {
            audioManager.StopAll();
            
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
                }
                else
                {
                    throw new FileNotFoundException("Fichier de carte embarqué introuvable : " + resourceName);
                }
            }
            if(!IsNextLevelTransition)
                audioManager.Play("start");
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

            // Initialisation pour le suivi des fruits (ne se réinitialise pas à la mort)
            initialDotsCount = DotsRemaining;
            fruit70Spawned = false;
            fruit170Spawned = false;
        }

        public void ResetPositions(bool afterDeath = false)
        {
            currentSirenIndex = -1;
            this.CurrentSpecs = GameConfig.GetSpecsForLevel(this.CurrentLevel);

            // Les compteurs individuels survivent à une mort (les fantômes sont recréés)
            var savedCounters = new Dictionary<string, int>();
            if (afterDeath)
            {
                foreach (Ghost g in Ghosts) savedCounters[g.GetType().Name] = g.DotCounter;
            }

            Ghosts.Clear();

            Blinky blinky = new Blinky();
            Pinky pinky = new Pinky();
            Inky inky = new Inky(blinky);
            Clyde clyde = new Clyde();

            Ghosts.Add(blinky);
            Ghosts.Add(pinky);
            Ghosts.Add(inky);
            Ghosts.Add(clyde);

            SpawnPacman();

            IsFruitActive = false;
            IsFruitScoreActive = false;
            inactivityTimerMs = 0;

            ConfigureGhostDotLimits();
            foreach (Ghost g in Ghosts)
            {
                if (savedCounters.TryGetValue(g.GetType().Name, out int saved))
                    g.DotCounter = saved;
            }

            // Compteur global : uniquement après une mort (pas au début d'un niveau)
            useGlobalDotCounter = afterDeath;
            globalDotCounter = 0;
            elroySuspended = afterDeath;

            currentWaveIndex = 0;
            waveTimerMs = 0;
            GlobalGhostMode = GhostMode.Scatter;

            State = GameState.Ready;
            readyTimer = 240;
            ShowCharacters = false;
        }

        private void SpawnPacman()
        {
            Pacman.X = MazeLayout.PacmanStartX;
            Pacman.Y = MazeLayout.PacmanStartY;
            Pacman.CurrentDirection = Direction.Left;
            Pacman.NextDirection = Direction.None;
            Pacman.State = PacmanState.Alive;
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
                if (readyTimer <= 120) ShowCharacters = true;
                if (readyTimer <= 0)
                {
                    State = GameState.Playing;
                    IsNextLevelTransition = false;
                    ReleaseGhostByCounter();
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
                    // Le temps est écoulé (2-3s), on relance une partie propre (qui repassera en READY!)
                    RestartGame();
                }
                return; // On fige toute la physique du labyrinthe pendant le Game Over
            }

            if (State != GameState.Playing) return;

            UpdateTimerVagues(16.67);

            // --- GESTION DU FRUIT (TIMERS ET COLLISION) ---
            if (IsFruitActive)
            {
                FruitTimer--;
                if (FruitTimer <= 0)
                {
                    IsFruitActive = false; // Le fruit disparait après ~10 secondes
                }
                else
                {
                    CheckFruitCollision();
                }
            }

            if (IsFruitScoreActive)
            {
                FruitScoreTimer--;
                if (FruitScoreTimer <= 0)
                {
                    IsFruitScoreActive = false;
                }
            }

            // --- VÉRIFICATION DES SEUILS DE SPAWN (70 et 170 gommes mangées) ---
            int dotsEaten = initialDotsCount - DotsRemaining;

            if (!fruit70Spawned && dotsEaten >= 70)
            {
                fruit70Spawned = true;
                SpawnFruit();
            }
            if (!fruit170Spawned && dotsEaten >= 170)
            {
                fruit170Spawned = true;
                SpawnFruit();
            }

            // TIMING DU MODE FRIGHTENED
            if (IsFrightenedMode)
            {
                FrightenedMsRemaining -= 16;
                if (FrightenedMsRemaining <= 0)
                {
                    FrightenedMsRemaining = 0;
                    EndFrightenedMode();
                }
            }

            CheckSirenUpdate();
            
            ReleaseGhostByCounter();
            
            // Le chrono de sortie ne tourne QUE si le joueur est en train de jouer activement
            Ghost ghostToRelease = GetPreferredGhostInHouse();

            if (ghostToRelease != null)
            {
                inactivityTimerMs += 16; // ~16.67ms par frame sous 60Hz

                // Règle d'inactivité en fonction du niveau
                int inactivityLimitMs = (CurrentLevel < 5) ? 4000 : 3000;
                if (inactivityTimerMs >= inactivityLimitMs)
                {
                    inactivityTimerMs = 0;
                    ghostToRelease.Phase = GhostPhase.Exiting; // Sortie forcée !
                }
            }
            else
            {
                inactivityTimerMs = 0; // Personne dans le nid, on reset le timer
            }
            

            // VITESSE ET MOUVEMENT DE PAC-MAN
            Pacman.Move(Map, CurrentLevel, IsFrightenedMode);


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

                ghost.Move(Map, Pacman, CurrentLevel, GlobalGhostMode);
            }

            if (!Ghosts.Any(g => g.Phase == GhostPhase.Eaten))
                audioManager.Stop("eyes");



            // ANIMATIONS ET COLLISIONS
            frameCounter++;
            if (frameCounter % 6 == 0)
            {
                GhostAnimationFrame = (GhostAnimationFrame + 1) % 2;
            }

            CheckPelletCollision();
            CheckGhostCollisions();


        }

        private void PelletEated()
        {
            inactivityTimerMs = 0;

            if (useGlobalDotCounter)
            {
                globalDotCounter++;

                Ghost pinky = Ghosts.FirstOrDefault(g => g is Pinky && g.Phase == GhostPhase.InHouse);
                Ghost inky = Ghosts.FirstOrDefault(g => g is Inky && g.Phase == GhostPhase.InHouse);
                Ghost clyde = Ghosts.FirstOrDefault(g => g is Clyde && g.Phase == GhostPhase.InHouse);

                if (globalDotCounter == 7 && pinky != null) pinky.Phase = GhostPhase.Exiting;
                if (globalDotCounter == 17 && inky != null) inky.Phase = GhostPhase.Exiting;

                if (globalDotCounter == 32 && clyde != null)
                {
                    clyde.Phase = GhostPhase.Exiting;
                    useGlobalDotCounter = false;
                    globalDotCounter = 0;
                }
            }
            else
            {
                Ghost preferred = GetPreferredGhostInHouse();
                if (preferred != null) preferred.DotCounter++;
            }
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
                    wakaToggle = !wakaToggle;
                    audioManager.Play(wakaToggle ? "eat_dot_0" : "eat_dot_1");

                    Map.Grid[tileY, tileX] = TileType.Empty;
                    AddScore(10);
                    DotsRemaining--;
                    PelletEated();
                    CheckLevelClear();
                }
                else if (currentTile == TileType.Energizer) // Super gomme
                {
                    audioManager.Stop($"siren{currentSirenIndex}"); // Coupe la sirène en cours
                    audioManager.PlaySequence("fright_firstloop", "fright", loopNext: true);

                    Map.Grid[tileY, tileX] = TileType.Empty;
                    AddScore(50);
                    DotsRemaining--;
                    PelletEated();
                    ghostsEatenInCurrentFrightenedMode = 0;

                    if (CurrentSpecs.FrightenedTimeSec > 0)
                    {
                        IsFrightenedMode = true;
                        FrightenedMsRemaining = CurrentSpecs.FrightenedTimeSec * 1000;

                        foreach (Ghost ghost in Ghosts)
                        {
                            if (ghost.Phase == GhostPhase.Eaten)
                                continue;

                            ghost.IsFrightened = true;
                            ghost.ForceDemiTour(); // Sans effet si le fantôme n'est pas Active
                        }
                    }
                    else
                    {
                        foreach (Ghost ghost in Ghosts) { ghost.ForceDemiTour(); }
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
                        audioManager.Play("eat_ghost");

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

                        audioManager.PlaySequence("eyes_firstloop", "eyes", loopNext: true);

                        break; // On sort immédiatement de la boucle de détection
                    }
                    else if (ghost.Phase == GhostPhase.Active)
                    {
                        audioManager.StopAll();
                        // On change l'état interne pour signaler la mort au MainForm
                        State = GameState.Dying;
                        Pacman.State = PacmanState.Dying;
                        Pacman.DeathAnimationFrame = 0;
                        // Choisis le son de mort selon ce que tu as (souvent death_0 est le bruit de dissolution)
                        audioManager.PlaySequence("death_0", "death_1",false);
                        break; // Inutile de vérifier les autres fantômes si Pac-Man est mort
                    }
                }
            }
        }

        private void CheckLevelClear()
        {
            if (DotsRemaining <= 0)
            {
                State = GameState.Victory;
            }
        }

        public void SetPacmanState(PacmanState state)
        {
            Pacman.State = state;
        }

        public void PrepareNextLifeOrGameOver()
        {
            Lives--;
            if (Lives <= 0)
            {
                State = GameState.GameOver;
                gameOverTimer = 180; // 180 frames = exactement 3 secondes à 60 Hz
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
        public void StartNextLevel()
        {
            CurrentLevel++;
            IsNextLevelTransition = true;

            InitializeGameValue();
            readyTimer = 120;        // 120 frames = ~2 secondes à 60 Hz
        }

        /// <summary>
        /// Réinitialise complètement le jeu à zéro pour démarrer une nouvelle partie (futur retour écran titre).
        /// </summary>
        public void RestartGame()
        {
            Score = 0;
            Lives = 3;
            CurrentLevel = 1;
            IsNextLevelTransition = false;
            isBonusLifeAwarded = false;
            

            // Recharge la carte initiale, compte les pellets et déclenche le ResetPositions en mode Ready
            InitializeGameValue();
        }

        /// <summary>
        /// Ajoute des points au score et vérifie l'attribution de la vie bonus arcade.
        /// </summary>
        private void AddScore(int points)
        {
            Score += points;

            // Mise à jour en temps réel du High Score si le joueur le bat
            if (Score > HighScore)
            {
                HighScore = Score;
            }

            // Règle Arcade : Si on franchit le seuil et qu'on ne l'a pas encore eu dans cette partie
            if (!isBonusLifeAwarded && Score >= GameConfig.BonusLifeThreshold)
            {
                Lives++;
                isBonusLifeAwarded = true;

                audioManager.Play("extend");
            }
        }

        private void SpawnFruit()
        {
            IsFruitActive = true;
            FruitTimer = 600; // 600 frames = 10 secondes exactes à 60 Hz

            // Alignement parfait sous le nid (Même X que le spawn de Pac-man, ligne 20)
            FruitX = (13 * GameConfig.TileSize);
            FruitY = (20 * GameConfig.TileSize) - 4;

            CurrentFruitValue = GetFruitValueForLevel(CurrentLevel);
        }

        private int GetFruitValueForLevel(int level)
        {
            if (level == 1) return 100;
            if (level == 2) return 300;
            if (level == 3 || level == 4) return 500;
            if (level == 5 || level == 6) return 700;
            if (level == 7 || level == 8) return 1000;
            if (level == 9 || level == 10) return 2000;
            if (level == 11 || level == 12) return 3000;
            return 5000; // Niveau 13+ (Clé)
        }

        private void CheckFruitCollision()
        {
            // Collision basée sur les cases centrales (Règle arcade)
            int pacmanTileX = Pacman.TileX;
            int pacmanTileY = Pacman.TileY;

            int fruitTileX = Tiles.Of(FruitX);
            int fruitTileY = Tiles.Of(FruitY);

            if (pacmanTileX == fruitTileX && pacmanTileY == fruitTileY)
            {
                audioManager.Play("eat_fruit");

                IsFruitActive = false;
                AddScore(CurrentFruitValue);

                // Déclenche l'affichage du score flottant
                IsFruitScoreActive = true;
                FruitScoreTimer = 120; // Affiché pendant 2 secondes (120 frames), sans figer le jeu
            }
        }
        private void ConfigureGhostDotLimits()
        {
            int inkyLimit, clydeLimit;
            if (CurrentLevel == 1) { inkyLimit = 30; clydeLimit = 60; }
            else if (CurrentLevel == 2) { inkyLimit = 0; clydeLimit = 50; }
            else { inkyLimit = 0; clydeLimit = 0; }

            foreach (Ghost ghost in Ghosts)
            {
                ghost.DotCounter = 0;
                string name = ghost.GetType().Name;

                if (name == "Pinky") ghost.DotLimit = 0;
                else if (name == "Inky") ghost.DotLimit = inkyLimit;
                else if (name == "Clyde") ghost.DotLimit = clydeLimit;
            }
        }

        private Ghost GetPreferredGhostInHouse()
        {
            // Ordre de priorité immuable de l'arcade
            foreach (var name in new[] { "Pinky", "Inky", "Clyde" })
            {
                Ghost g = Ghosts.FirstOrDefault(x => x.GetType().Name == name);
                if (g != null && g.Phase == GhostPhase.InHouse)
                {
                    return g;
                }
            }
            return null;
        }

        private void ReleaseGhostByCounter()
        {
            if (useGlobalDotCounter) return;
            if (Ghosts.Any(g => g.Phase == GhostPhase.Exiting)) return; // un seul à la fois

            Ghost preferred = GetPreferredGhostInHouse();
            if (preferred != null && preferred.DotCounter >= preferred.DotLimit)
                preferred.Phase = GhostPhase.Exiting;
        }

        public void UpdateTimerVagues(double elapsedTimeMs)
        {
            // Règle Arcade : Le timer global des vagues est figé si le jeu est en mode Frightened
            if (IsFrightenedMode)
                return;

            // Si on est sur la dernière vague (Chase infini), inutile d'incrémenter
            if (currentWaveIndex >= 7)
                return;

            waveTimerMs += elapsedTimeMs;
            double currentDuration = GameConfig.GetWaveDurationMs(CurrentLevel, currentWaveIndex);

            if (waveTimerMs >= currentDuration)
            {
                // Passage à la vague suivante
                currentWaveIndex++;
                waveTimerMs = 0;

                // Index pair = Scatter, Index impair = Chase
                GhostMode newMode = (currentWaveIndex % 2 == 0) ? GhostMode.Scatter : GhostMode.Chase;

                if (newMode != GlobalGhostMode)
                {
                    GlobalGhostMode = newMode;
                    ChangerVagueGlobale();
                }
            }
        }

        private void EndFrightenedMode()
        {
            IsFrightenedMode = false;
            foreach (Ghost ghost in Ghosts)
            {
                ghost.IsFrightened = false;
            }

            audioManager.Stop("fright");
            audioManager.Stop("eyes");
            audioManager.PlaySequence(
                    $"siren{currentSirenIndex}_firstloop",
                    $"siren{currentSirenIndex}",
                    loopNext: true);
        }

        private void ChangerVagueGlobale()
        {
            foreach (Ghost ghost in Ghosts)
            {
                // Seuls les fantômes actifs changent de mode et font demi-tour.
                // InHouse, Exiting et Eaten ne sont pas touchés.
                if (ghost.Phase == GhostPhase.Active)
                {
                    ghost.Mode = GlobalGhostMode;
                    ghost.ForceDemiTour();
                }
            }
        }

        private void CheckSirenUpdate()
        {
            // 1. Définition des seuils de l'arcade (Gommes restantes)
            int thresholdSiren1 = 164;
            int thresholdSiren2 = 144;
            int thresholdSiren3 = 124;
            int thresholdSiren4 = 104;

            // Ajustement des seuils selon le niveau actuel
            if (CurrentLevel == 2)
            {
                thresholdSiren1 = 174;
                thresholdSiren2 = 154;
                thresholdSiren3 = 134;
                thresholdSiren4 = 114;
            }
            else if (CurrentLevel >= 3 && CurrentLevel <= 5)
            {
                thresholdSiren1 = 184;
                thresholdSiren2 = 164;
                thresholdSiren3 = 144;
                thresholdSiren4 = 124;
            }
            else if (CurrentLevel >= 6 && CurrentLevel <= 20)
            {
                thresholdSiren1 = 194;
                thresholdSiren2 = 174;
                thresholdSiren3 = 154;
                thresholdSiren4 = 134;
            }
            else if (CurrentLevel >= 21)
            {
                thresholdSiren1 = 204;
                thresholdSiren2 = 184;
                thresholdSiren3 = 164;
                thresholdSiren4 = 144;
            }

            // 2. Détermination de la sirène cible en fonction du nombre de gommes restantes
            int targetSiren = 0;

            if (DotsRemaining <= thresholdSiren4)
            {
                targetSiren = 4;
            }
            else if (DotsRemaining <= thresholdSiren3)
            {
                targetSiren = 3;
            }
            else if (DotsRemaining <= thresholdSiren2)
            {
                targetSiren = 2;
            }
            else if (DotsRemaining <= thresholdSiren1)
            {
                targetSiren = 1;
            }
            else
            {
                targetSiren = 0;
            }

            // 3. Si l'état des fantômes est "Frightened" ou "Eyes Only", on met à jour l'index en mémoire
            // mais on ne déclenche pas le son pour ne pas écraser les bruitages prioritaires
            // (La sirène reprendra au bon index lors du retour à l'état normal)
            bool status = false;
            foreach (Ghost ghost in Ghosts)
            {
                if ((ghost.Phase == GhostPhase.Active && ghost.IsFrightened) || ghost.Phase == GhostPhase.Eaten)
                {
                    status = true; break;
                }
            }

            if (status)
            {
                currentSirenIndex = targetSiren;
                return;
            }

            // 4. Si la sirène calculée est différente de celle qui joue actuellement, on change !
            if (targetSiren != currentSirenIndex)
            {
                // On coupe l'ancienne sirène s'il y en avait une
                if (currentSirenIndex >= 0)
                {
                    audioManager.Stop($"siren{currentSirenIndex}");
                }

                currentSirenIndex = targetSiren;

                // On lance la nouvelle séquence : Intro (firstloop) -> Boucle infinie
                audioManager.PlaySequence(
                    $"siren{currentSirenIndex}_firstloop",
                    $"siren{currentSirenIndex}",
                    loopNext: true
                );
            }
        }

    }
}