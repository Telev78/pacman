namespace PacmanWinForms
{
    /// <summary>
    /// Enchaîne les vagues Scatter / Chase d'un niveau.
    /// Index pairs = Scatter, index impairs = Chase, la vague 7 (Chase) est infinie.
    /// </summary>
    public class WaveScheduler
    {
        private const int LastWaveIndex = 7;

        private int waveIndex = 0;
        private int timerFrames = 0;

        public GhostMode CurrentMode { get; private set; } = GhostMode.Scatter;

        // Début de niveau ou nouvelle vie : on repart de la première vague
        public void Reset()
        {
            waveIndex = 0;
            timerFrames = 0;
            CurrentMode = GhostMode.Scatter;
        }

        // À appeler à chaque frame de jeu. Renvoie true si le mode global vient de changer.
        // Le timer est figé quand isFrozen est vrai (mode Frightened).
        public bool Update(int level, bool isFrozen, GameVariant variant)
        {
            if (isFrozen) return false;
            if (waveIndex >= LastWaveIndex) return false;

            timerFrames++;
            if (timerFrames < variant.GetWaveDurationFrames(level, waveIndex))
                return false;

            waveIndex++;
            timerFrames = 0;

            GhostMode newMode = (waveIndex % 2 == 0) ? GhostMode.Scatter : GhostMode.Chase;
            if (newMode == CurrentMode)
                return false;

            CurrentMode = newMode;
            return true;
        }
    }
}