using System.Collections.Generic;
using static PacmanWinForms.SpriteManager;

namespace PacmanWinForms
{
    /// <summary>
    /// Menu de test : toutes les intermissions de tous les jeux. Haut / Bas pour choisir, 1 (ou 2) pour lancer,
    /// Échap pour revenir au choix du jeu.
    /// </summary>
    public sealed class IntermissionMenuScreen : IScreen
    {
        private const int FirstEntryRow = 8;
        private const int EntrySpacing = 2;
        private const int CursorColumn = 6;
        private const int NameColumn = 9;
        private const int AnimFramesPerStep = 6;
        private const int ActsPerGame = 3;

        // Niveau affiché dans les fruits du bas : le niveau qui précède réellement chaque intermission
        private static readonly int[] LevelForAct = { 2, 5, 9 };
        private static readonly int[] CursorFrames = { 1, 0, 3, 0 };

        private readonly ScreenManager manager;
        private readonly IReadOnlyList<GameVariant> games = GameVariants.All;
        private int selected;
        private int tick;

        public IntermissionMenuScreen(ScreenManager manager)
        {
            this.manager = manager;
        }

        // Une entrée par intermission, puis « BACK »
        private int EntryCount { get { return games.Count * ActsPerGame + 1; } }

        public void Enter()
        {
            tick = 0;
            selected = 0;
        }

        public void Update()
        {
            tick++;
        }

        public void OnInput(InputKey key)
        {
            switch (key)
            {
                case InputKey.Up:
                    selected = (selected + EntryCount - 1) % EntryCount;
                    break;
                case InputKey.Down:
                    selected = (selected + 1) % EntryCount;
                    break;
                case InputKey.Menu:
                    manager.ShowMenu();
                    break;
                case InputKey.Start1:
                case InputKey.Start2:
                    if (selected == EntryCount - 1)
                    {
                        manager.ShowMenu();
                    }
                    else
                    {
                        int act = selected % ActsPerGame;
                        manager.PlayIntermissionTest(games[selected / ActsPerGame], act + 1, LevelForAct[act]);
                    }
                    break;
            }
        }

        public void Draw(GameRenderer renderer, Canvas g)
        {
            renderer.DrawText(g, "INTERMISSIONS", 7, 4, ArcadeColor.Cyan);

            for (int i = 0; i < EntryCount; i++)
            {
                int row = FirstEntryRow + i * EntrySpacing;
                bool isSelected = (i == selected);
                string name = i == EntryCount - 1
                    ? "BACK"
                    : games[i / ActsPerGame].DisplayName + " " + (i % ActsPerGame + 1);

                renderer.DrawText(g, name, NameColumn, row, isSelected ? ArcadeColor.Yellow : ArcadeColor.White);

                if (isSelected)
                {
                    int frame = CursorFrames[(tick / AnimFramesPerStep) % CursorFrames.Length];
                    Sprite cursor = renderer.SpritesFor(i == EntryCount - 1 ? games[0] : games[i / ActsPerGame]).GetPacmanSprite(frame, Direction.Right);
                    GameRenderer.DrawSprite(g, cursor, CursorColumn * GameConfig.TileSize, row * GameConfig.TileSize - GameConfig.TileSize / 2);
                }
            }

            renderer.DrawText(g, "UP/DOWN TO CHOOSE", 5, 28, ArcadeColor.Cyan);
            renderer.DrawText(g, "PRESS 1 TO PLAY", 5, 30, ArcadeColor.Cyan);
        }
    }
}
