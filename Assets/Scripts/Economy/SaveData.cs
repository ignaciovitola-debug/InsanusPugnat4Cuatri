using System;
using System.Collections.Generic;

namespace GladiusAI
{
    // Todo lo que se guarda del jugador, en un solo archivo JSON (ver SaveService).
    [Serializable]
    public class SaveData
    {
        public int denarii;
        public int energy;
        public long energyStampTicks;
        public List<GladiatorRosterEntry> roster = new List<GladiatorRosterEntry>();
        public bool ludusTutorialSeen;
        public int skinsVersion;
        public int starterGift;
        public int ludusTutorialVersion;

        // Entrenamiento en curso: que puesto del roster, hasta cuando (hora UTC) y que duracion se eligio.
        public int trainingSlot = -1;
        public long trainingEndTicks;
        public int trainingTier;
    }
}
