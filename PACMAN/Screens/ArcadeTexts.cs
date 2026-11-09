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
        public static string BonusLife
        {
            get
            {
                string name = IsUsa ? "PAC-MAN" : "PUCKMAN";
                return "BONUS " + name + " FOR " + Settings.BonusLife;   // Suivi du sprite "PTS"
            }
        }
        public const string Copyright = "©";                         // Suivi du logo "namco" puis de l'année
        public const string Year = "1980";
        public const string Credit = "CREDIT";
        public const string PlayerOne = "PLAYER ONE";
        public const string PlayerTwo = "PLAYER TWO";

        public const string CharacterNickname = "CHARACTER / NICKNAME";

        // Un nom et un surnom par fantôme, dans l'ordre Blinky, Pinky, Inky, Clyde.
        // Le surnom (entre guillemets) s'écrit directement derrière le nom.
        // Colonne de départ du nom ; les surnoms sont toujours à la colonne 18
        public static int GhostNameColumn { get { return IsUsa ? 7 : 8; } }
        
        // JAP : le surnom suit directement le nom (colonne 8 + longueur du nom) ; USA : toujours colonne 18
        public static int GhostNicknameColumn(int ghostIndex)
        {
            return IsUsa ? 18 : GhostNameColumn + GhostNames[ghostIndex].Length;
        }

        private static readonly string[] JapNames = { "OIKAKE----", "MACHIBUSE--", "KIMAGURE--", "OTOBOKE---" };
        private static readonly string[] JapNicknames = { "\"AKABEI\"", "\"PINKY\"", "\"AOSUKE\"", "\"GUZUTA\"" };
        private static readonly string[] UsaNames = { "-SHADOW", "-SPEEDY", "-BASHFUL", "-POKEY" };
        private static readonly string[] UsaNicknames = { "\"BLINKY\"", "\"PINKY\"", "\"INKY\"", "\"CLYDE\"" };

        public static string[] GhostNames { get { return IsUsa ? UsaNames : JapNames; } }
        public static string[] GhostNicknames { get { return IsUsa ? UsaNicknames : JapNicknames; } }

        // Version USA : ligne de copyright en texte, à la place du logo namco
        public const string MidwayLine = "© 1980 MIDWAY MFG.CO.";

        public const string Points10 = "10";
        public const string Points50 = "50";

        public static bool IsUsa { get { return Settings.Version == GameVersion.Usa; } }
    }
}