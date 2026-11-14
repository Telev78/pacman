namespace PacmanWinForms
{
    /// <summary>
    /// TEMPORAIRE (phase 2) : sert à tester le menu et le changement de jeu. Joue exactement comme Pac-Man ;
    /// remplacé en phase 3 par le vrai Ms Pac-Man (labyrinthes, sprites, règles).
    /// </summary>
    public sealed class MsPacManVariant : PacManVariant
    {
        public override string Id { get { return "mspacman"; } }
        public override string DisplayName { get { return "MS PAC-MAN"; } }
    }
}
