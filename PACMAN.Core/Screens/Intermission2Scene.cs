using System;

namespace PacmanWinForms
{
    /// <summary>
    /// Intermission 2 (après le niveau 5) : Blinky poursuit Pac-Man vers la gauche, sa tunique s'accroche à un clou
    /// planté dans le sol, s'étire de plus en plus, puis se déchire. Blinky reste là, un trou dans la tunique,
    /// et regarde le morceau resté accroché. Valeurs mesurées image par image sur une vidéo de l'arcade (59,94 images/s).
    /// </summary>
    public sealed class Intermission2Scene : IIntermission
    {
        private const int ScreenWidth = 224;

        // Positions (coin haut-gauche des sprites 16x16)
        private const int SpriteY = 20 * GameConfig.TileSize - 4;   // Pac-Man et Blinky
        private const int ClothX = 111;     // Le clou et tous les morceaux de tunique partagent ce coin
        private const int ClothY = 155;     // (la tunique est attachée au clou, 1 pixel plus haut que Blinky)
        private const int RippedGhostX = 102;

        // Pac-Man entre par la droite, puis Blinky 88 images plus tard, plus vite que lui
        private const float PacmanEnterFrame = 16.4f;
        private const float PacmanSpeed = 1.227f;
        private const float BlinkyEnterFrame = 104.5f;
        private const float BlinkySpeed = 1.283f;

        // Blinky accroche le clou : il ralentit brusquement et avance d'un pixel de temps en temps,
        // la tunique s'étire. x de départ de ce ralentissement, puis les images où il avance d'un pixel
        private const int StretchStartFrame = 190;
        private const int StretchStartX = 114;
        private static readonly int[] StretchStepFrames = { 191, 195, 202, 207, 213, 219, 223, 228, 235, 240 };

        // Après 4 pas la tunique s'étire avec le 1er sprite, après 8 pas avec le 2e, après 9 pas avec le 3e
        private const int FirstClothStep = 4;
        private const int SecondClothStep = 8;
        private const int ThirdClothStep = 9;

        private const int NailHiddenFrame = 186;        // Blinky passe sur le clou : on ne le voit plus
        private const int TearFrame = 326;              // La tunique se déchire
        private const int LookDownFrame = 385;          // Blinky baisse les yeux vers le morceau accroché
        private const int BlackFrame = 503;             // L'écran devient noir (tout, y compris les fruits)
        private const int TotalFrames = 511;            // Fin de la musique
        private const int SoundFrame = 5;

        // Jambes de Blinky : changent toutes les 8,3 images, figées quand il ne bouge plus
        private const float LegPeriod = 8.3f;
        private const float LegPhase = 2f;
        private const int LegFreezeFrame = 240;

        private const int PixelsPerMouthFrame = 4;      // Bouche de Pac-Man tous les 4 pixels

        private readonly SoundDirector sound;
        private readonly int level;
        private int frame = 0;

        public bool Finished { get; private set; }

        // level : le niveau qui vient d'être terminé (pour l'indicateur de niveaux en bas)
        public Intermission2Scene(SoundDirector sound, int level)
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
            // La dernière partie de la scène est un écran entièrement noir
            if (frame >= BlackFrame) return;

            SpriteManager sprites = renderer.Sprites;

            renderer.DrawLevelIndicator(g, level);

            // Le clou, sous tout le reste (Pac-Man passe devant)
            if (frame < NailHiddenFrame)
            {
                GameRenderer.DrawSprite(g, sprites.GetSheetSprite(128, 177), ClothX, ClothY);
            }

            // Pac-Man vers la gauche
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

            if (frame >= TearFrame)
            {
                DrawAfterTear(sprites, g);
            }
            else if (frame >= BlinkyEnterFrame)
            {
                DrawBlinky(sprites, g);
            }
        }

        // Blinky qui court, puis qui se retrouve retenu par sa tunique
        private void DrawBlinky(SpriteManager sprites, Canvas g)
        {
            int steps = 0;
            int x;

            if (frame < StretchStartFrame)
            {
                x = Pixel(ScreenWidth - BlinkySpeed * (frame - BlinkyEnterFrame));
            }
            else
            {
                foreach (int stepFrame in StretchStepFrames)
                {
                    if (frame >= stepFrame) steps++;
                }
                x = StretchStartX - steps;
            }

            // La tunique étirée est dessinée par-dessous, attachée au clou
            if (steps >= ThirdClothStep)
            {
                GameRenderer.DrawSprite(g, sprites.GetSheetSprite(176, 177), ClothX, ClothY);
            }
            else if (steps >= SecondClothStep)
            {
                GameRenderer.DrawSprite(g, sprites.GetSheetSprite(160, 177), ClothX, ClothY);
            }
            else if (steps >= FirstClothStep)
            {
                GameRenderer.DrawSprite(g, sprites.GetSheetSprite(144, 177), ClothX, ClothY);
            }

            int legFrame = LegFrame();
            GameRenderer.DrawSprite(g, sprites.GetGhostSprite(GhostType.Blinky, Direction.Left, legFrame), x, SpriteY);

            
        }

        // Blinky troué (il regarde d'abord vers le haut, puis vers le bas) et le morceau de tunique resté au clou
        private void DrawAfterTear(SpriteManager sprites, Canvas g)
        {
            int spriteX = (frame >= LookDownFrame) ? 144 : 128;
            GameRenderer.DrawSprite(g, sprites.GetSheetSprite(spriteX, 193), RippedGhostX, SpriteY);
            GameRenderer.DrawSprite(g, sprites.GetSheetSprite(192, 177), ClothX, ClothY);
        }

        private int LegFrame()
        {
            int f = Math.Min(frame, LegFreezeFrame);
            int k = (int)Math.Floor((f - LegPhase) / LegPeriod);
            return (k % 2 == 0) ? 1 : 0;
        }

        private static int Pixel(float x)
        {
            return (int)Math.Floor(x);
        }
    }
}