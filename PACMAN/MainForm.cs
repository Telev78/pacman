using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public partial class MainForm : Form
    {
        private SpriteManager spriteManager;
        private Bitmap backgroundMaze;
        private Timer gameTimer;
        // Timer dédié à la séquence de mort (indépendant du gameTimer)
        private Timer deathAnimationTimer;

        private LevelMap levelMap;
        private Pacman pacman;
        private bool isLevelEndingFlash = false; // Passer à true pour déclencher l'animation de fin de niveau
        private int score = 0;
        private int dotsRemaining = 0;
        private int lives = 3; // Pac-Man commence généralement avec 3 vies
        private List<Ghost> ghosts = new List<Ghost>();
        private Blinky blinky;
        private int ghostAnimationFrame = 0;
        private int frameCounter = 0;
        private int houseTimer = 0; // Compteur de boucles de jeu
        private int currentLevel = 1;
        private bool isFrightenedMode = false; // Sera mis à "true" quand on mangera une grosse gomme
        private LevelSpecs currentSpecs;            // Contient toutes les vitesses et configurations du niveau actuel
        private int frightenedTicksRemaining = 0;   // Compteur de ticks pour la durée de la Super-Gomme

        public MainForm()
        {
            this.Text = "Pac-Man Arcade (C#)";
            this.Size = new Size(GameConfig.ScreenWidth + 16, GameConfig.ScreenHeight + 39);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;

            this.KeyDown += new KeyEventHandler(OnKeyDown);

            spriteManager = new SpriteManager();
            levelMap = new LevelMap();
            pacman = new Pacman();
            blinky = new Blinky();
            ghosts.Add(blinky); // On ajoute Blinky à la liste globale des fantômes

            // --- CONFIGURATION DU TILE ENGINE VIA FICHIER DATA ---
            // Le fichier est maintenant inclus dans le projet et déployé par Visual Studio !
            string mapPath = Path.Combine(Application.StartupPath, "asset", "maps", "original_maze.txt");

            if (File.Exists(mapPath))
            {
                levelMap.LoadMapFromFile(mapPath);
                CountInitialPellets(); // <-- Compte les gommes dès que la map est chargée
                
                // INTRO : On affiche le score et les 3 vies initiales, 
                // mais Pac-Man n'est pas encore injecté.
                UpdateGameTitleScore();

                // Simulation du "READY!" : On fait spawner Pac-Man juste après.
                // (Plus tard, on pourra mettre un vrai timer de 2 secondes ici pour la musique d'intro !)
                ResetPositions();
            }
            else
            {
                // Sécurité au cas où le fichier est manquant au runtime
                MessageBox.Show("Fichier de carte introuvable : " + mapPath, "Erreur Critique", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            // -----------------------------------------------------

            deathAnimationTimer = new System.Windows.Forms.Timer();
            deathAnimationTimer.Interval = 70; // Vitesse de l'animation de mort
            deathAnimationTimer.Tick += DeathAnimationTimer_Tick;

            gameTimer = new Timer();
            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
        }

        // Capture les appuis sur le clavier
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down ||
                e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                // On stocke l'intention du joueur
                pacman.NextDirection = e.KeyCode;
            }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            // --- 1. GESTION DU TIMING DU MODE FRIGHTENED (SUPER-GOMME) ---
            if (isFrightenedMode)
            {
                frightenedTicksRemaining--;
                if (frightenedTicksRemaining <= 0)
                {
                    isFrightenedMode = false;
                    // On repasse tous les fantômes vivants en mode Normal (Chase/Scatter)
                    foreach (Ghost ghost in ghosts)
                    {
                        if (ghost.CurrentState == GhostState.Frightened)
                        {
                            ghost.CurrentState = GhostState.Scatter; // Ou ton état par défaut
                        }
                    }
                }
            }

            // --- 2. CALCUL ET INJECTION DE LA VITESSE DE PAC-MAN ---
            int pTileX = (int)(pacman.X / GameConfig.TileSize);
            int pTileY = (int)(pacman.Y / GameConfig.TileSize);

            // Est-ce que Pac-Man est actuellement sur une case contenant une gomme ?
            bool isPacmanEating = false;
            if (pTileX >= 0 && pTileX < GameConfig.MapWidth && pTileY >= 0 && pTileY < GameConfig.MapHeight)
            {
                int tile = levelMap.Grid[pTileY, pTileX];
                isPacmanEating = (tile == 2 || tile == 3);
            }

            // Sélection de la vitesse selon la matrice officielle
            if (isFrightenedMode)
            {
                pacman.Speed = isPacmanEating ? currentSpecs.FrightenedPacmanDots : currentSpecs.FrightenedPacman;
            }
            else
            {
                pacman.Speed = isPacmanEating ? currentSpecs.PacmanDots : currentSpecs.PacmanNormal;
            }

            // Déplacement de Pac-Man (tu peux enlever currentLevel s'il utilise désormais pacman.Speed)
            pacman.Move(levelMap, currentLevel, isFrightenedMode);


            // --- 3. GESTION DU TIMING DE SORTIE DU NID ---
            houseTimer++;
            if (houseTimer == 60) ReleaseGhostFromHouse("Pinky");
            else if (houseTimer == 240) ReleaseGhostFromHouse("Inky");
            else if (houseTimer == 480) ReleaseGhostFromHouse("Clyde");


            // --- 4. CALCUL ET INJECTION DE LA VITESSE DES FANTÔMES ---
            foreach (Ghost ghost in ghosts)
            {
                int gTileX = (int)(ghost.X / GameConfig.TileSize);
                int gTileY = (int)(ghost.Y / GameConfig.TileSize);

                // Détection du tunnel latéral (Ligne 14 d'origine + 3 lignes de HUD = Ligne 17)
                // Les tunnels ralentissent fortement les fantômes en arcade
                bool isInTunnel = (gTileY == 17 && (gTileX <= 5 || gTileX >= 22));

                if (ghost.CurrentState == GhostState.Frightened)
                {
                    ghost.Speed = currentSpecs.FrightenedGhost;
                }
                else if (isInTunnel)
                {
                    ghost.Speed = currentSpecs.GhostTunnel;
                }
                else
                {
                    ghost.Speed = currentSpecs.GhostNormal;
                }

                ghost.Move(levelMap, pacman, currentLevel);
            }

            // --- 5. ANIMATIONS, COLLISIONS ET RENDU ---
            frameCounter++;
            if (frameCounter % 6 == 0)
            {
                ghostAnimationFrame = (ghostAnimationFrame + 1) % 2;
            }

            CheckPelletCollision();
            CheckGhostCollisions();

            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 1. DESSIN DU LABYRINTHE ET DES GOMMES PAR TUILES
            // Nouvelle configuration des correspondances :
            // 0 = Lignes 0-2 (Rouge)
            // 3 = Lignes 3-5 (Bleu) -> Couleur d'origine du Niveau 1
            // 6 = Lignes 6-8 (Blanc)
            int colorRowOffset = 3; // Initialisé à 3 pour avoir le Bleu d'origine au départ !

            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    int screenX = c * GameConfig.ScaledTileSize;
                    int screenY = r * GameConfig.ScaledTileSize;
                    int size = GameConfig.ScaledTileSize;

                    // A. Dessiner le mur s'il y a une tuile valide programmée
                    if (levelMap.VisualGrid[r, c] != -1)
                    {
                        int tileID = levelMap.VisualGrid[r, c];
                        int sheetCol = tileID % 16;
                        int sheetRow = (tileID / 16) + colorRowOffset; // On applique le décalage de couleur !

                        using (Bitmap tileSprite = spriteManager.GetMazeTile(sheetCol, sheetRow))
                        {
                            g.DrawImage(tileSprite, screenX, screenY, size, size);
                        }
                    }

                    // B. Dessiner les Pac-gommes d'après la grille physique
                    int cellPhysic = levelMap.Grid[r, c];
                    if (cellPhysic == 2) // Pac-gomme classique
                    {
                        // Dans maze.png, la gomme classique est à la Colonne 13, Ligne 2 de sa palette
                        using (Bitmap dotSprite = spriteManager.GetMazeTile(13, 2 + colorRowOffset))
                        {
                            g.DrawImage(dotSprite, screenX, screenY, size, size);
                        }
                    }
                    else if (cellPhysic == 3) // Super Pac-gomme (Clignotante)
                    {
                        // Effet de clignotement rétro (visible 200ms sur 400ms)
                        if ((Environment.TickCount / 200) % 2 == 0)
                        {
                            // La super gomme est à la Colonne 15, Ligne 2
                            using (Bitmap energizerSprite = spriteManager.GetMazeTile(15, 2 + colorRowOffset))
                            {
                                g.DrawImage(energizerSprite, screenX, screenY, size, size);
                            }
                        }
                    }

                    // C. DESSIN DE LA PORTE DU NID DES FANTÔMES
                    if (levelMap.Grid[r, c] == 4 && c == 13 && !isLevelEndingFlash)
                    {
                        Bitmap door = spriteManager.GetGhostDoorSprite();
                        g.DrawImage(door, screenX, screenY, 16 * GameConfig.Zoom, 8 * GameConfig.Zoom);
                    }
                }
            }

            
            
            // 2. Dessiner Pac-Man (avec correction de l'alignement -4px avant Zoom)
            if (pacman != null)
            {
                // CORRECTIF : On retire 4 pixels à X et Y pour centrer le sprite de 16px sur la case de 8px
                int screenX = (int)((pacman.X - 4) * GameConfig.Zoom);
                int screenY = (int)((pacman.Y - 4) * GameConfig.Zoom);
                int spriteSize = 16 * GameConfig.Zoom;
                if (pacman.State == PacmanState.Alive)
                {
                    // On passe maintenant l'état de l'animation et de la direction actuelle
                    Bitmap pacmanSprite = spriteManager.GetPacmanSprite(pacman.AnimFrame, pacman.CurrentDirection);              
                    g.DrawImage(pacmanSprite, screenX, screenY, spriteSize, spriteSize);
                }
                else if (pacman.State == PacmanState.Dying)
                {
                    // Dessin de l'animation de mort
                    Bitmap deathSprite = spriteManager.GetPacmanDeathSprite(pacman.DeathAnimationFrame);
                    g.DrawImage(deathSprite, screenX, screenY, spriteSize, spriteSize);
                }
            }

            // En arcade, les fantômes disparaissent dès que l'animation de mort commence
            if (pacman.State == PacmanState.Alive)
            {
                foreach (Ghost ghost in ghosts)
                {
                    Bitmap ghostSprite;

                    // --- DIRECTION DE L'AIGUILLAGE VISUEL SELON L'ÉTAT ---
                    if (ghost.CurrentState == GhostState.Frightened)
                    {
                        // Le fantôme est effrayé -> Sprite bleu de panique
                        bool shouldFlashWhite = isFrightenedMode &&
                            (frightenedTicksRemaining < 120) &&
                            ((frightenedTicksRemaining / 8) % 2 == 0);

                        // On passe la variable dynamique ici au lieu de 'false'
                        ghostSprite = spriteManager.GetGhostFrightenedSprite(ghostAnimationFrame, shouldFlashWhite);
                    }
                    else if (ghost.CurrentState == GhostState.Eaten)
                    {
                        // Le fantôme a été mangé -> On n'affiche que ses yeux
                        ghostSprite = spriteManager.GetGhostEyesSprite(ghost.CurrentDirection);
                    }
                    else
                    {
                        // État normal (Scatter, Chase, InHouse, Exiting) -> Look d'origine
                        string ghostName = ghost.GetType().Name;
                        ghostSprite = spriteManager.GetGhostSprite(ghostName, ghost.CurrentDirection, ghostAnimationFrame);
                    }

                    // Calcul des coordonnées à l'écran (identique à ton code)
                    int bx = (int)((ghost.X - 4) * GameConfig.Zoom);
                    int by = (int)((ghost.Y - 4) * GameConfig.Zoom);

                    if (ghost.CurrentState == GhostState.InHouse)
                    {
                        by += 1;
                    }
                    int spriteScaledSize = 16 * GameConfig.Zoom;

                    // Dessin du sprite sélectionné
                    g.DrawImage(ghostSprite, bx, by, spriteScaledSize, spriteScaledSize);
                }
            }
        }

        private void CountInitialPellets()
        {
            dotsRemaining = 0;
            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    int tile = levelMap.Grid[r, c];
                    if (tile == 2 || tile == 3)
                    {
                        dotsRemaining++;
                    }
                }
            }
        }

        private void CheckPelletCollision()
        {
            // Convertir les coordonnées de pixels de Pac-Man en index de grille (tuile 8x8)
            int tileX = (int)(pacman.X / GameConfig.TileSize);
            int tileY = (int)(pacman.Y / GameConfig.TileSize);

            // Sécurité pour éviter de sortir des limites du tableau (ex: dans les tunnels)
            if (tileX >= 0 && tileX < GameConfig.MapWidth && tileY >= 0 && tileY < GameConfig.MapHeight)
            {
                int currentTile = levelMap.Grid[tileY, tileX];

                if (currentTile == 2) // Pac-gomme normale
                {
                    levelMap.Grid[tileY, tileX] = 0; // On vide la case de la grille physique
                    score += 10;                     // +10 points en arcade
                    dotsRemaining--;

                    UpdateGameTitleScore();          // Met à jour l'affichage de debug
                    CheckLevelClear();
                }
                else if (currentTile == 3) // Super Pac-gomme (Energizer)
                {
                    levelMap.Grid[tileY, tileX] = 0; // On vide la case
                    score += 50;                     // +50 points
                    dotsRemaining--;

                    UpdateGameTitleScore();

                    // --- DÉCLENCHEMENT DU MODE FRIGHTENED VIA SPECS ---
                    // Si le niveau autorise la panique (temps > 0)
                    if (currentSpecs.FrightenedTimeSec > 0)
                    {
                        isFrightenedMode = true;
                        // Conversion des secondes en nombre de frames (ex: 6 sec * 60 fps = 360 ticks)
                        frightenedTicksRemaining = currentSpecs.FrightenedTimeSec * 60;

                        // On force tous les fantômes qui ne sont pas dans le nid ou déjà mangés à paniquer
                        foreach (Ghost ghost in ghosts)
                        {
                            if (ghost.CurrentState == GhostState.Chase || ghost.CurrentState == GhostState.Scatter)
                            {
                                ghost.CurrentState = GhostState.Frightened;
                                // En arcade, un changement d'état force aussi un demi-tour immédiat !
                                ghost.ForceDemiTour();
                            }
                            else if (ghost.CurrentState == GhostState.Frightened)
                            {
                                // Si une autre super-gomme est mangée alors qu'ils sont déjà bleus,
                                // l'arcade originale veut qu'ils fassent à nouveau un demi-tour !
                                ghost.ForceDemiTour();
                            }
                        }
                    }
                    else
                    {
                        // Aux niveaux 17, 19, 20+, les fantômes ne font qu'un demi-tour sans paniquer !
                        foreach (Ghost ghost in ghosts)
                        {
                            ghost.ForceDemiTour();
                        }
                    }

                    CheckLevelClear();
                }
            }
        }

        // Petite fonction utilitaire pour voir ton score grimper en temps réel dans la barre de la fenêtre
        private void UpdateGameTitleScore()
        {
            this.Text = $"Pac-Man Arcade (C#) - SCORE: {score} - VIES: {lives} - Gommes: {dotsRemaining}";
        }

        private void CheckLevelClear()
        {
            if (dotsRemaining <= 0)
            {
                gameTimer.Stop(); // Arrête le jeu
                MessageBox.Show($"Félicitations ! Vous avez nettoyé le labyrinthe !\nScore final : {score}", "Victoire !");
                Application.Exit();
            }
        }

        private void SpawnPacman()
        {
            // 1. On consomme une tentative pour la mettre en jeu
            lives--;

            // 2. On actualise immédiatement l'affichage (qui va passer de 3 à 2 au premier spawn)
            UpdateGameTitleScore();

            // 3. Positionnement initial de Pac-Man sur la grille (+3 pour le HUD)
            pacman.X = 13 * GameConfig.TileSize;
            pacman.Y = (23 + 3) * GameConfig.TileSize; 
            pacman.CurrentDirection = Keys.Left;
            pacman.NextDirection = Keys.None;
            pacman.State=PacmanState.Alive;
        }

        private void ResetPositions()
        {
            this.currentSpecs = GameConfig.GetSpecsForLevel(this.currentLevel);
            ghosts.Clear();
            houseTimer = 0;

            // L'ordre d'instanciation est important car Inky a besoin de Blinky !
            blinky = new Blinky();
            Pinky pinky = new Pinky();
            Inky inky = new Inky(blinky);
            Clyde clyde = new Clyde();

            ghosts.Add(blinky);
            ghosts.Add(pinky);
            ghosts.Add(inky);
            ghosts.Add(clyde);

            SpawnPacman();
        }

        private void CheckGhostCollisions()
        {
            // On passe en revue tous les fantômes présents dans le jeu
            foreach (Ghost ghost in ghosts)
            {
                // Calcul de la distance absolue en pixels d'origine (0-8 px)
                float distanceX = Math.Abs(pacman.X - ghost.X);
                float distanceY = Math.Abs(pacman.Y - ghost.Y);

                // Seuil de collision : moins de 6 pixels de distance
                if (distanceX < 6 && distanceY < 6)
                {
                    if (ghost.CurrentState == GhostState.Frightened)
                    {
                        // --- CAS 1 : LE FANTÔME EST EFFRAYÉ ---
                        ghost.CurrentState = GhostState.Eaten;
                        score += 200; // En arcade : 200, puis 400, 800, 1600 par cycle
                        UpdateGameTitleScore();
                        // TODO: Jouer le son de capture et geler brièvement le jeu
                    }
                    else if (ghost.CurrentState == GhostState.Scatter || ghost.CurrentState == GhostState.Chase)
                    {
                        // --- CAS 2 : LE FANTÔME EST DANGEREUX ---
                        StartDeathSequence();
                        break; // On sort de la boucle immédiatement
                    }
                    // Si le fantôme est déjà à l'état 'Eaten' (les yeux seuls), il ne se passe rien !
                }
            }
        }

        private void StartDeathSequence()
        {
            // 1. On fige le moteur de jeu (plus de mouvements, plus de gommes mangées)
            gameTimer.Stop();

            // 2. On change l'état de Pac-Man pour le dessin
            pacman.State = PacmanState.Dying;
            pacman.DeathAnimationFrame = 0;

            // 3. Pause historique : En arcade, le jeu se fige 1 seconde avant que Pac-Man ne tourne.
            // Nous simulons cela par un petit timer de pause (ou Task.Delay plus simplement)
            System.Threading.Thread.Sleep(1000);

            // 4. On lance l'animation cinématique
            deathAnimationTimer.Start();
        }

        private void DeathAnimationTimer_Tick(object sender, EventArgs e)
        {
            pacman.DeathAnimationFrame++;

            // Forcer le redessin de l'écran pour voir la frame suivante
            this.Invalidate();

            // Si l'animation est terminée (12 frames)
            if (pacman.DeathAnimationFrame >= 12)
            {
                deathAnimationTimer.Stop();

                // Pause historique après la mort (Pac-Man a disparu)
                System.Threading.Thread.Sleep(500);

                // On passe à la gestion administrative de la vie perdue
                CompleteLifeLostSequence();
            }
        }

        private void CompleteLifeLostSequence()
        {
            // (Cette méthode contient ton ancienne logique administrative)
            if (lives <= 0)
            {
                MessageBox.Show($"GAME OVER\nScore final : {score}", "Fin de partie");
                Application.Exit();
            }
            else
            {
                // En arcade, le message "READY!" s'affiche ici
                ResetPositions();

                // On remet Pac-Man en vie AVANT de relancer le timer de jeu
                pacman.State = PacmanState.Alive;
                gameTimer.Start();
            }
        }

        private void ReleaseGhostFromHouse(string ghostType)
        {
            foreach (Ghost ghost in ghosts)
            {
                // On cherche le bon fantôme s'il est encore dans la maison
                if (ghost.GetType().Name == ghostType && ghost.CurrentState == GhostState.InHouse)
                {
                    ghost.CurrentState = GhostState.Exiting;
                    break; // On a trouvé, on arrête la boucle
                }
            }
        }
    }
}