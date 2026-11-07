namespace PacmanWinForms
{
    /// <summary>
    /// Paramètres d'affichage : l'image du jeu est dessinée à la résolution de l'arcade, puis agrandie.
    /// </summary>
    public static class DisplayConfig
    {
        public const int Zoom = 3;                                                   // Multiplicateur pour écran moderne
        public const int NativeWidth = GameConfig.MapWidth * GameConfig.TileSize;    // 224
        public const int NativeHeight = GameConfig.MapHeight * GameConfig.TileSize;  // 288
        public const int ScreenWidth = NativeWidth * Zoom;                           // 672
        public const int ScreenHeight = NativeHeight * Zoom;                         // 864
    }
}