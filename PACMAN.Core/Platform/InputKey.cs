namespace PacmanWinForms
{
    /// <summary>
    /// Les touches du jeu, indépendantes du clavier : les écrans ne connaissent pas WinForms.
    /// </summary>
    public enum InputKey
    {
        Up, Down, Left, Right,
        Coin,    // Insertion d'un crédit
        Start1,  // Bouton start 1 joueur
        Start2,  // Bouton start 2 joueurs
        Record,  // Développement : enregistre la démo (F9)
        Menu     // Retour au menu de choix du jeu (Échap)
    }

    public static class InputKeyExtensions
    {
        public static Direction ToDirection(this InputKey key)
        {
            switch (key)
            {
                case InputKey.Up: return Direction.Up;
                case InputKey.Down: return Direction.Down;
                case InputKey.Left: return Direction.Left;
                case InputKey.Right: return Direction.Right;
                default: return Direction.None;
            }
        }
    }
}