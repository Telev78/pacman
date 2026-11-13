
namespace PacmanWinForms
{
    /// <summary>
    /// Une intermission (petite animation entre deux niveaux), mise à jour à 60 Hz par l'écran de jeu.
    /// </summary>
    public interface IIntermission
    {
        bool Finished { get; }
        void Update();
        void Draw(GameRenderer renderer, Canvas g);
    }
}