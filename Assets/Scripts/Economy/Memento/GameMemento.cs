using System;

namespace GladiusAI
{
    // Junta las fotos de todos los originadores. Es lo que SaveService guarda en el archivo.
    [Serializable]
    public class GameMemento
    {
        public WalletMemento wallet;
        public EnergyMemento energy;
        public TrainingMemento training;
        public RosterMemento roster;
        public BloodMemento blood;
    }
}
