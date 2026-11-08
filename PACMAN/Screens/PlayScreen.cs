using System.Drawing;

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

        public PlayScreen(ScreenManager manager)
        {
            this.manager = manager;
        }

        public void Enter()
        {
            game = new Game(manager.Sound, manager.HighScore);
        }

        public void Update()
        {
            game.Update();

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

        public void Draw(GameRenderer renderer, Graphics g)
        {
            renderer.DrawGame(g, game);
        }
    }
}