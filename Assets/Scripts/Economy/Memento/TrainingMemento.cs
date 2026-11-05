using System;

namespace GladiusAI
{
    // Foto del entrenamiento en curso (la crea y la usa Training).
    [Serializable]
    public struct TrainingMemento
    {
        public int slot;
        public long endTicks;
        public int tier;
    }
}
