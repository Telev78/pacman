using System;

namespace PacmanWinForms
{
    /// <summary>
    /// Intermission 1 (après le niveau 2) : Blinky poursuit Pac-Man et le rattrape petit à petit, ils sortent
    /// à gauche ; puis un Blinky bleu revient, poursuivi par un Pac-Man géant, et ils sortent à droite.
    /// Toutes les valeurs sont mesurées image par image sur une vidéo de l'arcade (59,94 images/s).
    /// </summary>
    public sealed class Intermission1Scene : IIntermission
    {
        private const int ScreenWidth = 224;

        // Sprites normaux sur la ligne 20 ; le Pac-Man géant (32x32) est centré sur la même ligne
        private const int SpriteY = 20 * GameConfig.TileSize - 4;
        private const int GiantY = 140;

        // --- Partie 1 : Pac-Man puis Blinky entrent par la droite et sortent par la gauche ---
        private const int PacmanEnterFrame = 15;
        private const float PacmanSpeed = 1.22f;
        private const int BlinkyEnterFrame = 34;
        private const float BlinkySpeed = 1.285f;       // Un peu plus rapide : il rattrape Pac-Man peu à peu

        // --- Partie 2 : Blinky bleu entre par la gauche, suivi du Pac-Man géant ---
        private const int FrightenedEnterFrame = 258;
        private const float FrightenedSpeed = 0.725f;
        private const int GiantEnterFrame = 405;
        private const float GiantSpeed = 1.23f;

        // Les 5 secondes de musique sont jouées deux fois de suite, 20 images après le début
        private const int SoundFrame = 20;
        private const int TotalFrames = 648;            // Fin de la deuxième musique (≈ 10,8 s)

        private const float LegPeriod = 7.95f;       // Jambes des fantômes : une image toutes les ~8 frames
        private const float RedLegPhase = 6f;        // Réglage mesuré pour Blinky rouge
        private const float BlueLegPhase = 1.25f;    // et pour Blinky bleu
        private const int PixelsPerMouthFrame = 4;      // Bouche de Pac-Man (et du géant) tous les 4 pixels

        private readonly SoundDirector sound;
        private readonly int level;
        private int frame = 0;

        public bool Finished { get; private set; }

        // level : le niveau qui vient d'être terminé (pour l'indicateur de niveaux en bas)
        public Intermission1Scene(SoundDirector sound, int level)
        {
            this.sound = sound;
            this.level = level;
        }

        public void Update()
        {
            frame++;

            if (frame == SoundFrame)
            {
                sound.PlayIntermission();
            }

            if (frame >= TotalFrames)
            {
                Finished = true;
            }
        }

        public void Draw(GameRenderer renderer, Canvas g)
        {
            SpriteManager sprites = renderer.Sprites;

            // Pas de bandeau du haut dans cette scène, seulement les fruits des niveaux
            renderer.DrawLevelIndicator(g, level);

            // Partie 1 : Pac-Man, puis Blinky derrière lui, vers la gauche
            if (frame >= PacmanEnterFrame)
            {
                float travelled = PacmanSpeed * (frame - PacmanEnterFrame);
                float x = ScreenWidth - travelled;

                if (x > -16)
                {
                    int mouthFrame = (int)(travelled / PixelsPerMouthFrame) % 4;
                    GameRenderer.DrawSprite(g, sprites.GetPacmanSprite(mouthFrame, Direction.Left), Pixel(x), SpriteY);
                }
            }

            if (frame >= BlinkyEnterFrame)
            {
                float x = ScreenWidth - BlinkySpeed * (frame - BlinkyEnterFrame);

                if (x > -16)
                {
                    GameRenderer.DrawSprite(g, sprites.GetGhostSprite(GhostType.Blinky, Direction.Left, LegFrame(RedLegPhase)), Pixel(x), SpriteY);
                }
            }

            // Partie 2 : Blinky bleu, puis le Pac-Man géant derrière lui, vers la droite
            if (frame >= FrightenedEnterFrame)
            {
                float x = -16 + FrightenedSpeed * (frame - FrightenedEnterFrame);

                if (x < ScreenWidth)
                {
                    GameRenderer.DrawSprite(g, sprites.GetGhostFrightenedSprite(LegFrame(BlueLegPhase)), Pixel(x), SpriteY);
                }
            }

            if (frame >= GiantEnterFrame)
            {
                float travelled = GiantSpeed * (frame - GiantEnterFrame);
                float x = -32 + travelled;

                if (x < ScreenWidth)
                {
                    int mouthFrame = (int)(travelled / PixelsPerMouthFrame) % 4;
                    GameRenderer.DrawSprite(g, sprites.GetGiantPacmanSprite(mouthFrame), Pixel(x), GiantY);
                }
            }
        }

        private static int Pixel(float x)
        {
            return (int)Math.Floor(x);
        }

        private int LegFrame(float phase)
        {
            int k = (int)Math.Floor((frame - phase) / LegPeriod);
            return (k % 2 == 0) ? 1 : 0;
        }
    }
}