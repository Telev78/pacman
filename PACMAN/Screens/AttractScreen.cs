using System.Drawing;

namespace PacmanWinForms
{
    public sealed class AttractScreen : IScreen
    {
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
        }

        public void OnInput(InputKey key)
        {
            // Rien en attract mode : l'insertion d'un crédit est gérée par le ScreenManager
        }

        public void Draw(GameRenderer renderer, Graphics g)
        {
            renderer.DrawTopHud(g, 0, manager.HighScore, show1Up: true, show2Up: true);
            renderer.DrawCredits(g, manager.Credits);
        }
    }
}