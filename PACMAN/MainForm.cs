using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public partial class MainForm : Form
    {
        private SpriteManager spriteManager;
        private Bitmap backgroundMaze;
        private Timer gameTimer;

        private LevelMap levelMap;
        private Pacman pacman;

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

            // --- CONFIGURATION DU TILE ENGINE VIA FICHIER DATA ---
            // Le fichier est maintenant inclus dans le projet et déployé par Visual Studio !
            string mapPath = Path.Combine(Application.StartupPath, "asset", "maps", "original_maze.txt");

            if (File.Exists(mapPath))
            {
                levelMap.LoadMapFromFile(mapPath);
            }
            else
            {
                // Sécurité au cas où le fichier est manquant au runtime
                MessageBox.Show("Fichier de carte introuvable : " + mapPath, "Erreur Critique", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            // -----------------------------------------------------

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
            // Mettre à jour la position de Pac-Man à chaque tick
            pacman.Move(levelMap);

            // Redessiner l'écran
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
                }
            }

            // 2. Dessiner Pac-Man (Reste inchangé)
            if (pacman != null)
            {
                // ... (Ton code de rendu Pac-Man existant)

                // 2. Dessiner Pac-Man (avec correction de l'alignement -4px avant Zoom)
                if (pacman != null)
                {
                    // On passe maintenant l'état de l'animation et de la direction actuelle
                    Bitmap pacmanSprite = spriteManager.GetPacmanSprite(pacman.AnimFrame, pacman.CurrentDirection);

                    // CORRECTIF : On retire 4 pixels à X et Y pour centrer le sprite de 16px sur la case de 8px
                    int screenX = (int)((pacman.X - 4) * GameConfig.Zoom);
                    int screenY = (int)((pacman.Y - 4) * GameConfig.Zoom);
                    int spriteSize = 16 * GameConfig.Zoom;

                    g.DrawImage(pacmanSprite, screenX, screenY, spriteSize, spriteSize);
                }
            }
        }
    }
}