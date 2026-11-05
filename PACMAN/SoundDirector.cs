namespace PacmanWinForms
{
    /// <summary>
    /// Pilote tous les sons du jeu : jingles, bruitages et sirènes de fond.
    /// Le reste du code décrit des événements ("une gomme est mangée"), pas des fichiers audio.
    /// </summary>
    public class SoundDirector
    {
        private readonly AudioManager audio = new AudioManager();

        private bool wakaToggle = false;
        private int currentSirenIndex = -1; // -1 : aucune sirène lancée (forcera le premier chargement)

        public void StopAll()
        {
            audio.StopAll();
        }

        public void CleanUp()
        {
            audio.CleanUp();
        }

        // Début de niveau ou nouvelle vie : la sirène devra être relancée
        public void ResetSiren()
        {
            currentSirenIndex = -1;
        }

        // --- Événements ponctuels ---

        public void PlayStartJingle()
        {
            audio.Play("start");
        }

        public void PlayDot()
        {
            wakaToggle = !wakaToggle;
            audio.Play(wakaToggle ? "eat_dot_0" : "eat_dot_1");
        }

        public void PlayFruitEaten()
        {
            audio.Play("eat_fruit");
        }

        public void PlayExtraLife()
        {
            audio.Play("extend");
        }

        public void PlayGhostEaten()
        {
            audio.Play("eat_ghost");
        }

        public void PlayDeath()
        {
            audio.StopAll(); // On coupe impérativement les sirènes de fond
            audio.PlaySequence("death_0", "death_1", false);
        }

        // --- Mode effrayé et yeux ---

        public void StartFrightened()
        {
            audio.Stop($"siren{currentSirenIndex}");
            audio.PlaySequence("fright_firstloop", "fright", loopNext: true);
        }

        public void EndFrightened()
        {
            audio.Stop("fright");
            audio.Stop("eyes");

            if (currentSirenIndex >= 0)
            {
                PlaySirenLoop();
            }
        }

        public void StartEyes()
        {
            audio.PlaySequence("eyes_firstloop", "eyes", loopNext: true);
        }

        // À appeler à chaque frame : coupe le son des yeux quand plus aucun fantôme n'est en retour au nid
        public void UpdateEyes(bool anyEyesRunning)
        {
            if (!anyEyesRunning)
            {
                audio.Stop("eyes");
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
                if (currentSirenIndex >= 0)
                {
                    audio.Stop($"siren{currentSirenIndex}");
                }

                currentSirenIndex = targetSiren;
                PlaySirenLoop();
            }
        }

        // Intro de la sirène (firstloop), puis boucle infinie
        private void PlaySirenLoop()
        {
            audio.PlaySequence(
                $"siren{currentSirenIndex}_firstloop",
                $"siren{currentSirenIndex}",
                loopNext: true);
        }
    }
}