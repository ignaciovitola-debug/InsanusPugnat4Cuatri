using System;
using System.Collections.Generic;

namespace GladiusAI
{
    // Foto de los gladiadores y del tutorial visto (la crea y la usa GladiatorRoster).
    [Serializable]
    public struct RosterMemento
    {
        public List<GladiatorRosterEntry> slots;
        public int tutorialVersion;
        public bool arenaTutorialDone;
    }
}
