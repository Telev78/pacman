using System.Collections.Generic;
using System.Linq;

namespace PacmanWinForms
{
    /// <summary>
    /// Gère la sortie des fantômes du nid : compteurs individuels, compteur global (après une mort)
    /// et règle d'inactivité. Blinky n'est jamais dans le nid.
    /// </summary>
    public class GhostHouse
    {
        // Ordre de priorité immuable de l'arcade
        private static readonly GhostType[] ReleaseOrder = { GhostType.Pinky, GhostType.Inky, GhostType.Clyde };

        private readonly IReadOnlyList<Ghost> ghosts;
        private readonly Dictionary<GhostType, int> dotLimits = new Dictionary<GhostType, int>();
        private readonly Dictionary<GhostType, int> dotCounters = new Dictionary<GhostType, int>();

        private bool useGlobalCounter = false;
        private int globalCounter = 0;
        private int inactivityFrames = 0;

        public GhostHouse(IReadOnlyList<Ghost> ghosts)
        {
            this.ghosts = ghosts;

            foreach (GhostType t in ReleaseOrder)
            {
                dotLimits[t] = 0;
                dotCounters[t] = 0;
            }
        }

        // Début de niveau ou nouvelle vie.
        // Après une mort : les compteurs individuels sont conservés et le compteur global prend le relais.
        public void Reset(int level, bool afterDeath)
        {
            int inkyLimit, clydeLimit;
            if (level == 1) { inkyLimit = 30; clydeLimit = 60; }
            else if (level == 2) { inkyLimit = 0; clydeLimit = 50; }
            else { inkyLimit = 0; clydeLimit = 0; }

            dotLimits[GhostType.Pinky] = 0;
            dotLimits[GhostType.Inky] = inkyLimit;
            dotLimits[GhostType.Clyde] = clydeLimit;

            if (!afterDeath)
            {
                foreach (GhostType t in ReleaseOrder)
                {
                    dotCounters[t] = 0;
                }
            }

            useGlobalCounter = afterDeath;
            globalCounter = 0;
            inactivityFrames = 0;
        }

        // À appeler chaque fois que Pac-Man mange une gomme (normale ou super-gomme)
        public void OnDotEaten()
        {
            inactivityFrames = 0; // Une gomme est mangée : on remet à zéro le timer d'inactivité

            if (useGlobalCounter)
            {
                globalCounter++;

                Ghost pinky = FindInHouse(GhostType.Pinky);
                Ghost inky = FindInHouse(GhostType.Inky);
                Ghost clyde = FindInHouse(GhostType.Clyde);

                // Règle post-mort : Pinky sort à 7, Inky à 17, Clyde à 32
                if (globalCounter == 7 && pinky != null) pinky.Phase = GhostPhase.Exiting;
                if (globalCounter == 17 && inky != null) inky.Phase = GhostPhase.Exiting;

                // À 32, Clyde sort et on repasse aux compteurs individuels
                if (globalCounter == 32 && clyde != null)
                {
                    clyde.Phase = GhostPhase.Exiting;
                    useGlobalCounter = false;
                    globalCounter = 0;
                }
            }
            else
            {
                Ghost preferred = GetPreferredGhostInHouse();
                if (preferred != null)
                {
                    dotCounters[preferred.Type]++;
                }
            }
        }

        // Fait sortir le fantôme prioritaire si son compteur a atteint sa limite.
        // Un seul fantôme sort à la fois.
        public void ReleaseByCounter()
        {
            if (useGlobalCounter) return;
            if (ghosts.Any(g => g.Phase == GhostPhase.Exiting)) return;

            Ghost preferred = GetPreferredGhostInHouse();
            if (preferred != null)
            {
                GhostType t = preferred.Type;
                if (dotCounters[t] >= dotLimits[t])
                {
                    preferred.Phase = GhostPhase.Exiting;
                }
            }
        }

        // À appeler à chaque frame de jeu (Pac-Man en train de jouer)
        public void Update(int level)
        {
            ReleaseByCounter();

            Ghost preferred = GetPreferredGhostInHouse();
            if (preferred == null)
            {
                inactivityFrames = 0; // Personne dans le nid
                return;
            }

            inactivityFrames++;

            // Règle d'inactivité : 4 secondes aux niveaux 1-4, 3 secondes ensuite
            int limitFrames = ((level < 5) ? 4 : 3) * GameConfig.FramesPerSecond;
            if (inactivityFrames >= limitFrames)
            {
                inactivityFrames = 0;
                preferred.Phase = GhostPhase.Exiting; // Sortie forcée !
            }
        }

        private Ghost GetPreferredGhostInHouse()
        {
            foreach (GhostType t in ReleaseOrder)
            {
                Ghost g = ghosts.FirstOrDefault(x => x.Type == t);
                if (g != null && g.Phase == GhostPhase.InHouse)
                {
                    return g;
                }
            }
            return null;
        }

        private Ghost FindInHouse(GhostType type)
        {
            return ghosts.FirstOrDefault(g => g.Type == type && g.Phase == GhostPhase.InHouse);
        }

        public void SaveCounters(PlayerState player)
        {
            for (int i = 0; i < ReleaseOrder.Length; i++)
                player.HouseDotCounters[i] = dotCounters[ReleaseOrder[i]];
        }

        public void LoadCounters(PlayerState player)
        {
            for (int i = 0; i < ReleaseOrder.Length; i++)
                dotCounters[ReleaseOrder[i]] = player.HouseDotCounters[i];
        }
    }
}