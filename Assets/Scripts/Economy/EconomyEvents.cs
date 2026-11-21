namespace GladiusAI
{
    public readonly struct DenariiChangedEvent
    {
        public readonly int balance;

        public DenariiChangedEvent(int balance)
        {
            this.balance = balance;
        }
    }

    public readonly struct EnergyChangedEvent
    {
        public readonly int current;

        public EnergyChangedEvent(int current)
        {
            this.current = current;
        }
    }

    // Termino un entrenamiento (la Ludus muestra lo que salio).
    public readonly struct TrainingCompletedEvent
    {
        public readonly int slot;
        public readonly TrainingOutcome outcome;

        public TrainingCompletedEvent(int slot, TrainingOutcome outcome)
        {
            this.slot = slot;
            this.outcome = outcome;
        }
    }

    // Un originador avisa que su estado cambio y hay que guardar.
    public readonly struct SaveRequestedEvent
    {
    }
}
