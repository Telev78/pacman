namespace PacmanWinForms
{
    /// <summary>
    /// Paramètres d'affichage : l'image du jeu est dessinée à la résolution de l'arcade, puis agrandie.
    /// </summary>
    public static class DisplayConfig
    {
        public const int NativeWidth = GameConfig.MapWidth * GameConfig.TileSize;    // 224
        public const int NativeHeight = GameConfig.MapHeight * GameConfig.TileSize;  // 288
    }
}