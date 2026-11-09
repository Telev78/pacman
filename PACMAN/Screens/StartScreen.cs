using System.Drawing;
using static PacmanWinForms.SpriteManager;

namespace PacmanWinForms
{
    public sealed class StartScreen : IScreen
    {
        private readonly ScreenManager manager;

        public StartScreen(ScreenManager manager)
        {
            this.manager = manager;
        }

        public void Enter() { }

        public void Update() { }

        public void OnInput(InputKey key)
        {
            if (key == InputKey.Start1)
            {
                manager.StartGame(1);
            }
            else if (key == InputKey.Start2)
            {
                manager.StartGame(2);   // Sans effet s'il n'y a pas 2 crédits
            }
        }

        public void Draw(GameRenderer renderer, Graphics g)
        {
            int tile = GameConfig.TileSize;

            renderer.DrawTopHud(g, 0, manager.HighScore, show1Up: true, show2Up: true);

            renderer.DrawText(g, ArcadeTexts.PushStart, 6, 16, ArcadeColor.Orange);

            // Avec deux crédits ou plus, on peut choisir entre 1 et 2 joueurs
            string players = (manager.Credits >= 2) ? ArcadeTexts.OneOrTwoPlayers : ArcadeTexts.OnePlayerOnly;
            renderer.DrawText(g, players, 8, 20, ArcadeColor.Cyan);

            // "BONUS PUCKMAN FOR 10000 PTS" : le "PTS" est un sprite
            if (Settings.BonusLife > 0)
            {
                renderer.DrawText(g, ArcadeTexts.BonusLife, 1, 24, ArcadeColor.Flesh);
                GameRenderer.DrawSprite(g, renderer.Sprites.GetPtsSprite(ArcadeColor.Flesh), 25 * tile, 24 * tile);
            }

            if (ArcadeTexts.IsUsa)
            {
                renderer.DrawText(g, ArcadeTexts.MidwayLine, 4, 28, ArcadeColor.Pink);
            }
            else
            {
                renderer.DrawText(g, ArcadeTexts.Copyright, 8, 28, ArcadeColor.Pink);
                GameRenderer.DrawSprite(g, renderer.Sprites.GetNamcoLogo(ArcadeColor.Pink), 10 * tile, 28 * tile);
                renderer.DrawText(g, ArcadeTexts.Year, 18, 28, ArcadeColor.Pink);
            }

            renderer.DrawCredits(g, manager.Credits);
        }
    }
}