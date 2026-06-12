using System;
using System.Collections.Generic;
using System.IO;
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
        private int houseTimer = 0;
        private int readyTimer = 0;
        private int gameOverTimer = 0;
        private int ghostsEatenInCurrentFrightenedMode = 0;
        private int ghostEatenFreezeTimer = 0;
        private bool isBonusLifeAwarded = false;
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
            string mapPath = Path.Combine(Application.StartupPath, "asset", "maps", "original_maze.txt");

            if (File.Exists(mapPath))
            {
                Map.LoadMapFromFile(mapPath);
                CountInitialPellets();
                ResetPositions();
            }
            else
            {
                throw new FileNotFoundException("Fichier de carte introuvable : " + mapPath);
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
            houseTimer = 0;

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
                    ReleaseGhostFromHouse("Pinky");
                    IsNextLevelTransition = false;
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
                    IsFrightenedMode = false;
                    foreach (Ghost ghost in Ghosts)
                    {
                        if (ghost.CurrentState == GhostState.Frightened)
                        {
                            ghost.CurrentState = GhostState.Scatter;
                        }
                    }
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

            // 3. SORTIE DU NID
            houseTimer++;
            if (houseTimer == 180)       // ~3 secondes après le début
            {
                ReleaseGhostFromHouse("Inky");
            }
            else if (houseTimer == 420)  // ~7 secondes après le début
            {
                ReleaseGhostFromHouse("Clyde");
            }

            // 4. VITESSE ET MOUVEMENT DES FANTÔMES
            foreach (Ghost ghost in Ghosts)
            {
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
                else
                {
                    ghost.Speed = CurrentSpecs.GhostNormal;
                }

                ghost.Move(Map, Pacman, CurrentLevel);
            }

            // 5. ANIMATIONS ET COLLISIONS
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
                    CheckLevelClear();
                }
                else if (currentTile == 3) // Super gomme
                {
                    Map.Grid[tileY, tileX] = 0;
                    AddScore(50);
                    DotsRemaining--;

                    ghostsEatenInCurrentFrightenedMode = 0;

                    if (CurrentSpecs.FrightenedTimeSec > 0)
                    {
                        IsFrightenedMode = true;
                        FrightenedMsRemaining = CurrentSpecs.FrightenedTimeSec * 1000;

                        foreach (Ghost ghost in Ghosts)
                        {
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
                    if (ghost.CurrentState == GhostState.Frightened)
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

        private void ReleaseGhostFromHouse(string ghostType)
        {
            foreach (Ghost ghost in Ghosts)
            {
                if (ghost.GetType().Name == ghostType && ghost.CurrentState == GhostState.InHouse)
                {
                    ghost.CurrentState = GhostState.Exiting;
                    break;
                }
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

            // On recharge la carte pour replacer toutes les gommes
            string mapPath = Path.Combine(Application.StartupPath, "asset", "maps", "original_maze.txt");

            if (File.Exists(mapPath))
            {
                Map.LoadMapFromFile(mapPath);
                CountInitialPellets();
                ResetPositions(); // Remet l'état à GameState.Ready, ShowCharacters = false, etc.
            }
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
    }
}