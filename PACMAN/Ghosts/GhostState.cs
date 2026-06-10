using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacmanWinForms
{
    public enum GhostState
    {
        Scatter,    // Mode patrouille (chacun va dans son coin)
        Chase,      // Mode chasse (attaque)
        Frightened, // Mode effrayé (bleu, après une super-gomme)
        InHouse,    // Mode fantôme dans le nid
        Exiting, // <-- Le fantôme est en train de franchir la porte
        Eaten       // Mode yeux seuls (retour au nid après s'être fait manger)
    }
}
