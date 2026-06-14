using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

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

        /// <summary>
        /// L'état de comportement global actuel imposé par le timer du jeu (Scatter ou Chase).
        /// </summary>
        public GhostState GlobalGhostState { get; private set; } = GhostState.Scatter;

        public Game()
        {
            Map = new LevelMap();
            Pacman = new Pacman();
            Ghosts = new List<Ghost>();

            Score = 0;
            Lives = 3;
            CurrentLevel = 1;
            State = GameState.Playing;

            InitializeGameValue();
        }

        private void InitializeGameValue()
        {
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
        }

        private void CountInitialPellets()
        {
            DotsRemaining = 0;
            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    int tile = Map.Grid[r, c];
                    if (tile == 2 || tile == 3)
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

        public void ResetPositions()
        {
            this.CurrentSpecs = GameConfig.GetSpecsForLevel(this.CurrentLevel);
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

            // Reset du fruit temporaire pour la nouvelle vie
            IsFruitActive = false;
            IsFruitScoreActive = false;

            inactivityTimerMs = 0;
            // Si on est au tout début du niveau 1, on veut le mode individuel.
            // Mais si on réinitialise après une mort (Lives < 3 par exemple), on active le compteur global.
            if (Score > 0)
            {
                useGlobalDotCounter = true;
            }
            else
            {
                useGlobalDotCounter = false;
                ConfigureGhostDotLimits(); // Initialise les limites de base (0, 30, 60)
            }

            globalDotCounter = 0;

            currentWaveIndex = 0;
            waveTimerMs = 0;
            GlobalGhostState = GhostState.Scatter;

            State = GameState.Ready;
            readyTimer = 240;        // 240 frames = ~4 secondes à 60 Hz
            ShowCharacters = false;  // Cachés au tout début !
        }

        private void SpawnPacman()
        {
            Pacman.X = (13 * GameConfig.TileSize)+4;
            Pacman.Y = (23 + 3) * GameConfig.TileSize;
            Pacman.CurrentDirection = Keys.Left;
            Pacman.NextDirection = Keys.None;
            Pacman.State = PacmanState.Alive;
        }

        public void HandleInput(Keys key)
        {
            if (key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right)
            {
                Pacman.NextDirection = key;
            }
        }

        // Ta logique fixe à 60 Hz isolée
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
                    Ghost preferred = GetPreferredGhostInHouse();
                    if (preferred != null && preferred.DotCounter >= preferred.DotLimit && !useGlobalDotCounter)
                    {
                        preferred.CurrentState = GhostState.Exiting;
                    }
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

            // Le chrono de sortie ne tourne QUE si le joueur est en train de jouer activement
            if (State == GameState.Playing)
            {
                Ghost ghostToRelease = GetPreferredGhostInHouse();

                if (ghostToRelease != null)
                {
                    inactivityTimerMs += 16; // ~16.67ms par frame sous 60Hz

                    // Règle des 4 secondes d'inactivité
                    if (inactivityTimerMs >= 4000)
                    {
                        inactivityTimerMs = 0;
                        ghostToRelease.CurrentState = GhostState.Exiting; // Sortie forcée !
                    }
                }
                else
                {
                    inactivityTimerMs = 0; // Personne dans le nid, on reset le timer
                }
            }

            // VITESSE ET MOUVEMENT DE PAC-MAN
            // COMPORTEMENT ARCADE : On ajoute +4 pour tester le centre de Pac-Man
            int pTileX = (int)((Pacman.X + 4) / GameConfig.TileSize);
            int pTileY = (int)((Pacman.Y + 4) / GameConfig.TileSize);

            bool isPacmanEating = false;
            if (pTileX >= 0 && pTileX < GameConfig.MapWidth && pTileY >= 0 && pTileY < GameConfig.MapHeight)
            {
                int tile = Map.Grid[pTileY, pTileX];
                isPacmanEating = (tile == 2 || tile == 3);
            }

            if (IsFrightenedMode)
            {
                Pacman.Speed = isPacmanEating ? CurrentSpecs.FrightenedPacmanDots : CurrentSpecs.FrightenedPacman;
            }
            else
            {
                Pacman.Speed = isPacmanEating ? CurrentSpecs.PacmanDots : CurrentSpecs.PacmanNormal;
            }

            Pacman.Move(Map, CurrentLevel, IsFrightenedMode);


            // VITESSE ET MOUVEMENT DES FANTÔMES
            foreach (Ghost ghost in Ghosts)
            {

                if (ghost is Blinky blinky)
                {
                    // On met à jour Elroy TOUT LE TEMPS (flags + vitesse de base d'Blinky)
                    blinky.UpdateElroyStatus(CurrentLevel, DotsRemaining, CurrentSpecs);
                }
                else
                {
                    // Vitesse de base standard pour Pinky, Inky et Clyde
                    ghost.Speed = CurrentSpecs.GhostNormal;
                }

                int gTileX = (int)(ghost.X / GameConfig.TileSize);
                int gTileY = (int)(ghost.Y / GameConfig.TileSize);
                bool isInTunnel = (gTileY == 17 && (gTileX <= 5 || gTileX >= 22));

                if (ghost.CurrentState == GhostState.Frightened)
                {
                    ghost.Speed = CurrentSpecs.FrightenedGhost;
                }
                else if (isInTunnel)
                {
                    ghost.Speed = CurrentSpecs.GhostTunnel;
                }
              
                ghost.Move(Map, Pacman, CurrentLevel, GlobalGhostState);
            }

            // ANIMATIONS ET COLLISIONS
            frameCounter++;
            if (frameCounter % 6 == 0)
            {
                GhostAnimationFrame = (GhostAnimationFrame + 1) % 2;
            }

            CheckPelletCollision();
            CheckGhostCollisions();

            // Mise à jour en temps réel du High Score si le joueur le bat
            if (Score > HighScore)
            {
                HighScore = Score;
            }
        }

        private void PelletEated()
        {
            
            inactivityTimerMs = 0; // Une gomme est mangée ! On reset immédiatement le timer de sécurité.

            if (useGlobalDotCounter)
            {
                globalDotCounter++;

                Ghost pinky = Ghosts.FirstOrDefault(g => g.GetType().Name == "Pinky" && g.CurrentState == GhostState.InHouse);
                Ghost inky = Ghosts.FirstOrDefault(g => g.GetType().Name == "Inky" && g.CurrentState == GhostState.InHouse);

                // Règle post-mort : Pinky sort à 7, Inky à 17
                if (pinky != null && globalDotCounter == 7)
                {
                    pinky.CurrentState = GhostState.Exiting;
                }
                else if (inky != null && globalDotCounter == 17)
                {
                    inky.CurrentState = GhostState.Exiting;
                }
                // Règle absolue de l'arcade : si le compteur global atteint 32 et que Clyde est dedans,
                // on stoppe le compteur global et on repasse instantanément aux compteurs individuels !
                else if (globalDotCounter >= 32)
                {
                    useGlobalDotCounter = false;
                    globalDotCounter = 0;
                }
            }
            else
            {
                // Mode Normal : On incrémente le compteur du fantôme prioritaire dans le nid
                Ghost preferredGhost = GetPreferredGhostInHouse();
                if (preferredGhost != null)
                {
                    preferredGhost.DotCounter++;
                    if (preferredGhost.DotCounter >= preferredGhost.DotLimit)
                    {
                        preferredGhost.CurrentState = GhostState.Exiting;
                    }
                }
            }
        }

        private void CheckPelletCollision()
        {
            // COMPORTEMENT ARCADE : Collision basée sur le centre (+4 pixels)
            int tileX = (int)((Pacman.X + 4) / GameConfig.TileSize);
            int tileY = (int)((Pacman.Y + 4) / GameConfig.TileSize);

            if (tileX >= 0 && tileX < GameConfig.MapWidth && tileY >= 0 && tileY < GameConfig.MapHeight)
            {
                int currentTile = Map.Grid[tileY, tileX];

                if (currentTile == 2) // Normal
                {
                    Map.Grid[tileY, tileX] = 0;
                    AddScore(10);
                    DotsRemaining--;
                    PelletEated();
                    CheckLevelClear();
                }
                else if (currentTile == 3) // Super gomme
                {
                    Map.Grid[tileY, tileX] = 0;
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
                            if (ghost.CurrentState == GhostState.Eaten)
                                continue;
                            
                            ghost.IsFrightened = true;

                            if (ghost.CurrentState == GhostState.Chase || ghost.CurrentState == GhostState.Scatter)
                            {
                                ghost.CurrentState = GhostState.Frightened;
                                ghost.ForceDemiTour();
                            }
                            else if (ghost.CurrentState == GhostState.Frightened)
                            {
                                ghost.ForceDemiTour();
                            }
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
            int pacmanTileX = (int)((Pacman.X + 4) / GameConfig.TileSize);
            int pacmanTileY = (int)((Pacman.Y + 4) / GameConfig.TileSize);

            foreach (Ghost ghost in Ghosts)
            {
                // 2. Détermination de la tuile du fantôme (en cohérence avec ton étape 4 de Update())
                int ghostTileX = (int)(ghost.X / GameConfig.TileSize);
                int ghostTileY = (int)(ghost.Y / GameConfig.TileSize);

                // 3. Règle Arcade : Collision STRICTEMENT sur la même case
                if (pacmanTileX == ghostTileX && pacmanTileY == ghostTileY)
                {
                    if (ghost.CurrentState == GhostState.Frightened || (ghost.IsFrightened && ghost.CurrentState != GhostState.Eaten))
                    {
                        // Passage immédiat en mode yeux (il commencera à courir APRÈS le freeze)
                        ghost.CurrentState = GhostState.Eaten;

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
                    else if (ghost.CurrentState == GhostState.Scatter || ghost.CurrentState == GhostState.Chase)
                    {
                        // On change l'état interne pour signaler la mort au MainForm
                        State = GameState.Dying;
                        Pacman.State = PacmanState.Dying;
                        Pacman.DeathAnimationFrame = 0;
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
                ResetPositions();
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

            // Règle Arcade : Si on franchit le seuil et qu'on ne l'a pas encore eu dans cette partie
            if (!isBonusLifeAwarded && Score >= GameConfig.BonusLifeThreshold)
            {
                Lives++;
                isBonusLifeAwarded = true;

                // Note : Si plus tard tu ajoutes des bruitages, 
                // c'est ici qu'il faudra déclencher le son de la vie supplémentaire !
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
            int pacmanTileX = (int)((Pacman.X + 4) / GameConfig.TileSize);
            int pacmanTileY = (int)((Pacman.Y + 4) / GameConfig.TileSize);

            int fruitTileX = (int)((FruitX + 4) / GameConfig.TileSize); // Donnera colonne 14
            int fruitTileY = (int)((FruitY + 4) / GameConfig.TileSize); // Donnera ligne 20

            if (pacmanTileX == fruitTileX && pacmanTileY == fruitTileY)
            {
                IsFruitActive = false;
                AddScore(CurrentFruitValue);

                // Déclenche l'affichage du score flottant
                IsFruitScoreActive = true;
                FruitScoreTimer = 120; // Affiché pendant 2 secondes (120 frames), sans figer le jeu
            }
        }
        private void ConfigureGhostDotLimits()
        {
            foreach (Ghost ghost in Ghosts)
            {
                ghost.DotCounter = 0;
                string name = ghost.GetType().Name;

                if (name == "Pinky") ghost.DotLimit = 0;    // Sort immédiatement
                if (name == "Inky") ghost.DotLimit = 30;    // Sort après 30 gommes
                if (name == "Clyde") ghost.DotLimit = 60;   // Sort après 60 gommes
            }
        }

        private Ghost GetPreferredGhostInHouse()
        {
            // Ordre de priorité immuable de l'arcade
            foreach (var name in new[] { "Pinky", "Inky", "Clyde" })
            {
                Ghost g = Ghosts.FirstOrDefault(x => x.GetType().Name == name);
                if (g != null && g.CurrentState == GhostState.InHouse)
                {
                    return g;
                }
            }
            return null;
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
                GhostState newState = (currentWaveIndex % 2 == 0) ? GhostState.Scatter : GhostState.Chase;

                if (newState != GlobalGhostState)
                {
                    GlobalGhostState = newState;
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
                if (ghost.CurrentState == GhostState.Frightened)
                {
                    ghost.CurrentState = GlobalGhostState; // On les réaligne sur la vague globale !
                }
            }
        }

        private void ChangerVagueGlobale()
        {
            foreach (Ghost ghost in Ghosts)
            {
                // Règle très importante : On ne change l'état et on ne force le demi-tour 
                // QUE pour les fantômes actuellement actifs en mode Chase ou Scatter.
                // Si un fantôme est InHouse, Exiting ou Eaten (yeux), on ne casse pas son état !
                if (ghost.CurrentState == GhostState.Scatter || ghost.CurrentState == GhostState.Chase)
                {
                    ghost.CurrentState = GlobalGhostState;
                    ghost.ForceDemiTour();
                }
            }
        }

    }
}