using System;

namespace PacmanWinForms
{
    /// <summary>
    /// Scène de l'attract mode : Pac-Man poursuivi par les fantômes mange une super-gomme,
    /// puis mange les fantômes devenus bleus. Les valeurs sont mesurées sur une vidéo de l'arcade.
    /// </summary>
    public sealed class ChaseScene
    {
        private const int GhostCount = 4;
        private static readonly GhostType[] GhostTypes = { GhostType.Blinky, GhostType.Pinky, GhostType.Inky, GhostType.Clyde };

        // Les sprites sont sur la ligne 20, comme la super-gomme de gauche
        private const int SpriteY = 20 * GameConfig.TileSize - 4;

        // --- Paramètres mesurés (en pixels et en frames) ---
        private const float PacmanStartX = 220f;        // Pac-Man entre par le bord droit
        private const float GhostSpacing = 16f;         // Les fantômes se touchent, derrière Pac-Man
        private const float ChaseSpeed = 1.117f;        // Vitesse vers la gauche (Pac-Man et fantômes)
        private const float GhostChaseSpeed = 1.19f;  // un peu plus rapide que Pac-Man (ChaseSpeed)
        private const float GhostInitialGap = 28f;    // écart supplémentaire de Blinky au départ
        private const float EnergizerEatX = 35f;        // Pac-Man mange la super-gomme à cette position
        private const float PacmanTurnX = 30f;          // ...et continue jusqu'ici avant de faire demi-tour
        private const int TurnPauseFrames = 3;          // Petite pause au demi-tour
        private const float PacmanReturnSpeed = 1.2f;   // Pac-Man poursuit les fantômes bleus
        private const float FrightenedGhostSpeed = 0.57f;
        private const float CaptureDistance = 6.5f;     // Écart (bord gauche à bord gauche) à partir duquel il mange un fantôme
        private const int CaptureFreezeFrames = 60;     // Gel d'une seconde à chaque capture

        private const int LegAnimationFrames = 6;       // Les jambes des fantômes changent toutes les 6 frames
        private const int PixelsPerMouthFrame = 4;      // La bouche de Pac-Man change tous les 4 pixels parcourus

        private enum Stage { ChasingLeft, Overshoot, Pause, ChasingRight }

        private Stage stage = Stage.ChasingLeft;

        private float pacmanX = PacmanStartX;
        private bool pacmanFacingRight = false;
        private bool pacmanVisible = true;
        private float distanceTraveled = 0f;

        private readonly float[] ghostX = new float[GhostCount];
        private readonly bool[] ghostEaten = new bool[GhostCount];
        private bool frightened = false;

        private int pauseFrames = 0;
        private int freezeFrames = 0;
        private int eatenCount = 0;
        private int scoreValue = 0;
        private float scoreX = 0f;
        private int animationTicks = 0;

        // La super-gomme de gauche a été mangée
        public bool EnergizerEaten { get; private set; }

        // Le dernier fantôme est mangé et son gel est terminé
        public bool Finished { get; private set; }

        public ChaseScene()
        {
            for (int i = 0; i < GhostCount; i++)
            {
                ghostX[i] = PacmanStartX + GhostSpacing * (i + 1);
                ghostX[i] = PacmanStartX + GhostInitialGap + GhostSpacing * i;
            }
        }

        public void Update()
        {
            // Gel après la capture d'un fantôme : tout est figé
            if (freezeFrames > 0)
            {
                freezeFrames--;

                if (freezeFrames == 0)
                {
                    if (eatenCount == GhostCount) Finished = true;
                    else pacmanVisible = true;
                }
                return;
            }

            animationTicks++;

            switch (stage)
            {
                case Stage.ChasingLeft:
                    MovePacman(-ChaseSpeed);
                    for (int i = 0; i < GhostCount; i++)
                    {
                        ghostX[i] -= GhostChaseSpeed;
                    }

                    if (pacmanX <= EnergizerEatX)
                    {
                        // Super-gomme mangée : les fantômes deviennent bleus sur place et repartent à droite
                        EnergizerEaten = true;
                        frightened = true;
                        stage = Stage.Overshoot;
                    }
                    break;

                case Stage.Overshoot:
                    MoveGhostsRight();
                    MovePacman(-ChaseSpeed);

                    if (pacmanX <= PacmanTurnX)
                    {
                        pauseFrames = TurnPauseFrames;
                        stage = Stage.Pause;
                    }
                    break;

                case Stage.Pause:
                    MoveGhostsRight();
                    pauseFrames--;

                    if (pauseFrames <= 0)
                    {
                        pacmanFacingRight = true;
                        stage = Stage.ChasingRight;
                    }
                    break;

                case Stage.ChasingRight:
                    MovePacman(PacmanReturnSpeed);
                    MoveGhostsRight();
                    CheckCapture();
                    break;
            }
        }

        private void MovePacman(float dx)
        {
            pacmanX += dx;
            distanceTraveled += Math.Abs(dx);
        }

        private void MoveGhostsRight()
        {
            for (int i = 0; i < GhostCount; i++)
            {
                ghostX[i] += FrightenedGhostSpeed;
            }
        }

        // Pac-Man mange toujours le fantôme le plus proche de lui : Blinky, puis Pinky, Inky, Clyde
        private void CheckCapture()
        {
            if (eatenCount >= GhostCount) return;

            int i = eatenCount;
            if (ghostX[i] - pacmanX <= CaptureDistance)
            {
                ghostEaten[i] = true;
                eatenCount++;

                scoreValue = 200 << (eatenCount - 1);   // 200, 400, 800, 1600
                scoreX = ghostX[i];

                pacmanVisible = false;
                freezeFrames = CaptureFreezeFrames;
            }
        }

        public void Draw(GameRenderer renderer, Canvas g)
        {
            SpriteManager sprites = renderer.Sprites;
            int legFrame = (animationTicks / LegAnimationFrames) % 2;

            // Les fantômes (celui qui vient d'être mangé n'est plus dessiné)
            for (int i = 0; i < GhostCount; i++)
            {
                if (ghostEaten[i]) continue;

                Sprite sprite = frightened
                    ? sprites.GetGhostFrightenedSprite(legFrame)
                    : sprites.GetGhostSprite(GhostTypes[i], Direction.Left, legFrame);

                GameRenderer.DrawSprite(g, sprite, (int)ghostX[i], SpriteY);
            }

            if (pacmanVisible)
            {
                int mouthFrame = (int)(distanceTraveled / PixelsPerMouthFrame) % 4;
                Direction direction = pacmanFacingRight ? Direction.Right : Direction.Left;

                GameRenderer.DrawSprite(g, sprites.GetPacmanSprite(mouthFrame, direction), (int)pacmanX, SpriteY);
            }

            // Pendant le gel, le score prend la place du fantôme mangé
            if (freezeFrames > 0)
            {
                GameRenderer.DrawSprite(g, sprites.GetGhostPointsSprite(scoreValue), (int)scoreX, SpriteY);
            }
        }
    }
}