using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static PacmanWinForms.SpriteManager;

namespace PacmanWinForms
{
    public partial class MainForm : Form
    {
        private SpriteManager spriteManager;
        private Timer gameTimer;
        private Timer deathAnimationTimer;
        private Stopwatch gameStopwatch = new Stopwatch();
        private double accumulatedTimeMs = 0;

        // NOTRE MOTEUR DE JEU UNIQUE
        private Game game;

        public MainForm()
        {
            try
            {
                this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
                this.ShowIcon = true;
            }
            catch { /* Sécurité */ }
            this.Text = "Pac-Man Arcade (C#)";
            this.Size = new Size(GameConfig.ScreenWidth + 16, GameConfig.ScreenHeight + 39);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyDown += new KeyEventHandler(OnKeyDown);

            spriteManager = new SpriteManager();

            try
            {
                // Instanciation de la logique pure
                game = new Game();
                UpdateGameTitleScore();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erreur Critique", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            deathAnimationTimer = new Timer();
            deathAnimationTimer.Interval = 200;
            deathAnimationTimer.Tick += DeathAnimationTimer_Tick;

            gameTimer = new Timer();
            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;

            gameStopwatch.Start();
            gameTimer.Start();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            Direction direction = KeyToDirection(e.KeyCode);
            if (direction != Direction.None)
            {
                game.HandleInput(direction);
            }
        }

        private static Direction KeyToDirection(Keys key)
        {
            switch (key)
            {
                case Keys.Up: return Direction.Up;
                case Keys.Down: return Direction.Down;
                case Keys.Left: return Direction.Left;
                case Keys.Right: return Direction.Right;
                default: return Direction.None;
            }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            long elapsedMs = gameStopwatch.ElapsedMilliseconds;
            gameStopwatch.Restart();

            if (elapsedMs > 100) elapsedMs = 100;
            accumulatedTimeMs += elapsedMs;

            const double TargetFrameTime = 16.67;

            while (accumulatedTimeMs >= TargetFrameTime)
            {
                game.Update();
                accumulatedTimeMs -= TargetFrameTime;

                // INTERCEPTION DES CHANGEMENTS D'ÉTAT DU MOTEUR
                if (game.State == GameState.Dying)
                {
                    HandleDeathTransition();
                    return; // On sort immédiatement de la boucle
                }
                else if (game.State == GameState.Victory)
                {
                    // On force le rafraîchissement pour voir le labyrinthe vide
                    this.Refresh();

                    // Pause de satisfaction d'arcade (2 secondes) avant le niveau suivant
                    System.Threading.Thread.Sleep(2000);

                    // On appelle notre nouvelle méthode au lieu de quitter !
                    game.StartNextLevel();
                    UpdateGameTitleScore();
                    return;
                }
            }

            UpdateGameTitleScore();
            this.Invalidate();
        }

        private void HandleDeathTransition()
        {
            gameTimer.Stop();
            UpdateGameTitleScore();
            this.Invalidate();

            // Pause historique : On fige le thread graphique 1 seconde avant l'animation
            System.Threading.Thread.Sleep(1000);
            deathAnimationTimer.Start();
        }

        private void DeathAnimationTimer_Tick(object sender, EventArgs e)
        {
            game.Pacman.DeathAnimationFrame++;
            this.Invalidate();

            // L'animation de mort (12 frames) est terminée
            if (game.Pacman.DeathAnimationFrame >= 12)
            {
                deathAnimationTimer.Stop();

                System.Threading.Thread.Sleep(500); // Pause historique après la disparition de Pac-Man

                // Le moteur vérifie les vies :
                // - S'il reste des vies -> passe en GameState.Ready et réinitialise les positions
                // - S'il n'y a plus de vies -> passe en GameState.GameOver et active le timer de 3s (180 frames)
                game.PrepareNextLifeOrGameOver();

                // Dans les deux cas, on remet l'état de Pac-Man à Vivant pour la suite
                game.SetPacmanState(PacmanState.Alive);

                // On rafraîchit la barre de titre (pour voir instantanément la perte d'une vie)
                UpdateGameTitleScore();

                // CRITIQUE : On relance TOUJOURS la boucle de jeu principale (60 Hz).
                // C'est elle qui va faire défiler soit le "READY!" (si vivant), soit le "GAME OVER" (si mort).
                gameTimer.Start();
            }
        }

        private void UpdateGameTitleScore()
        {
            this.Text = $"Pac-Man Arcade (C#) - SCORE: {game.Score} - VIES: {game.Lives} - Gommes: {game.DotsRemaining}";
        }

        private void DrawArcadeString(Graphics g, string text, int startTileX, int tileY, ArcadeColor colorSet)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ') continue; // On ne dessine rien pour un espace

                using (Bitmap charSprite = spriteManager.GetArcadeCharacter(c, colorSet))
                {
                    int posX = (startTileX + i) * GameConfig.ScaledTileSize;
                    int posY = tileY * GameConfig.ScaledTileSize;
                    int size = GameConfig.ScaledTileSize;
                    g.DrawImage(charSprite, posX, posY, size, size);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 1. DESSIN DU LABYRINTHE (via game.Map)
            for (int r = 0; r < GameConfig.MapHeight; r++)
            {
                for (int c = 0; c < GameConfig.MapWidth; c++)
                {
                    int screenX = c * GameConfig.ScaledTileSize;
                    int screenY = r * GameConfig.ScaledTileSize;
                    int size = GameConfig.ScaledTileSize;

                    if (game.Map.VisualGrid[r, c] != -1)
                    {
                        int tileID = game.Map.VisualGrid[r, c];
                        int sheetCol = tileID % 16;
                        int sheetRow = (tileID / 16) ;

                        using (Bitmap tileSprite = spriteManager.GetMazeTile(sheetCol, sheetRow))
                        {
                            g.DrawImage(tileSprite, screenX, screenY, size, size);
                        }
                    }

                    TileType cellPhysic = game.Map.Grid[r, c];
                    if (cellPhysic == TileType.Dot)
                    {
                        using (Bitmap dotSprite = spriteManager.GetMazeTile(13, 2 ))
                        {
                            g.DrawImage(dotSprite, screenX, screenY, size, size);
                        }
                    }
                    else if (cellPhysic == TileType.Energizer)
                    {
                        if ((Environment.TickCount / 200) % 2 == 0)
                        {
                            using (Bitmap energizerSprite = spriteManager.GetMazeTile(15, 2 ))
                            {
                                g.DrawImage(energizerSprite, screenX, screenY, size, size);
                            }
                        }
                    }

                    if (game.Map.Grid[r, c] == TileType.GhostDoor && c == 13)
                    {
                        Bitmap door = spriteManager.GetGhostDoorSprite();
                        g.DrawImage(door, screenX, screenY, 16 * GameConfig.Zoom, 8 * GameConfig.Zoom);
                    }
                }
            }

            // 2. Dessiner Pac-Man et les Fantômes uniquement s'ils doivent être visibles
            if (game.ShowCharacters && game.State != GameState.GameOver)
            {
                // --- BLOC DESSIN DE PAC-MAN ---
                // REGLE ARCADE : Pac-Man disparaît complètement pendant le freeze de point !
                if (game.Pacman != null && game.State != GameState.GhostEaten)
                {
                    int screenX = (int)((game.Pacman.X - 4) * GameConfig.Zoom);
                    int screenY = (int)((game.Pacman.Y - 4) * GameConfig.Zoom);
                    int spriteSize = 16 * GameConfig.Zoom;

                    if (game.Pacman.State == PacmanState.Alive)
                    {
                        int framePourDessin = (game.State == GameState.Ready) ? 3 : game.Pacman.AnimFrame;

                        Bitmap pacmanSprite = spriteManager.GetPacmanSprite(framePourDessin, game.Pacman.CurrentDirection);
                        g.DrawImage(pacmanSprite, screenX, screenY, spriteSize, spriteSize);
                    }
                    else if (game.Pacman.State == PacmanState.Dying)
                    {
                        Bitmap deathSprite = spriteManager.GetPacmanDeathSprite(game.Pacman.DeathAnimationFrame);
                        g.DrawImage(deathSprite, screenX, screenY, spriteSize, spriteSize);
                    }
                }

                // --- BLOC DESSIN DES FANTÔMES ---
                if (game.Pacman.State == PacmanState.Alive)
                {
                    foreach (Ghost ghost in game.Ghosts)
                    {
                        // REGLE ARCADE : On ne dessine pas le fantôme qui vient d'être mangé (le score prend sa place)
                        if (game.State == GameState.GhostEaten && ghost == game.JustEatenGhost)
                        {
                            continue;
                        }

                        Bitmap ghostSprite;

                        if (ghost.Phase == GhostPhase.Eaten)
                        {
                            ghostSprite = spriteManager.GetGhostEyesSprite(ghost.CurrentDirection);
                        }
                        else if (ghost.IsFrightened)
                        {
                            int virtualTicksRemaining = game.FrightenedFramesRemaining;
                            const int TicksPerFlashCycle = 16;
                            int totalFlashTicks = game.CurrentSpecs.FlashesCount * TicksPerFlashCycle;

                            bool shouldFlashWhite = game.IsFrightenedMode &&
                                (virtualTicksRemaining < totalFlashTicks) &&
                                ((virtualTicksRemaining / 8) % 2 == 0);

                            ghostSprite = spriteManager.GetGhostFrightenedSprite(game.GhostAnimationFrame, shouldFlashWhite);
                        }
                        else
                        {
                            string ghostName = ghost.GetType().Name;
                            ghostSprite = spriteManager.GetGhostSprite(ghostName, ghost.CurrentDirection, game.GhostAnimationFrame);
                        }

                        int bx = (int)((ghost.X - 4) * GameConfig.Zoom);
                        int by = (int)((ghost.Y - 4) * GameConfig.Zoom);

                        if (ghost.Phase == GhostPhase.InHouse) by += 1;
                        int spriteScaledSize = 16 * GameConfig.Zoom;

                        g.DrawImage(ghostSprite, bx, by, spriteScaledSize, spriteScaledSize);
                    }
                }
            }

            // 3. TEXTE "READY!" RETRO (Dessiné lettre par lettre via text.png)
            if (game.State == GameState.Ready)
            {
                DrawArcadeString(g, "READY!", 11, 20, ArcadeColor.Yellow);
            }

            // 4. TEXTE "GAME OVER" RETRO (Dessiné lettre par lettre en Rouge via text.png)
            if (game.State == GameState.GameOver)
            {
                DrawArcadeString(g, "GAME  OVER", 9, 20, ArcadeColor.Red);
            }

            // 5. TEXTE DU SCORE DU FANTÔME MANGÉ 
            if (game.State == GameState.GhostEaten)
            {
                int pointsGagnes = game.EatenGhostScoreValue; // (200, 400, 800 ou 1600)

                using (Bitmap pointsSprite = spriteManager.GetGhostPointsSprite(pointsGagnes))
                {
                    // On centre le sprite de 16x16 à la position où le fantôme a été capturé
                    int posX = game.EatenGhostScoreX * GameConfig.Zoom;
                    int posY = game.EatenGhostScoreY * GameConfig.Zoom;

                    // Grâce à notre refactoring précédent, ce sprite en provenance de generalSheet
                    // profite AUTOMATIQUEMENT de la transparence sur le fond noir !
                    g.DrawImage(pointsSprite, posX, posY, GameConfig.ScaledTileSize * 2, GameConfig.ScaledTileSize * 2);
                }

                
            }

            // 6. DESSIN DU FRUIT BONUS
            if (game.Fruit.IsActive)
            {
                using (Bitmap fruitSprite = spriteManager.GetFruitSprite(game.CurrentLevel))
                {
                    int posX = game.Fruit.X * GameConfig.Zoom;
                    int posY = game.Fruit.Y * GameConfig.Zoom;

                    // Dessin du fruit (Taille 16x16 zoomé, hérite de la transparence noire automatiquement !)
                    g.DrawImage(fruitSprite, posX, posY, GameConfig.ScaledTileSize * 2, GameConfig.ScaledTileSize * 2);
                }
            }

            // 7. DESSIN DES POINTS DU FRUIT MANGÉ ---
            if (game.Fruit.IsScoreVisible)
            {
                using (Bitmap pointsSprite = spriteManager.GetFruitPointsSprite(game.Fruit.Value))
                {
                    int posX = game.Fruit.X * GameConfig.Zoom;
                    int posY = game.Fruit.Y * GameConfig.Zoom;

                    // Si le score est de 2000 ou plus, le sprite extrait commence 3 pixels plus à gauche.
                    // On décale le dessin de -3 pixels (au niveau du zoom) pour que la bordure droite
                    // reste parfaitement alignée avec celle du score 1000 !
                    if (game.Fruit.Value >= 2000)
                    {
                        posX -= 3 * GameConfig.Zoom;
                    }

                    // Dessin en utilisant la largeur RÉELLE (pointsSprite.Width) pour éviter tout écrasement
                    g.DrawImage(pointsSprite, posX, posY, pointsSprite.Width * GameConfig.Zoom, pointsSprite.Height * GameConfig.Zoom);
                }
            }

            // =========================================================================
            // DESSIN DU HUD RETRO (BANDEAUX SUPÉRIEUR ET INFÉRIEUR)
            // =========================================================================



            // --- LIGNE 0 : BANDEAU SUPÉRIEUR (Textes fixes et clignotement) ---

            // Gestion du clignotement réglementaire du "1UP" (uniquement en cours de partie)
            bool show1Up = true;
            if (game.State == GameState.Playing)
            {
                show1Up = (Environment.TickCount / 220) % 2 == 0; // Clignote toutes les ~220ms
            }

            if (show1Up)
            {
                DrawArcadeString(g, "1UP", 3, 0, ArcadeColor.White);
            }

            // Inscription centrale HIGH SCORE
            DrawArcadeString(g, "HIGH SCORE", 9, 0, ArcadeColor.White);


            // --- LIGNE 1 : SCORES ALIGNÉS À DROITE ---

            // Score Joueur 1 : S'affiche "00" si à zéro, sinon recule vers la gauche
            string scoreStr = (game.Score == 0) ? "00" : game.Score.ToString();
            int scoreStartX = 6 - scoreStr.Length + 1; // Aligné à droite sur la colonne 6
            DrawArcadeString(g, scoreStr, scoreStartX, 1, ArcadeColor.White);

            // CORRECTION HIGH SCORE : Si la borne vient d'être allumée (HighScore == 0),
            // la zone reste vide. Elle s'affichera dès que le joueur marque ses premiers points.
            if (game.HighScore > 0)
            {
                string highScoreStr = game.HighScore.ToString();
                int highScoreStartX = 16 - highScoreStr.Length + 1; // Aligné à droite sur la colonne 16
                DrawArcadeString(g, highScoreStr, highScoreStartX, 1, ArcadeColor.White);
            }


            // --- LIGNE 34 : RÉSERVE DE VIES (En bas à gauche) ---

            // LOGIQUE DE TRANSITION :
            // - Au début du jeu ou après une mort : Tant que Pac-Man est caché, on montre tout le stock (ex: 3),
            //   puis dès qu'il apparaît, le stock en bas diminue de 1 (ex: 2).
            // - Lors d'un changement de niveau (IsNextLevelTransition == true) : Pac-Man n'est pas mort,
            //   on force donc le maintien du compteur à sa valeur normale pour éviter l'effet de fluctuation (+1 / -1).
            bool subtractLifeIcon = game.ShowCharacters || game.IsNextLevelTransition;
            int livesToDraw = subtractLifeIcon ? game.Lives - 1 : game.Lives;
            int maxVisibleLives = Math.Min(livesToDraw, 5);

            for (int i = 0; i < maxVisibleLives; i++)
            {
                using (Bitmap lifeSprite = spriteManager.GetPacmanSprite(1, Direction.Left))
                {
                    int posX = (2 + (i * 2)) * GameConfig.ScaledTileSize;
                    int posY = 34 * GameConfig.ScaledTileSize;
                    int spriteSize = 16 * GameConfig.Zoom;

                    g.DrawImage(lifeSprite, posX, posY, spriteSize, spriteSize);
                }
            }

            // --- LIGNE 34 : INDICATEUR DE NIVEAU (Fruits en bas à droite) ---

            // On affiche au maximum les 7 derniers niveaux joués
            int fruitsToDraw = Math.Min(game.CurrentLevel, 7);

            for (int i = 0; i < fruitsToDraw; i++)
            {
                // i = 0 correspond au niveau actuel (tout à droite)
                // i = 1 correspond au niveau précédent, etc.
                int targetLevel = game.CurrentLevel - i;

                using (Bitmap fruitIndicatorSprite = spriteManager.GetFruitSprite(targetLevel))
                {
                    // Colonne de départ tout à droite : 26 (la grille fait 28 colonnes, de 0 à 27).
                    // On recule de 2 cases (16 pixels) pour chaque fruit précédent (i).
                    int tileX = 26 - (i * 2);

                    int posX = tileX * GameConfig.ScaledTileSize;
                    int posY = 34 * GameConfig.ScaledTileSize; // Même ligne que les icônes de vie

                    // Dessin du fruit de l'indicateur (Taille 16x16 pixels à l'échelle)
                    g.DrawImage(fruitIndicatorSprite, posX, posY, GameConfig.ScaledTileSize * 2, GameConfig.ScaledTileSize * 2);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (gameTimer != null) gameTimer.Stop();
            if (game != null) game.Shutdown();

            base.OnFormClosed(e);
        }
    }
}