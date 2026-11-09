using System;
using System.Drawing;

namespace PacmanWinForms
{
    /// <summary>
    /// Intermission 3 (après les niveaux 5, 9, 13 et 17) : Blinky, qui a recousu sa tunique, poursuit Pac-Man
    /// et ils sortent à gauche ; puis Blinky « déshabillé » revient en rampant vers la droite en traînant
    /// sa tunique (sprites de 32 px de large). Valeurs mesurées image par image sur la vidéo de l'arcade.
    /// </summary>
    public sealed class Intermission3Scene : IIntermission
    {
        private const int ScreenWidth = 224;

        private const int SpriteY = 20 * GameConfig.TileSize - 4;   // 156
        private const int CrawlY = SpriteY + 1;                     // Le sprite rampant est dessiné 1 px plus bas

        // --- Partie 1 : Pac-Man puis Blinky entrent par la droite et sortent par la gauche ---
        private const float PacmanEnterFrame = 15f;
        private const float PacmanSpeed = 1.2217f;
        private const float BlinkyEnterFrame = 56.1f;
        private const float BlinkySpeed = 1.2845f;
        private const float LegPeriod = 7.95f;       // Jambes de Blinky (changement toutes les ~7,95 images)
        private const float LegPhase = 78f;          // Image où le sprite (160,193) apparaît

        // --- Partie 2 : Blinky rampant (32x16) entre par la gauche, vers la droite ---
        private const float CrawlEnterFrame = 306.7f;   // Le sprite est entièrement hors écran à gauche (x = -32)
        private const float CrawlSpeed = 1.2763f;
        private const float CrawlPeriod = 6f;           // Alternance des 2 sprites rampants
        private const float CrawlPhase = 340f;

        // La musique (5 s) est jouée deux fois, coupée net à la fin de la scène
        private const int SoundFrame = 18;
        private const int TotalFrames = 533;

        private const int PixelsPerMouthFrame = 4;

        private readonly SoundDirector sound;
        private readonly int level;
        private int frame = 0;

        public bool Finished { get; private set; }

        public Intermission3Scene(SoundDirector sound, int level)
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

        public void Draw(GameRenderer renderer, Graphics g)
        {
            SpriteManager sprites = renderer.Sprites;

            renderer.DrawLevelIndicator(g, level);

            // Partie 1 : Pac-Man, puis Blinky (tunique recousue) derrière lui, vers la gauche
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
                    int leg = (int)Math.Floor((frame - LegPhase) / LegPeriod) & 1;   // 0 -> sprite (160,193), 1 -> (176,193)
                    GameRenderer.DrawSprite(g, sprites.GetWideSheetSprite(160 + 16 * leg, 193, 16, 16), Pixel(x), SpriteY);
                }
            }

            // Partie 2 : Blinky rampant vers la droite en traînant sa tunique
            if (frame >= CrawlEnterFrame)
            {
                float x = -32 + CrawlSpeed * (frame - CrawlEnterFrame);

                if (x < ScreenWidth)
                {
                    int crawl = (int)Math.Floor((frame - CrawlPhase) / CrawlPeriod) & 1;  // 0 -> (128,209), 1 -> (160,209)
                    GameRenderer.DrawSprite(g, sprites.GetWideSheetSprite(128 + 32 * crawl, 209, 32, 16), Pixel(x), CrawlY);
                }
            }
        }

        private static int Pixel(float x)
        {
            return (int)Math.Floor(x);
        }
    }
}