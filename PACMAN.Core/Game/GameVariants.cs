using System;
using System.Collections.Generic;

namespace PacmanWinForms
{
    /// <summary>Les jeux proposés dans le menu, dans l'ordre d'affichage.</summary>
    public static class GameVariants
    {
        private static readonly GameVariant[] all =
        {
            new PacManVariant(),
            new MsPacManVariant()
        };

        public static IReadOnlyList<GameVariant> All { get { return all; } }

        /// <summary>Le jeu portant cet identifiant ("pacman", "mspacman"), ou null (vide, "ASK" ou inconnu : le menu s'affiche).</summary>
        public static GameVariant FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (GameVariant variant in all)
            {
                if (variant.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) return variant;
            }
            return null;
        }
    }
}
