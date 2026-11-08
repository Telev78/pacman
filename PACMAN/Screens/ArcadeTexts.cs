namespace PacmanWinForms
{
    /// <summary>
    /// Textes des écrans hors partie. Version japonaise (Namco) ;
    /// une version américaine (Midway) pourra être ajoutée plus tard.
    /// </summary>
    public static class ArcadeTexts
    {
        public const string PushStart = "PUSH START BUTTON";
        public const string OnePlayerOnly = "1 PLAYER ONLY";
        public const string OneOrTwoPlayers = "1 OR 2 PLAYERS";
        public const string BonusLife = "BONUS PUCKMAN FOR 10000";   // Suivi du sprite "PTS"
        public const string Copyright = "©";                         // Suivi du logo "namco" puis de l'année
        public const string Year = "1980";
        public const string Credit = "CREDIT";
        public const string PlayerOne = "PLAYER ONE";
        public const string PlayerTwo = "PLAYER TWO";

        public const string CharacterNickname = "CHARACTER / NICKNAME";

        // Un nom et un surnom par fantôme, dans l'ordre Blinky, Pinky, Inky, Clyde.
        // Le surnom (entre guillemets) s'écrit directement derrière le nom.
        public static readonly string[] GhostNames = { "OIKAKE----", "MACHIBUSE--", "KIMAGURE--", "OTOBOKE---" };
        public static readonly string[] GhostNicknames = { "\"AKABEI\"", "\"PINKY\"", "\"AOSUKE\"", "\"GUZUTA\"" };

        public const string Points10 = "10";
        public const string Points50 = "50";
    }
}