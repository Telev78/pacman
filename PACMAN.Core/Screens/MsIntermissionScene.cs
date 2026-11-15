using static PacmanWinForms.SpriteManager;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace PacmanWinForms
{
    /// <summary>
    /// Intermissions 1, 2 et 3 de Ms Pac-Man (« They meet », « The chase », « Junior »).
    /// Les positions des sprites sont lues dans MsIntermissionData (mesures image par image sur les vidéos
    /// de l'arcade) ; la scène gère la claquette, le texte, les fruits de niveau et les sons.
    /// </summary>
    public sealed class MsIntermissionScene : IIntermission
    {
        private const int ClapX = 45;
        private const int ClapY = 90;
        private static readonly int[] DigitX = { 65, 66, 66 };
        private const int DigitY = 113;
        private const int ClipLeft = 4;      // L'arcade n'affiche pas les 4 pixels de chaque bord
        private const int ClipRight = 220;

        private static readonly string[] Titles = { "THEY MEET", "THE CHASE", "JUNIOR" };
        private static readonly int[] TotalFrames = { 611, 1284, 504 };
        private static readonly int[] TitleLastFrame = { 55, 56, 55 };
        private const int ClapperEndFrame = 86;
        private const int BumpFrame = 487;   // Acte 1 : les deux fantômes se percutent

        private sealed class Step
        {
            public int Sprite;
            public int X;
            public int Y;
        }

        private sealed class Track
        {
            public int Start;
            public readonly List<Step> Steps = new List<Step>();
        }

        private readonly SoundDirector sound;
        private readonly int level;
        private readonly int number;      // 1, 2 ou 3
        private readonly List<Track> tracks = new List<Track>();
        private int frame = 0;

        public bool Finished { get; private set; }

        public MsIntermissionScene(int number, int level, SoundDirector sound)
        {
            this.number = Math.Max(1, Math.Min(3, number));
            this.level = level;
            this.sound = sound;
            Parse(MsIntermissionData.Tracks[this.number - 1]);
        }

        private void Parse(string[] data)
        {
            foreach (string line in data)
            {
                int colon = line.IndexOf(':');
                Track track = new Track { Start = int.Parse(line.Substring(0, colon), CultureInfo.InvariantCulture) };
                string[] tokens = line.Substring(colon + 1).Split(' ');
                int x = 0, y = 0, sprite = 0;
                for (int i = 0; i < tokens.Length; i++)
                {
                    string[] p = tokens[i].Split(',');
                    if (i == 0)
                    {
                        sprite = int.Parse(p[0], CultureInfo.InvariantCulture);
                        x = int.Parse(p[1], CultureInfo.InvariantCulture);
                        y = int.Parse(p[2], CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        x += int.Parse(p[0], CultureInfo.InvariantCulture);
                        y += int.Parse(p[1], CultureInfo.InvariantCulture);
                        if (p.Length > 2) sprite = int.Parse(p[2], CultureInfo.InvariantCulture);
                    }
                    track.Steps.Add(new Step { Sprite = sprite, X = x, Y = y });
                }
                tracks.Add(track);
            }
        }

        public void Update()
        {
            frame++;

            if (frame == 1)
            {
                sound.PlayMsIntermission(number);
            }

            if (number == 1 && frame == BumpFrame)
            {
                sound.PlayIntermissionBump();
            }

            if (frame >= TotalFrames[number - 1])
            {
                Finished = true;
            }
        }

        public void Draw(GameRenderer renderer, Canvas g)
        {
            SpriteManager sprites = renderer.Sprites;

            renderer.DrawLevelIndicator(g, level);

            DrawClapper(renderer, g, sprites);

            foreach (Track track in tracks)
            {
                int i = frame - track.Start;
                if (i < 0 || i >= track.Steps.Count) continue;
                Step s = track.Steps[i];
                int[] r = MsIntermissionData.Palette[s.Sprite];
                DrawClipped(g, sprites, r[0], r[1], r[2], r[3], s.X, s.Y);
            }
        }

        private void DrawClapper(GameRenderer renderer, Canvas g, SpriteManager sprites)
        {
            if (frame < 1 || frame > ClapperEndFrame) return;

            // Bras de la claquette : ouvert (1-46), mi-ouvert (47-52), fermé (53-58), ouvert à nouveau
            int clap = frame <= 46 ? 0 : frame <= 52 ? 1 : frame <= 58 ? 2 : 0;
            GameRenderer.DrawSprite(g, sprites.GetWideSheetSprite(32 * clap, 290, 32, 32), ClapX, ClapY);

            // Chiffres 1, 2, 3 : cases de 8x8 en (96, 314), (104, 314), (112, 314)
            GameRenderer.DrawSprite(g, sprites.GetWideSheetSprite(88 + 8 * number, 314, 8, 8), DigitX[number - 1], DigitY);

            if (frame <= TitleLastFrame[number - 1])
            {
                string text = Titles[number - 1];
                for (int i = 0; i < text.Length; i++)
                {
                    GameRenderer.DrawSprite(g, sprites.GetArcadeCharacter(text[i], ArcadeColor.White), 80 + i * GameConfig.TileSize, 103);
                }
            }
        }

        // Dessine un sprite en le découpant aux bords visibles de l'écran de l'arcade
        private static void DrawClipped(Canvas g, SpriteManager sprites, int sx, int sy, int w, int h, int x, int y)
        {
            int x0 = Math.Max(x, ClipLeft);
            int x1 = Math.Min(x + w, ClipRight);
            if (x1 <= x0) return;
            Sprite sprite = sprites.GetWideSheetSprite(sx + (x0 - x), sy, x1 - x0, h);
            GameRenderer.DrawSprite(g, sprite, x0, y);
        }
    }
}
