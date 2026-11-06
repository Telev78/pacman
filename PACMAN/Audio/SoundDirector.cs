namespace PacmanWinForms
{
    /// <summary>
    /// Pilote tous les sons du jeu : jingles, bruitages et sirènes de fond.
    /// Le reste du code décrit des événements ("une gomme est mangée"), pas des fichiers audio.
    /// </summary>
    public class SoundDirector
    {
        private static readonly SoundId[] SirenIntros =
        {
            SoundId.Siren0FirstLoop, SoundId.Siren1FirstLoop, SoundId.Siren2FirstLoop,
            SoundId.Siren3FirstLoop, SoundId.Siren4FirstLoop
        };

        private static readonly SoundId[] SirenLoops =
        {
            SoundId.Siren0, SoundId.Siren1, SoundId.Siren2, SoundId.Siren3, SoundId.Siren4
        };

        private readonly AudioEngine audio = new AudioEngine();

        private bool wakaToggle = false;
        private int currentSirenIndex = -1; // -1 : aucune sirène lancée (forcera le premier chargement)

        public void StopAll()
        {
            audio.StopAll();
        }

        public void CleanUp()
        {
            audio.Dispose();
        }

        // Début de niveau ou nouvelle vie : la sirène devra être relancée
        public void ResetSiren()
        {
            currentSirenIndex = -1;
        }

        // --- Événements ponctuels ---

        public void PlayStartJingle()
        {
            audio.Play(SoundId.Start);
        }

        public void PlayDot()
        {
            wakaToggle = !wakaToggle;
            audio.Play(wakaToggle ? SoundId.EatDot0 : SoundId.EatDot1);
        }

        public void PlayFruitEaten()
        {
            audio.Play(SoundId.EatFruit);
        }

        public void PlayExtraLife()
        {
            audio.Play(SoundId.Extend);
        }

        public void PlayGhostEaten()
        {
            audio.Play(SoundId.EatGhost);
        }

        public void PlayDeath()
        {
            audio.StopAll(); // On coupe impérativement les sirènes de fond
            audio.PlaySequence(SoundId.Death0, SoundId.Death1, loopNext: false);
        }

        // --- Mode effrayé et yeux ---

        public void StartFrightened()
        {
            StopCurrentSiren();
            audio.PlaySequence(SoundId.FrightFirstLoop, SoundId.Fright, loopNext: true);
        }

        public void EndFrightened()
        {
            audio.Stop(SoundId.Fright);
            audio.Stop(SoundId.Eyes);

            if (currentSirenIndex >= 0)
            {
                PlaySirenLoop();
            }
        }

        public void StartEyes()
        {
            audio.PlaySequence(SoundId.EyesFirstLoop, SoundId.Eyes, loopNext: true);
        }

        // À appeler à chaque frame : coupe le son des yeux quand plus aucun fantôme n'est en retour au nid
        public void UpdateEyes(bool anyEyesRunning)
        {
            if (!anyEyesRunning)
            {
                audio.Stop(SoundId.Eyes);
            }
        }

        // --- Sirènes de fond ---

        // À appeler à chaque frame de jeu.
        // ghostsSpooked : au moins un fantôme est effrayé (dans le labyrinthe) ou en yeux.
        public void UpdateSiren(int level, int dotsRemaining, bool ghostsSpooked)
        {
            // Seuils (gommes restantes) : 164 / 144 / 124 / 104, décalés de 0, 10, 20, 30 ou 40 selon le niveau
            int offset;
            if (level == 1) offset = 0;
            else if (level == 2) offset = 10;
            else if (level <= 5) offset = 20;
            else if (level <= 20) offset = 30;
            else offset = 40;

            int targetSiren;
            if (dotsRemaining <= 104 + offset) targetSiren = 4;
            else if (dotsRemaining <= 124 + offset) targetSiren = 3;
            else if (dotsRemaining <= 144 + offset) targetSiren = 2;
            else if (dotsRemaining <= 164 + offset) targetSiren = 1;
            else targetSiren = 0;

            // Pendant la panique ou le retour des yeux, on met l'index à jour sans lancer le son,
            // pour ne pas écraser les bruitages prioritaires (la sirène reprendra au bon index ensuite)
            if (ghostsSpooked)
            {
                currentSirenIndex = targetSiren;
                return;
            }

            if (targetSiren != currentSirenIndex)
            {
                StopCurrentSiren();
                currentSirenIndex = targetSiren;
                PlaySirenLoop();
            }
        }

        private void StopCurrentSiren()
        {
            if (currentSirenIndex >= 0)
            {
                audio.Stop(SirenLoops[currentSirenIndex]);
            }
        }

        // Intro de la sirène (firstloop), puis boucle infinie
        private void PlaySirenLoop()
        {
            audio.PlaySequence(SirenIntros[currentSirenIndex], SirenLoops[currentSirenIndex], loopNext: true);
        }
    }
}