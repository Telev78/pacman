namespace PacmanWinForms
{
    /// <summary>
    /// Tout ce qui appartient à un joueur et qui doit survivre quand l'autre joueur prend la main :
    /// score, vies, niveau, labyrinthe (gommes restantes) et avancement des règles du fruit et du nid.
    /// </summary>
    public sealed class PlayerState
    {
        public int Number { get; private set; }        // 1 ou 2

        public int Score;
        public bool BonusLifeAwarded;                  // La vie bonus n'est donnée qu'une fois par partie
        public int Lives;
        public int Level = 1;

        public LevelMap Map = new LevelMap();
        public int DotsRemaining;

        // Fruit : seuils d'apparition déjà passés dans ce niveau (voir FruitManager)
        public int TotalDots;
        public bool FruitSpawned70;
        public bool FruitSpawned170;

        // Nid : compteurs individuels de Pinky, Inky, Clyde (voir GhostHouse)
        public int[] HouseDotCounters = new int[3];

        // Vrai après une mort : à son retour, les règles "après une mort" s'appliquent
        // (compteur global du nid, Elroy suspendu)
        public bool HasLostALife;

        public PlayerState(int number, int lives)
        {
            Number = number;
            Lives = lives;
        }
    }
}