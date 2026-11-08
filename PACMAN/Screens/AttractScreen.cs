using System.Drawing;
using static PacmanWinForms.SpriteManager;

namespace PacmanWinForms
{
    /// <summary>
    /// Attract mode. Pour l'instant : la présentation des personnages
    /// (la chasse puis la démo viendront aux étapes suivantes).
    /// </summary>
    public sealed class AttractScreen : IScreen
    {
        // Chronologie en frames depuis le début de l'écran (mesurée sur une vidéo de l'arcade à 60,6 images/s)
        private const int TitleFrame = 2;
        private static readonly int[] GhostRevealFrames = { 58, 178, 298, 418 };
        private const int NameDelay = 60;           // Le nom apparaît 1 s après le sprite
        private const int NicknameDelay = 90;       // Le surnom apparaît 0,5 s après le nom
        private const int LegendFrame = 568;        // "10 PTS" et "50 PTS"
        private const int LogoFrame = 628;          // Logo namco et super-gomme de gauche
        private const int BlinkStartFrame = 697;    // Les super-gommes se mettent à clignoter

        // Provisoire : la chasse et la démo ne sont pas encore là, la présentation recommence en boucle
        private const int CycleLength = 1212;

        private static readonly GhostType[] Ghosts = { GhostType.Blinky, GhostType.Pinky, GhostType.Inky, GhostType.Clyde };
        private static readonly ArcadeColor[] GhostColors = { ArcadeColor.Red, ArcadeColor.Pink, ArcadeColor.Cyan, ArcadeColor.Orange };

        private readonly ScreenManager manager;
        private int frame;

        public AttractScreen(ScreenManager manager)
        {
            this.manager = manager;
        }

        public void Enter()
        {
            frame = 0;
        }

        public void Update()
        {
            frame++;
            if (frame >= CycleLength)
            {
                frame = 0;
            }
        }

        public void OnInput(InputKey key)
        {
            // Rien en attract mode : l'insertion d'un crédit est gérée par le ScreenManager
        }

        public void Draw(GameRenderer renderer, Graphics g)
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
                // La super-gomme de la chasse (à gauche) et le logo
                if (energizerOn)
                {
                    GameRenderer.DrawSprite(g, renderer.Sprites.GetEnergizerSprite(), 4 * tile, 20 * tile);
                }
                GameRenderer.DrawSprite(g, renderer.Sprites.GetNamcoLogo(ArcadeColor.Pink), 11 * tile, 31 * tile);
            }
        }

        // Les quatre fantômes, un à la fois : sprite, puis nom, puis surnom
        private void DrawGhosts(GameRenderer renderer, Graphics g)
        {
            int tile = GameConfig.TileSize;

            for (int i = 0; i < Ghosts.Length; i++)
            {
                int start = GhostRevealFrames[i];
                if (frame < start) break; // Les suivants ne sont pas encore là

                int row = 7 + 3 * i;

                // Sprite fixe, regardant vers la droite, centré sur la ligne de texte
                Bitmap sprite = renderer.Sprites.GetGhostSprite(Ghosts[i], Direction.Right, 0);
                GameRenderer.DrawSprite(g, sprite, 4 * tile, row * tile - 4);

                string name = ArcadeTexts.GhostNames[i];

                if (frame >= start + NameDelay)
                {
                    renderer.DrawText(g, name, 8, row, GhostColors[i]);
                }

                if (frame >= start + NicknameDelay)
                {
                    renderer.DrawText(g, ArcadeTexts.GhostNicknames[i], 8 + name.Length, row, GhostColors[i]);
                }
            }
        }
    }
}