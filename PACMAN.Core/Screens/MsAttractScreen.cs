using static PacmanWinForms.SpriteManager;

namespace PacmanWinForms
{
    /// <summary>
    /// Attract mode de Ms Pac-Man : titre, cadre lumineux, présentation de Blinky, Pinky, Inky et Sue,
    /// puis "STARRING MS PAC-MAN", et enfin la démo. Chronologie, positions et trajets mesurés sur une vidéo
    /// de l'arcade (60,6 images/s) ; frame 0 = premier écran après l'écran de test.
    /// </summary>
    public sealed class MsAttractScreen : IAttractScreen
    {
        // --- Chronologie (frames) ---
        private const int HudFrame = 4;
        private const int TitleFrame = 5;          // Titre, cadre, logo Midway
        private const int WithFrame = 61;          // "WITH"
        private const int StarringFrame = 1054;    // "STARRING" puis "MS PAC-MAN"
        private const int MsPacManEntryFrame = 1066;
        private const int DemoFrame = 1304;

        // --- Cadre lumineux : 96 points de 2x2 pixels (un tous les 4 pixels), numérotés dans le sens inverse des aiguilles
        // d'une montre à partir du coin haut droit ---
        private const int FrameLeft = 60, FrameRight = 192, FrameTop = 88, FrameBottom = 148;
        private const int DotCount = 96;
        private const uint RedDot = 0xFFFF0000;
        private const uint WhiteDot = 0xFFDEDEFF;

        // --- Fantômes : entrent par la droite sous le cadre, vont à gauche puis montent s'empiler ---
        private const int GhostLineY = 156;
        private const int GhostStopX = 40;
        private const int GhostCornerPause = 4;

        private sealed class GhostSlot
        {
            public GhostType Type;
            public ArcadeColor Color;
            public string Name;
            public int NameX;          // Les noms sont placés au pixel près
            public int NameFrame;      // Le nom apparaît ; il disparaît à l'apparition du suivant
            public int EntryFrame;     // Le fantôme apparaît au bord droit de l'écran
            public int RestY;
        }

        private static readonly GhostSlot[] Slots =
        {
            new GhostSlot { Type = GhostType.Blinky, Color = ArcadeColor.Red,    Name = "BLINKY", NameX = 105, NameFrame = 62,  EntryFrame = 73,  RestY = 85 },
            new GhostSlot { Type = GhostType.Pinky,  Color = ArcadeColor.Pink,   Name = "PINKY",  NameX = 104, NameFrame = 334, EntryFrame = 345, RestY = 101 },
            new GhostSlot { Type = GhostType.Inky,   Color = ArcadeColor.Cyan,   Name = "INKY",   NameX = 106, NameFrame = 590, EntryFrame = 601, RestY = 117 },
            new GhostSlot { Type = GhostType.Clyde,  Color = ArcadeColor.Orange, Name = "SUE",    NameX = 113, NameFrame = 830, EntryFrame = 841, RestY = 133 }
        };

        private const int LegAnimationFrames = 6;
        private const int PixelsPerMouthFrame = 4;
        private const int MsPacManStopX = 118;

        private readonly ScreenManager manager;
        private int frame;

        public MsAttractScreen(ScreenManager manager)
        {
            this.manager = manager;
        }

        public void Enter()
        {
            frame = 0;
        }

        public void Update()
        {
            frame++;

            if (frame >= DemoFrame)
            {
                manager.ShowDemo();
            }
        }

        public void OnInput(InputKey key)
        {
            // Rien en attract mode : l'insertion d'un crédit est gérée par le ScreenManager
        }

        public void Draw(GameRenderer renderer, Canvas g)
        {
            int tile = GameConfig.TileSize;

            if (frame >= HudFrame)
            {
                renderer.DrawTopHud(g, 0, manager.HighScore, show1Up: true, show2Up: true);
                renderer.DrawCredits(g, manager.Credits);
            }

            if (frame < TitleFrame) return;

            DrawText(renderer, g, "\"MS PAC-MAN\"", 80, 56, ArcadeColor.Orange);
            DrawMarquee(g);

            // Logo Midway et copyright, comme sur l'écran start
            GameRenderer.DrawSprite(g, renderer.Sprites.GetMidwayLogo(), 6 * tile, 28 * tile);
            renderer.DrawText(g, "\u00A9 MIDWAY MFG CO", 11, 29, ArcadeColor.Red);
            renderer.DrawText(g, "1980/1981", 14, 31, ArcadeColor.Red);

            DrawTexts(renderer, g);
            DrawGhosts(renderer, g);
            DrawMsPacMan(renderer, g);
        }

        // ------------------------------------------------------------------
        // Textes
        // ------------------------------------------------------------------

        private void DrawTexts(GameRenderer renderer, Canvas g)
        {
            if (frame >= StarringFrame)
            {
                DrawText(renderer, g, "STARRING", 80, 104, ArcadeColor.White);
                if (frame >= StarringFrame + 1)
                {
                    DrawText(renderer, g, "MS PAC-MAN", 80, 128, ArcadeColor.Yellow);
                }
                return;
            }

            // "WITH" reste affiché pendant la présentation de Blinky
            if (frame >= WithFrame && frame < Slots[1].NameFrame)
            {
                DrawText(renderer, g, "WITH", 80, 104, ArcadeColor.White);
            }

            for (int i = 0; i < Slots.Length; i++)
            {
                int end = (i + 1 < Slots.Length) ? Slots[i + 1].NameFrame : StarringFrame;
                if (frame >= Slots[i].NameFrame && frame < end)
                {
                    DrawText(renderer, g, Slots[i].Name, Slots[i].NameX, 128, Slots[i].Color);
                }
            }
        }

        private static void DrawText(GameRenderer renderer, Canvas g, string text, int x, int y, ArcadeColor color)
        {
            for (int i = 0; i < text.Length; i++)
            {
                GameRenderer.DrawSprite(g, renderer.Sprites.GetArcadeCharacter(text[i], color), x + i * GameConfig.TileSize, y);
            }
        }

        // ------------------------------------------------------------------
        // Cadre lumineux
        // ------------------------------------------------------------------

        // Position (coin haut-gauche) du point i
        private static void DotPosition(int i, out int x, out int y)
        {
            if (i <= 33) { x = FrameRight - 4 * i; y = FrameTop; }                                 // Haut : vers la gauche
            else if (i <= 48) { x = FrameLeft; y = FrameTop + 4 * (i - 33); }                      // Gauche : vers le bas
            else if (i <= 81) { x = FrameLeft + 4 * (i - 48); y = FrameBottom; }                   // Bas : vers la droite
            else { x = FrameRight; y = FrameBottom - 4 * (i - 81); }                               // Droite : vers le haut
        }

        // Image à laquelle le point i apparaît : six foyers qui s'étendent chacun dans les deux sens
        private static int DotRevealFrame(int i)
        {
            int[] seeds = { 3, 19, 35, 51, 67, 83 };

            // Point juste avant un foyer : il apparaît peu après
            for (int s = 0; s < seeds.Length; s++)
            {
                if (i == seeds[s] - 1 || i == seeds[s] - 2) return 7;
            }

            int best = -1;
            int bestOffset = int.MaxValue;
            for (int s = 0; s < seeds.Length; s++)
            {
                int offset = ((i - seeds[s]) % DotCount + DotCount) % DotCount;
                if (offset < bestOffset) { bestOffset = offset; best = s; }
            }

            if (bestOffset <= 1) return 5;
            int frameOffset = 10 + ((bestOffset - 2) / 2) * 2;
            return System.Math.Min(frameOffset, 20);
        }

        private void DrawMarquee(Canvas g)
        {
            // Six lumières blanches, espacées de 16 points, avancent d'un point par image
            int phase = (((frame - 21) % 16) + 16) % 16;
            bool[] light = new bool[DotCount];
            for (int m = 0; m < DotCount / 16; m++)
            {
                light[phase + 16 * m] = true;
            }

            for (int i = 0; i < DotCount; i++)
            {
                if (frame < DotRevealFrame(i)) continue;

                int x, y;
                DotPosition(i, out x, out y);
                uint color = light[i] ? WhiteDot : RedDot;
                g.SetPixel(x, y, color);
                g.SetPixel(x + 1, y, color);
                g.SetPixel(x, y + 1, color);
                g.SetPixel(x + 1, y + 1, color);
            }
        }

        // ------------------------------------------------------------------
        // Personnages
        // ------------------------------------------------------------------

        private void DrawGhosts(GameRenderer renderer, Canvas g)
        {
            foreach (GhostSlot slot in Slots)
            {
                int t = frame - slot.EntryFrame;
                if (t < 0) continue;

                int runLength = 224 - GhostStopX;                      // 184 pixels vers la gauche
                int upLength = GhostLineY - slot.RestY;
                int x, y;
                Direction direction;
                int legFrame;

                if (t < runLength)
                {
                    x = 224 - t; y = GhostLineY; direction = Direction.Left;
                    legFrame = (t / LegAnimationFrames) % 2;
                }
                else if (t < runLength + GhostCornerPause)
                {
                    x = GhostStopX; y = GhostLineY; direction = Direction.Left;
                    legFrame = 0;
                }
                else
                {
                    int up = t - runLength - GhostCornerPause;
                    x = GhostStopX; y = GhostLineY - System.Math.Min(up, upLength); direction = Direction.Up;
                    legFrame = (up < upLength) ? (t / LegAnimationFrames) % 2 : 0;
                }

                GameRenderer.DrawSprite(g, renderer.Sprites.GetGhostSprite(slot.Type, direction, legFrame), x, y);
            }
        }

        private void DrawMsPacMan(GameRenderer renderer, Canvas g)
        {
            int t = frame - MsPacManEntryFrame;
            if (t < 0) return;

            int x = 224 - t;
            int mouth = (t / PixelsPerMouthFrame) % 4;

            if (x <= MsPacManStopX)
            {
                x = MsPacManStopX;
                mouth = 0;   // Elle s'arrête au centre, bouche mi-ouverte
            }

            GameRenderer.DrawSprite(g, renderer.Sprites.GetPacmanSprite(mouth, Direction.Left), x, GhostLineY);
        }
    }
}
