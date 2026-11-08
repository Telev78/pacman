using System.Drawing;

namespace PacmanWinForms
{
    /// <summary>
    /// Un écran de l'application (attract mode, écran start, partie...), mis à jour à 60 Hz.
    /// </summary>
    public interface IScreen
    {
        void Enter();                                  // L'écran devient actif
        void Update();                                 // Une frame de 1/60 s
        void OnInput(InputKey key);                    // Touche pressée (les crédits sont gérés par le ScreenManager)
        void Draw(GameRenderer renderer, Graphics g);  // Dessin à la résolution de l'arcade (224x288)
    }
}