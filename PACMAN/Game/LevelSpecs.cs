namespace PacmanWinForms
{
    /// <summary>
    /// Paramètres d'un niveau (vitesses, panique, Elroy).
    /// Les instances sont mises en cache et partagées par GameConfig : ne jamais les modifier.
    /// </summary>
    public class LevelSpecs
    {
        // Vitesses de Pac-Man
        public float PacmanNormal { get; set; }

        // Vitesses des fantômes
        public float GhostNormal { get; set; }
        public float GhostTunnel { get; set; }          // Dans les tunnels latéraux

        // Vitesses en mode Frightened (Super-Gomme)
        public float FrightenedPacman { get; set; }
        public float FrightenedGhost { get; set; }

        // Paramètres de configuration du mode Frightened
        public int FrightenedTimeSec { get; set; }     // Durée de la vulnérabilité en secondes
        public int FlashesCount { get; set; }          // Nombre de clignotements bleus/blancs avant la fin

        // Cruise Elroy (Blinky)
        public int Elroy1Dots { get; set; }            // Gommes restantes pour Elroy 1
        public int Elroy2Dots { get; set; }            // Gommes restantes pour Elroy 2
        public float Elroy1Speed { get; set; }
        public float Elroy2Speed { get; set; }
    }
}