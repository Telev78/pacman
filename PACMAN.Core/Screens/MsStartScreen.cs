using static PacmanWinForms.SpriteManager;

namespace PacmanWinForms
{
    /// <summary>
    /// Écran "PUSH START BUTTON" de Ms Pac-Man (mise en page relevée sur des captures de l'arcade).
    /// </summary>
    public sealed class MsStartScreen : IStartScreen
    {
        private readonly ScreenManager manager;

        public MsStartScreen(ScreenManager manager)
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

        public void Draw(GameRenderer renderer, Canvas g)
        {
            int tile = GameConfig.TileSize;

            renderer.DrawTopHud(g, 0, manager.HighScore, show1Up: true, show2Up: true);

            renderer.DrawText(g, ArcadeTexts.PushStart, 6, 15, ArcadeColor.Orange);

            string players = (manager.Credits >= 2) ? ArcadeTexts.OneOrTwoPlayers : ArcadeTexts.OnePlayerOnly;
            renderer.DrawText(g, players, 8, 17, ArcadeColor.Orange);

            // "ADDITIONAL [Ms Pac-Man] AT 10000 PTS" : l'icône et "PTS" sont des sprites
            if (Settings.BonusLife > 0)
            {
                renderer.DrawText(g, "ADDITIONAL", 2, 24, ArcadeColor.Orange);
                GameRenderer.DrawSprite(g, renderer.Sprites.GetPacmanSprite(1, Direction.Right), 13 * tile, 23 * tile);
                renderer.DrawText(g, "AT " + Settings.BonusLife, 16, 24, ArcadeColor.Orange);
                GameRenderer.DrawSprite(g, renderer.Sprites.GetPtsSprite(ArcadeColor.Orange), 25 * tile, 24 * tile);
            }

            // Logo Midway et copyright
            GameRenderer.DrawSprite(g, renderer.Sprites.GetMidwayLogo(), 6 * tile, 28 * tile);
            renderer.DrawText(g, "\u00A9 MIDWAY MFG CO", 11, 29, ArcadeColor.Red);
            renderer.DrawText(g, "1980/1981", 14, 31, ArcadeColor.Red);

            renderer.DrawCredits(g, manager.Credits);
        }
    }
}
