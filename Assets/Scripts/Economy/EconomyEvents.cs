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

    // Un originador avisa que su estado cambio y hay que guardar.
    public readonly struct SaveRequestedEvent
    {
    }
}
