
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
        void Draw(GameRenderer renderer, Canvas g);  // Dessin à la résolution de l'arcade (224x288)
    }

    /// <summary>L'écran de présentation (attract mode) d'un jeu : un crédit y mène à l'écran start.</summary>
    public interface IAttractScreen : IScreen
    {
    }

    /// <summary>L'écran "PUSH START BUTTON" d'un jeu.</summary>
    public interface IStartScreen : IScreen
    {
    }
}