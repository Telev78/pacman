namespace PacmanWinForms
{
    /// <summary>
    /// Écran de test : joue une seule intermission du jeu courant, puis revient au menu des intermissions.
    /// Échap (ou la touche menu) l'interrompt.
    /// </summary>
    public sealed class IntermissionTestScreen : IScreen
    {
        private readonly ScreenManager manager;
        private readonly int number;
        private readonly int level;
        private IIntermission intermission;

        public IntermissionTestScreen(ScreenManager manager, int number, int level)
        {
            this.manager = manager;
            this.number = number;
            this.level = level;
        }

        public void Enter()
        {
            intermission = manager.Variant.CreateIntermission(number, level, manager.Sound);
            if (intermission == null) manager.ShowIntermissionMenu();
        }

        public void Update()
        {
            if (intermission == null) return;

            intermission.Update();
            if (intermission.Finished) manager.ShowIntermissionMenu();
        }

        public void OnInput(InputKey key)
        {
            if (key == InputKey.Menu) manager.ShowIntermissionMenu();
        }

        public void Draw(GameRenderer renderer, Canvas g)
        {
            if (intermission != null) intermission.Draw(renderer, g);
        }
    }
}
