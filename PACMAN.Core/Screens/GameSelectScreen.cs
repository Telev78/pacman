using System.Collections.Generic;
using static PacmanWinForms.SpriteManager;

namespace PacmanWinForms
{
    /// <summary>
    /// Premier écran : choix du jeu (Pac-Man, Ms Pac-Man). Haut / Bas pour choisir, 1 (ou 2) pour valider.
    /// </summary>
    public sealed class GameSelectScreen : IScreen
    {
        private const int TitleRow = 8;
        private const int FirstEntryRow = 14;
        private const int EntrySpacing = 4;
        private const int CursorColumn = 8;
        private const int NameColumn = 11;
        private const int AnimFramesPerStep = 6;

        // Animation du curseur : bouche grande ouverte, mi-ouverte, fermée, mi-ouverte
        private static readonly int[] CursorFrames = { 1, 0, 3, 0 };

        private readonly ScreenManager manager;
        private readonly IReadOnlyList<GameVariant> choices = GameVariants.All;
        private int EntryCount { get { return choices.Count; } }

        // Code secret (Konami) qui ouvre le menu des intermissions : B = Start 2 joueurs, A = Start 1 joueur
        private static readonly InputKey[] SecretCode =
        {
            InputKey.Up, InputKey.Up, InputKey.Down, InputKey.Down,
            InputKey.Left, InputKey.Right, InputKey.Left, InputKey.Right,
            InputKey.Start2, InputKey.Start1
        };
        private int codeProgress;
        private int selected;
        private int tick;

        public GameSelectScreen(ScreenManager manager)
        {
            this.manager = manager;
        }

        public void Enter()
        {
            tick = 0;
            selected = 0;
            codeProgress = 0;

            // Au retour au menu, le curseur repart sur le jeu en cours
            if (manager.Variant != null)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    if (choices[i].Id == manager.Variant.Id) selected = i;
                }
            }
        }

        public void Update()
        {
            tick++;
        }

        public void OnInput(InputKey key)
        {
            // Le code avance d'une touche à chaque bonne touche ; une mauvaise touche le remet à zéro
            // (et peut en recommencer un si c'est un Haut).
            if (key == SecretCode[codeProgress])
            {
                codeProgress++;
                if (codeProgress == SecretCode.Length)
                {
                    codeProgress = 0;
                    manager.ShowIntermissionMenu();
                    return;
                }

                // Les touches B puis A du code ne doivent pas lancer une partie
                if (key == InputKey.Start1 || key == InputKey.Start2) return;
            }
            else
            {
                codeProgress = key == InputKey.Up ? 1 : 0;
            }

            switch (key)
            {
                case InputKey.Up:
                    selected = (selected + EntryCount - 1) % EntryCount;
                    break;
                case InputKey.Down:
                    selected = (selected + 1) % EntryCount;
                    break;
                case InputKey.Start1:
                case InputKey.Start2:
                    manager.SelectVariant(choices[selected]);
                    break;
            }
        }

        public void Draw(GameRenderer renderer, Canvas g)
        {
            renderer.DrawText(g, "SELECT GAME", 8, TitleRow, ArcadeColor.Cyan);

            for (int i = 0; i < EntryCount; i++)
            {
                int row = FirstEntryRow + i * EntrySpacing;
                bool isSelected = (i == selected);
                string name = choices[i].DisplayName;

                renderer.DrawText(g, name, NameColumn, row, isSelected ? ArcadeColor.Yellow : ArcadeColor.White);

                if (isSelected)
                {
                    int frame = CursorFrames[(tick / AnimFramesPerStep) % CursorFrames.Length];
                    Sprite cursor = renderer.SpritesFor(choices[i]).GetPacmanSprite(frame, Direction.Right);
                    GameRenderer.DrawSprite(g, cursor, CursorColumn * GameConfig.TileSize, row * GameConfig.TileSize - GameConfig.TileSize / 2);
                }
            }

            renderer.DrawText(g, "UP/DOWN TO CHOOSE", 5, 28, ArcadeColor.Cyan);
            renderer.DrawText(g, "PRESS 1 TO SELECT", 5, 30, ArcadeColor.Cyan);
        }
    }
}
