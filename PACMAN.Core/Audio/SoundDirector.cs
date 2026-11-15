namespace PacmanWinForms
{
    /// <summary>
    /// Pilote tous les sons du jeu : jingles, bruitages et son de fond.
    /// Un seul son de fond joue à la fois, par ordre de priorité : yeux > panique > sirène.
    /// </summary>
    public class SoundDirector
    {
        private enum Background { None, Siren, Frightened, Eyes }

        private static readonly SoundId[] SirenIntros =
        {
            SoundId.Siren0FirstLoop, SoundId.Siren1FirstLoop, SoundId.Siren2FirstLoop,
            SoundId.Siren3FirstLoop, SoundId.Siren4FirstLoop
        };

        private static readonly SoundId[] SirenLoops =
        {
            SoundId.Siren0, SoundId.Siren1, SoundId.Siren2, SoundId.Siren3, SoundId.Siren4
        };

        private readonly IAudioEngine audio;

        private bool wakaToggle = false;
        private Background background = Background.None;
        private int currentSirenIndex = -1; // Sirène en cours de lecture (quand background == Siren)

        public bool Muted { get; set; }

        public SoundDirector(IAudioEngine audio)
        {
            if (audio == null) throw new System.ArgumentNullException("audio");
            this.audio = audio;
        }

        public void StopAll()
        {
            audio.StopAll();
            background = Background.None;
            currentSirenIndex = -1;
        }

        public void CleanUp()
        {
            audio.Dispose();
        }

        // Début de niveau ou nouvelle vie : le son de fond sera relancé à la prochaine frame de jeu
        public void ResetBackground()
        {
            StopBackground();
            background = Background.None;
            currentSirenIndex = -1;
        }

        // --- Événements ponctuels ---

        public void PlayStartJingle()
        {
            if (Muted) return;
            audio.Play(SoundId.Start);
        }

        public void PlayDot()
        {
            if (Muted) return;
            wakaToggle = !wakaToggle;
            audio.Play(wakaToggle ? SoundId.EatDot0 : SoundId.EatDot1);
        }

        public void PlayFruitEaten()
        {
            if (Muted) return;
            audio.Play(SoundId.EatFruit);
        }

        public void PlayExtraLife()
        {
            if (Muted) return;
            audio.Play(SoundId.Extend);
        }

        public void PlayGhostEaten()
        {
            if (Muted) return;
            audio.Play(SoundId.EatGhost);
        }

        public void PlayDeath()
        {
            StopAll(); // On coupe impérativement le son de fond
            if (Muted) return;
            audio.PlaySequence(SoundId.Death0, SoundId.Death1, loopNext: false);
        }

        public void PlayFruitBounce()
        {
            if (Muted) return;
            audio.Play(SoundId.FruitBounce);
        }

        public void PlayCredit()
        {
            audio.Play(SoundId.Credit);
        }

        public void PlayIntermission()
        {
            if (Muted) return;
            audio.PlaySequence(SoundId.Intermission, SoundId.Intermission, loopNext: false);
        }

        /// <summary>Musique de l'intermission 1, 2 ou 3 de Ms Pac-Man (jouée une seule fois).</summary>
        public void PlayMsIntermission(int number)
        {
            if (Muted) return;
            SoundId id = number == 2 ? SoundId.Intermission2 : number == 3 ? SoundId.Intermission3 : SoundId.Intermission;
            audio.Play(id);
        }

        /// <summary>Choc des deux fantômes à la fin de l'intermission 1 de Ms Pac-Man.</summary>
        public void PlayIntermissionBump()
        {
            if (Muted) return;
            audio.Play(SoundId.IntermissionBump);
        }

        public float Volume
        {
            get { return audio.Volume; }
            set { audio.Volume = value; }
        }

        // --- Son de fond ---

        // À appeler à chaque frame de jeu.
        // eyesRunning : au moins un fantôme est en yeux, en route vers le nid.
        // frightenedGhosts : au moins un fantôme est encore effrayé (bleu).
        public void UpdateBackground(int level, int dotsRemaining, bool eyesRunning, bool frightenedGhosts)
        {
            if (Muted) return;
            int targetSiren = ComputeSirenIndex(level, dotsRemaining);

            Background wanted;
            if (eyesRunning) wanted = Background.Eyes;
            else if (frightenedGhosts) wanted = Background.Frightened;
            else wanted = Background.Siren;

            // Déjà le bon son ? (pour la sirène, il faut aussi que ce soit le bon index)
            if (wanted == background && (wanted != Background.Siren || targetSiren == currentSirenIndex))
            {
                return;
            }

            StopBackground();

            switch (wanted)
            {
                case Background.Eyes:
                    audio.PlaySequence(SoundId.EyesFirstLoop, SoundId.Eyes, loopNext: true);
                    break;

                case Background.Frightened:
                    audio.PlaySequence(SoundId.FrightFirstLoop, SoundId.Fright, loopNext: true);
                    break;

                case Background.Siren:
                    currentSirenIndex = targetSiren;
                    audio.PlaySequence(SirenIntros[currentSirenIndex], SirenLoops[currentSirenIndex], loopNext: true);
                    break;
            }

            background = wanted;
        }

        private void StopBackground()
        {
            switch (background)
            {
                case Background.Eyes:
                    audio.Stop(SoundId.Eyes);
                    break;

                case Background.Frightened:
                    audio.Stop(SoundId.Fright);
                    break;

                case Background.Siren:
                    if (currentSirenIndex >= 0)
                    {
                        audio.Stop(SirenLoops[currentSirenIndex]);
                    }
                    break;
            }
        }

        // Sirène selon le nombre de gommes restantes
        private static int ComputeSirenIndex(int level, int dotsRemaining)
        {
            // Seuils (gommes restantes) : 164 / 144 / 124 / 104, décalés de 0, 10, 20, 30 ou 40 selon le niveau
            int offset;
            if (level == 1) offset = 0;
            else if (level == 2) offset = 10;
            else if (level <= 5) offset = 20;
            else if (level <= 20) offset = 30;
            else offset = 40;

            if (dotsRemaining <= 104 + offset) return 4;
            if (dotsRemaining <= 124 + offset) return 3;
            if (dotsRemaining <= 144 + offset) return 2;
            if (dotsRemaining <= 164 + offset) return 1;
            return 0;
        }
    }
}