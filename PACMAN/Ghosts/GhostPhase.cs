namespace PacmanWinForms
{
    public enum GhostPhase
    {
        InHouse, // Dans le nid
        Exiting, // En train de franchir la porte
        Active,  // Dans le labyrinthe
        Eaten    // Yeux seuls, retour au nid
    }
}