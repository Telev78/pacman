namespace PacmanWinForms
{
    /// <summary>
    /// Positions du HUD et des messages à l'écran, en tuiles (colonne, ligne) de l'écran de l'arcade.
    /// </summary>
    public static class HudLayout
    {
        // --- Bandeau supérieur ---
        public const int TopRow = 0;
        public const int OneUpColumn = 3;
        public const int HighScoreLabelColumn = 9;

        public const int ScoreRow = 1;
        public const int ScoreRightColumn = 6;        // Le score est aligné à droite sur cette colonne
        public const int HighScoreRightColumn = 16;   // Le high score aussi

        // --- Messages au centre ---
        public const int MessageRow = 20;
        public const int ReadyColumn = 11;
        public const int GameOverColumn = 9;

        // --- Bandeau inférieur ---
        public const int BottomRow = 34;

        public const int LivesFirstColumn = 2;        // Réserve de vies, en bas à gauche
        public const int LivesSpacing = 2;
        public const int MaxVisibleLives = 5;

        public const int LevelIndicatorRightColumn = 24;   // Fruits des niveaux, en bas à droite
        public const int LevelIndicatorSpacing = 2;
        public const int MaxLevelIndicators = 7;

        public const int TwoUpColumn = 22;

        public const int PlayerLabelColumn = 9;     // "PLAYER ONE" au-dessus du nid, pendant READY!
        public const int PlayerLabelRow = 14;

        public const int CreditRow = 35;            // Ligne tout en bas (hors partie)
        public const int CreditLabelColumn = 2;
        public const int CreditValueRightColumn = 10;   // Le nombre est aligné à droite sur cette colonne
    }
}