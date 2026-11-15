using System;
using System.Drawing;

namespace PacmanWinForms
{
    /// <summary>
    /// Fruit de Ms Pac-Man : il entre par un tunnel, rejoint le nid des fantômes, en fait le tour complet dans le sens
    /// des aiguilles d'une montre (sans jamais faire demi-tour), puis quitte le labyrinthe par un autre tunnel.
    /// Il apparaît après 64 et 176 gommes mangées (le second est ignoré si le premier est encore à l'écran).
    /// Sept fruits aux niveaux 1 à 7, puis un fruit tiré au hasard (mais fixe pour un niveau donné).
    /// </summary>
    public sealed class MsFruitManager : FruitManager
    {
        private static readonly int[] FruitValues = { 100, 200, 500, 700, 1000, 2000, 5000 };

        private const float FruitSpeed = 0.5f;             // pixels par frame
        private const int TileSize = GameConfig.TileSize;
        private const int BounceFrames = 16;

        // Boucle autour du nid : rectangle de couloirs à 2 cases de l'intérieur de la maison (déduit de la porte)
        private int ringLeft, ringRight, ringTop, ringBottom;
        private Point[] ring = new Point[0];

        private enum Stage { Approach, Lap, Leave }

        // Position logique (coin haut-gauche de la boîte 8x8, comme les fantômes)
        private float fx, fy;
        private Direction direction = Direction.Left;
        private float accumulator;
        private int bounceFrames;
        private Stage stage;
        private int lapStepsLeft;
        private Point entryTunnel;      // Tunnel d'entrée (colonne -1 ou largeur, ligne) : le fruit n'en repart pas
        private Point exitTile;

        // Cherche l'intérieur de la maison depuis la porte, puis construit la boucle (sens horaire). Faux si impossible.
        private bool BuildRing(LevelMap map)
        {
            ring = new Point[0];
            Point door = new Point(-1, -1);
            for (int y = 0; y < GameConfig.MapHeight && door.X < 0; y++)
                for (int x = 0; x < GameConfig.MapWidth; x++)
                    if (map.Grid[y, x] == TileType.GhostDoor) { door = new Point(x, y); break; }
            if (door.X < 0) return false;

            // Remplissage depuis la case sous la porte (la maison est fermée par des murs)
            var seen = new bool[GameConfig.MapHeight, GameConfig.MapWidth];
            var stack = new System.Collections.Generic.Stack<Point>();
            stack.Push(new Point(door.X, door.Y + 1));
            int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1, count = 0;
            while (stack.Count > 0)
            {
                Point p = stack.Pop();
                if (p.X < 0 || p.X >= GameConfig.MapWidth || p.Y < 0 || p.Y >= GameConfig.MapHeight) return false;
                if (seen[p.Y, p.X]) continue;
                TileType t = map.Grid[p.Y, p.X];
                if (t == TileType.Wall || t == TileType.GhostDoor) continue;
                seen[p.Y, p.X] = true;
                if (++count > 200) return false;   // Fuite : la zone n'est pas fermée
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
                stack.Push(new Point(p.X + 1, p.Y)); stack.Push(new Point(p.X - 1, p.Y));
                stack.Push(new Point(p.X, p.Y + 1)); stack.Push(new Point(p.X, p.Y - 1));
            }

            ringLeft = minX - 2; ringRight = maxX + 2; ringTop = door.Y - 1; ringBottom = maxY + 2;
            if (ringLeft < 0 || ringRight >= GameConfig.MapWidth || ringBottom >= GameConfig.MapHeight || ringTop < 0) return false;

            var list = new System.Collections.Generic.List<Point>();
            for (int x = ringLeft; x < ringRight; x++) list.Add(new Point(x, ringTop));
            for (int y = ringTop; y < ringBottom; y++) list.Add(new Point(ringRight, y));
            for (int x = ringRight; x > ringLeft; x--) list.Add(new Point(x, ringBottom));
            for (int y = ringBottom; y > ringTop; y--) list.Add(new Point(ringLeft, y));

            foreach (Point p in list)
            {
                if (map.Grid[p.Y, p.X] == TileType.Wall || map.Grid[p.Y, p.X] == TileType.GhostDoor) return false;
            }

            ring = list.ToArray();
            return true;
        }

        public MsFruitManager(int? seed = null) : base(seed)
        {
        }

        /// <summary>Indice du fruit (0 cerise ... 6 banane) d'un niveau.</summary>
        public static int FruitIndexForLevel(int level)
        {
            if (level <= 7) return Math.Max(0, level - 1);

            // Tirage "au hasard" mais identique à chaque partie (l'indicateur de niveau reste cohérent)
            unchecked
            {
                uint h = (uint)level * 2654435761u;
                h ^= h >> 15;
                return (int)((h >> 4) % 7u);
            }
        }

        protected override int FirstFruitDots { get { return 64; } }
        protected override int SecondFruitDots { get { return 176; } }
        protected override bool SecondFruitIgnoredIfFirstActive { get { return true; } }
        protected override bool ExpiresByTimer { get { return false; } }

        protected override bool IsTouchedBy(Pacman pacman)
        {
            return pacman.TileX == Tiles.Of(fx) && pacman.TileY == Tiles.Of(fy);
        }

        protected override void Spawn(int level, LevelMap map)
        {
            int[] rows = map.Definition.TunnelRows;
            if (rows.Length == 0) return;   // Pas de tunnel : pas de fruit

            int row = rows[Rng.Next(rows.Length)];
            bool fromLeft = Rng.Next(2) == 0;

            fy = row * TileSize;
            fx = fromLeft ? -2 * TileSize : (GameConfig.MapWidth + 1) * TileSize;
            direction = fromLeft ? Direction.Right : Direction.Left;
            accumulator = 0;
            bounceFrames = 0;
            entryTunnel = new Point(fromLeft ? -1 : GameConfig.MapWidth, row);

            // Pas de tour possible si un couloir du rectangle est bouché : le fruit repart directement
            stage = BuildRing(map) ? Stage.Approach : Stage.Leave;
            if (stage == Stage.Leave) ChooseExit(map);

            Value = FruitValues[FruitIndexForLevel(level)];
            IsActive = true;
            Sync();
        }

        protected override void MoveFruit(LevelMap map)
        {
            accumulator += FruitSpeed;
            int pixels = (int)accumulator;
            accumulator -= pixels;

            for (int i = 0; i < pixels && IsActive; i++)
            {
                if (Tiles.IsAligned(fx, fy)) Turn(map);

                fx += direction.Dx();
                fy += direction.Dy();

                // Sorti par un bord de l'écran
                if (direction == Direction.Left && fx <= -2 * TileSize) IsActive = false;
                if (direction == Direction.Right && fx >= (GameConfig.MapWidth + 1) * TileSize) IsActive = false;
            }

            // Rebond du fruit toutes les 16 frames (période mesurée sur la vidéo de l'arcade : 0,267 s)
            bounceFrames++;
            if (IsActive && bounceFrames % BounceFrames == 0) HasBounced = true;

            Sync();
        }

        // Le sprite de 16x16 est centré sur la boîte logique
        private void Sync()
        {
            X = (int)fx - 4;
            Y = (int)fy - 4;
        }

        // Un autre tunnel que celui d'entrée, au hasard
        private void ChooseExit(LevelMap map)
        {
            var options = new System.Collections.Generic.List<Point>();
            foreach (int row in map.Definition.TunnelRows)
            {
                foreach (int col in new[] { -1, GameConfig.MapWidth })
                {
                    Point p = new Point(col, row);
                    if (p != entryTunnel) options.Add(p);
                }
            }
            exitTile = options.Count > 0 ? options[Rng.Next(options.Count)] : entryTunnel;
        }

        private bool IsOnRing(int x, int y)
        {
            bool inside = x >= ringLeft && x <= ringRight && y >= ringTop && y <= ringBottom;
            bool border = x == ringLeft || x == ringRight || y == ringTop || y == ringBottom;
            return inside && border;
        }

        private Direction RingDirectionAt(int x, int y)
        {
            for (int i = 0; i < ring.Length; i++)
            {
                if (ring[i].X != x || ring[i].Y != y) continue;
                Point next = ring[(i + 1) % ring.Length];
                if (next.X > x) return Direction.Right;
                if (next.X < x) return Direction.Left;
                return next.Y > y ? Direction.Down : Direction.Up;
            }
            return Direction.None;
        }

        // Appelé à chaque case : choisit la direction selon la partie du trajet (rejoindre le nid, en faire le tour, partir)
        private void Turn(LevelMap map)
        {
            int tx = Tiles.Of(fx);
            int ty = Tiles.Of(fy);

            // Dans le tunnel hors du labyrinthe : tout droit
            if (tx < 0 || tx >= GameConfig.MapWidth) return;

            if (stage == Stage.Approach && IsOnRing(tx, ty))
            {
                stage = Stage.Lap;
                lapStepsLeft = ring.Length;   // Un tour complet, retour à la case d'arrivée
            }

            if (stage == Stage.Lap)
            {
                if (lapStepsLeft > 0)
                {
                    lapStepsLeft--;
                    direction = RingDirectionAt(tx, ty);
                    return;
                }

                stage = Stage.Leave;
                ChooseExit(map);
            }

            Direction step = (stage == Stage.Approach)
                ? FirstStepToward(map, tx, ty, (x, y) => IsOnRing(x, y))
                : FirstStepToward(map, tx, ty, (x, y) => x == exitTile.X && y == exitTile.Y);

            if (step != Direction.None) direction = step;

            // Sécurité : ne jamais traverser un mur
            if (!CanEnter(map, tx + direction.Dx(), ty + direction.Dy(), direction))
            {
                Direction[] dirs = { direction.Opposite(), Direction.Up, Direction.Left, Direction.Down, Direction.Right };
                foreach (Direction d in dirs)
                {
                    if (d != direction.Opposite() && CanEnter(map, tx + d.Dx(), ty + d.Dy(), d)) { direction = d; return; }
                }
                direction = direction.Opposite();
            }
        }

        // Plus court chemin sans demi-tour (largeur d'abord) : renvoie la première direction à prendre
        private Direction FirstStepToward(LevelMap map, int startX, int startY, System.Func<int, int, bool> isGoal)
        {
            Direction[] dirs = { Direction.Up, Direction.Left, Direction.Down, Direction.Right };
            int width = GameConfig.MapWidth + 2;      // Colonnes -1 à MapWidth
            int height = GameConfig.MapHeight;
            var seen = new bool[width, height, 5];
            var queue = new System.Collections.Generic.Queue<int[]>();

            seen[startX + 1, startY, (int)direction] = true;
            queue.Enqueue(new[] { startX, startY, (int)direction, (int)Direction.None });

            while (queue.Count > 0)
            {
                int[] cur = queue.Dequeue();
                Direction heading = (Direction)cur[2];
                Direction first = (Direction)cur[3];

                foreach (Direction dir in dirs)
                {
                    if (dir == heading.Opposite()) continue;
                    int nx = cur[0] + dir.Dx();
                    int ny = cur[1] + dir.Dy();
                    if (!CanEnter(map, nx, ny, dir)) continue;
                    if (ny < 0 || ny >= height) continue;

                    Direction f = first == Direction.None ? dir : first;
                    if (isGoal(nx, ny)) return f;
                    if (nx < 0 || nx >= GameConfig.MapWidth) continue;   // Hors labyrinthe : seulement comme but

                    if (seen[nx + 1, ny, (int)dir]) continue;
                    seen[nx + 1, ny, (int)dir] = true;
                    queue.Enqueue(new[] { nx, ny, (int)dir, (int)f });
                }
            }

            return Direction.None;
        }

        private static bool CanEnter(LevelMap map, int x, int y, Direction dir)
        {
            if (x < 0 || x >= GameConfig.MapWidth || y < 0 || y >= GameConfig.MapHeight)
            {
                // Seul le tunnel mène hors du labyrinthe
                return map.Definition.IsTunnelRow(y) && (dir == Direction.Left || dir == Direction.Right);
            }

            TileType tile = map.Grid[y, x];
            return tile != TileType.Wall && tile != TileType.GhostDoor;
        }
    }
}
