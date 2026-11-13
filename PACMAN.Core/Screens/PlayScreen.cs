
namespace PacmanWinForms
{
    /// <summary>
    /// Une partie en cours : enveloppe le Game et prévient le ScreenManager quand elle est terminée.
    /// </summary>
    public sealed class PlayScreen : IScreen
    {
        private readonly ScreenManager manager;
        private Game game;

        public Game Game { get { return game; } }

        private readonly int playerCount;
        private IIntermission intermission;

        public PlayScreen(ScreenManager manager, int playerCount)
        {
            this.manager = manager;
            this.playerCount = playerCount;
        }

        public void Enter()
        {
            game = new Game(manager.Sound, manager.HighScore, playerCount: playerCount);
        }

        public void Update()
        {
            // Pendant une intermission, le jeu est en pause
            if (intermission != null)
            {
                intermission.Update();

                if (intermission.Finished)
                {
                    intermission = null;
                    game.FinishIntermission();
                }
                return;
            }

            game.Update();

            if (game.State == GameState.Intermission)
            {
                intermission = CreateIntermission(game.PendingIntermission, game.CurrentLevel);

                // Intermission pas encore écrite : on passe directement au niveau suivant
                if (intermission == null) game.FinishIntermission();
            }

            if (game.IsFinished)
            {
                manager.OnGameEnded(game.HighScore);
            }
        }

        public void OnInput(InputKey key)
        {
            Direction direction = key.ToDirection();
            if (direction != Direction.None)
            {
                game.HandleInput(direction);
            }
        }

        public void Draw(GameRenderer renderer, Canvas g)
        {
            if (intermission != null) intermission.Draw(renderer, g);
            else renderer.DrawGame(g, game);
        }

        private IIntermission CreateIntermission(int number, int level)
        {
            switch (number)
            {
                case 1: return new Intermission1Scene(manager.Sound, level);
                case 2: return new Intermission2Scene(manager.Sound, level);
                case 3: return new Intermission3Scene(manager.Sound, level);
                default: return null;   
            }
        }
    }
}