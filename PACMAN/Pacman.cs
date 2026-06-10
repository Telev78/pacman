using System;
using System.Drawing;
using System.Windows.Forms;

namespace PacmanWinForms
{
    public class Pacman
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Speed { get; private set; } = 1.0f; // 1 pixel par tick pour rester synchro avec la grille de 8

        public Keys CurrentDirection { get; set; }
        public Keys NextDirection { get; set; }

        public int AnimFrame { get; private set; } = 0;
        private int animTick = 0;
        private const int TicksPerFrame = 4;

        public Pacman()
        {
            X = 104; // Colonne 13 (13 * 8 = 104)
            Y = 208; // Ligne 26 (26 * 8 = 208) -> Parfaitement aligné !

            CurrentDirection = Keys.Left;
            NextDirection = Keys.None;
        }

        // On passe la carte en paramètre pour analyser l'environnement
        public void Move(LevelMap map)
        {
            // 1. Est-on parfaitement aligné sur une tuile de 8x8 ?
            bool isAlignedWithGrid = (X % 8 == 0) && (Y % 8 == 0);

            if (isAlignedWithGrid)
            {
                int currentTileX = (int)(X / 8);
                int currentTileY = (int)(Y / 8);

                // Essayer d'appliquer le changement de direction voulu par le joueur
                if (NextDirection != Keys.None)
                {
                    if (CanMoveInDirection(currentTileX, currentTileY, NextDirection, map))
                    {
                        CurrentDirection = NextDirection;
                        NextDirection = Keys.None;
                    }
                }

                // Vérifier si on va percuter un mur dans notre direction actuelle
                if (!CanMoveInDirection(currentTileX, currentTileY, CurrentDirection, map))
                {
                    // Un mur bloque le passage ! On immobilise Pac-Man sur le centre de la tuile.
                    return;
                }

                if (this.X < -8) // On utilise une petite marge pour qu'il disparaisse bien
                {
                    this.X = 224 + 8; // On le fait réapparaître à droite
                }
                // Si Pac-Man dépasse le bord droit
                else if (this.X > 224 + 8)
                {
                    this.X = -8; // On le fait réapparaître à gauche
                }
            }
            else
            {
                // Règle de l'arcade : Si le joueur demande un demi-tour immédiat (ex: va à droite alors qu'il avance à gauche),
                // on n'attend pas d'être aligné sur la grille, on l'autorise direct.
                if (IsOppositeDirection(CurrentDirection, NextDirection))
                {
                    CurrentDirection = NextDirection;
                    NextDirection = Keys.None;
                }
            }

            // 2. Déplacement physique si la voie est libre
            switch (CurrentDirection)
            {
                case Keys.Up: Y -= Speed; break;
                case Keys.Down: Y += Speed; break;
                case Keys.Left: X -= Speed; break;
                case Keys.Right: X += Speed; break;
            }

            // 3. Animation de la bouche (uniquement s'il bouge)
            animTick++;
            if (animTick >= TicksPerFrame)
            {
                animTick = 0;
                AnimFrame = (AnimFrame + 1) % 4;
            }
        }

        // Méthode de secours pour valider si la case suivante est praticable
        private bool CanMoveInDirection(int tileX, int tileY, Keys dir, LevelMap map)
        {
            int targetX = tileX;
            int targetY = tileY;

            switch (dir)
            {
                case Keys.Up: targetY--; break;
                case Keys.Down: targetY++; break;
                case Keys.Left: targetX--; break;
                case Keys.Right: targetX++; break;
                default: return false;
            }

            // Sécurité pour éviter de sortir des index du tableau (ex: dans le tunnel)
            if (targetX < 0 || targetX >= 28 || targetY < 0 || targetY >= 36)
            {
                // CORRECTIF : Hors-limites, on valide le mouvement UNIQUE-MENT si on reste sur l'axe du tunnel (ligne 17)
                // et qu'on progresse de gauche à droite ou de droite à gauche.
                return (targetY == 17 && (dir == Keys.Left || dir == Keys.Right));
            }

            // On ne peut PAS marcher sur un mur (1). La porte du nid (4) est aussi interdite à Pac-Man.
            return map.Grid[targetY, targetX] != 1 && map.Grid[targetY, targetX] != 4;
        }

        // Vérifie si la nouvelle direction est l'exact opposé de l'actuelle
        private bool IsOppositeDirection(Keys dir1, Keys dir2)
        {
            if (dir1 == Keys.Left && dir2 == Keys.Right) return true;
            if (dir1 == Keys.Right && dir2 == Keys.Left) return true;
            if (dir1 == Keys.Up && dir2 == Keys.Down) return true;
            if (dir1 == Keys.Down && dir2 == Keys.Up) return true;
            return false;
        }
    }
}