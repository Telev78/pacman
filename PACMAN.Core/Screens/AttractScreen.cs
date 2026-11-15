using static PacmanWinForms.SpriteManager;

namespace PacmanWinForms
{
    /// <summary>
    /// Attract mode : présentation des personnages, puis scène de chasse, puis démo.
    /// </summary>
    public sealed class AttractScreen : IAttractScreen
    {
        // Chronologie en frames depuis le début de l'écran (mesurée sur une vidéo de l'arcade à 60,6 images/s)
        private const int TitleFrame = 2;
        private static readonly int[] GhostRevealFrames = { 58, 178, 298, 418 };
        private const int NameDelay = 60;           // Le nom apparaît 1 s après le sprite
        private const int NicknameDelay = 90;       // Le surnom apparaît 0,5 s après le nom
        private const int LegendFrame = 568;        // "10 PTS" et "50 PTS"
        private const int LogoFrame = 628;          // Logo namco et super-gomme de gauche
        private const int ChaseStartFrame = 694;    // Pac-Man entre par la droite
        private const int BlinkStartFrame = 697;    // Les super-gommes se mettent à clignoter

        private static readonly GhostType[] Ghosts = { GhostType.Blinky, GhostType.Pinky, GhostType.Inky, GhostType.Clyde };
        private static readonly ArcadeColor[] GhostColors = { ArcadeColor.Red, ArcadeColor.Pink, ArcadeColor.Cyan, ArcadeColor.Orange };

        private readonly ScreenManager manager;
        private int frame;
        private ChaseScene chase;

        public AttractScreen(ScreenManager manager)
        {
            this.manager = manager;
        }

        public void Enter()
        {
            frame = 0;
            chase = null;
        }

        public void Update()
        {
            frame++;

            if (frame == ChaseStartFrame)
            {
                chase = new ChaseScene();
            }

            if (chase != null)
            {
                chase.Update();

                // Fin de la scène de chasse : place à la démo
                if (chase.Finished)
                {
                    manager.ShowDemo();
                }
            }
        }

        public void OnInput(InputKey key)
        {
            // Rien en attract mode : l'insertion d'un crédit est gérée par le ScreenManager
        }

        public void Draw(GameRenderer renderer, Canvas g)
        {
            int tile = GameConfig.TileSize;

            renderer.DrawTopHud(g, 0, manager.HighScore, show1Up: true, show2Up: true);
            renderer.DrawCredits(g, manager.Credits);

            if (frame >= TitleFrame)
            {
                renderer.DrawText(g, ArcadeTexts.CharacterNickname, 7, 5, ArcadeColor.White);
            }

            DrawGhosts(renderer, g);

            // Les super-gommes sont fixes jusqu'au début de la chasse, puis elles clignotent (éteintes d'abord)
            bool energizerOn = frame < BlinkStartFrame || !GameRenderer.IsEnergizerOn(frame - BlinkStartFrame);

            if (frame >= LegendFrame)
            {
                // Une gomme normale : 10 points
                GameRenderer.DrawSprite(g, renderer.Sprites.GetDotSprite(), 10 * tile, 24 * tile);
                renderer.DrawText(g, ArcadeTexts.Points10, 12, 24, ArcadeColor.White);
                GameRenderer.DrawSprite(g, renderer.Sprites.GetPtsSprite(ArcadeColor.White), 15 * tile, 24 * tile);

                // Une super-gomme : 50 points
                if (energizerOn)
                {
                    GameRenderer.DrawSprite(g, renderer.Sprites.GetEnergizerSprite(), 10 * tile, 26 * tile);
                }
                renderer.DrawText(g, ArcadeTexts.Points50, 12, 26, ArcadeColor.White);
                GameRenderer.DrawSprite(g, renderer.Sprites.GetPtsSprite(ArcadeColor.White), 15 * tile, 26 * tile);
            }

            if (frame >= LogoFrame)
            {
                // La super-gomme de la chasse (à gauche), tant que Pac-Man ne l'a pas mangée
                bool energizerEaten = (chase != null && chase.EnergizerEaten);
                if (energizerOn && !energizerEaten)
                {
                    GameRenderer.DrawSprite(g, renderer.Sprites.GetEnergizerSprite(), 4 * tile, 20 * tile);
                }

                if (ArcadeTexts.IsUsa)
                    renderer.DrawText(g, ArcadeTexts.MidwayLine, 4, 31, ArcadeColor.Pink);
                else
                    GameRenderer.DrawSprite(g, renderer.Sprites.GetNamcoLogo(ArcadeColor.Pink), 11 * tile, 31 * tile);
            }

            if (chase != null)
            {
                chase.Draw(renderer, g);
            }
        }

        // Les quatre fantômes, un à la fois : sprite, puis nom, puis surnom
        private void DrawGhosts(GameRenderer renderer, Canvas g)
        {
            int tile = GameConfig.TileSize;

            for (int i = 0; i < Ghosts.Length; i++)
            {
                int start = GhostRevealFrames[i];
                if (frame < start) break; // Les suivants ne sont pas encore là

                int row = 7 + 3 * i;

                // Sprite fixe, regardant vers la droite, centré sur la ligne de texte
                Sprite sprite = renderer.Sprites.GetGhostSprite(Ghosts[i], Direction.Right, 0);
                GameRenderer.DrawSprite(g, sprite, 4 * tile, row * tile - 4);

                string name = ArcadeTexts.GhostNames[i];

                if (frame >= start + NameDelay)
                {
                    renderer.DrawText(g, name, ArcadeTexts.GhostNameColumn, row, GhostColors[i]);
                }

                if (frame >= start + NicknameDelay)
                {
                    renderer.DrawText(g, ArcadeTexts.GhostNicknames[i], ArcadeTexts.GhostNicknameColumn(i), row, GhostColors[i]);
                }
            }
        }
    }
}