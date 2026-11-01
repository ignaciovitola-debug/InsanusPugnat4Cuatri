namespace GladiusAI
{
    public enum CombatResult { Victory, Defeat, Surrender }

    public readonly struct GladiatorDiedEvent
    {
        public readonly GladiatorNPC gladiator;

        public GladiatorDiedEvent(GladiatorNPC gladiator)
        {
            this.gladiator = gladiator;
        }
    }

    public readonly struct GladiatorSurrenderedEvent
    {
        public readonly GladiatorNPC gladiator;

        public GladiatorSurrenderedEvent(GladiatorNPC gladiator)
        {
            this.gladiator = gladiator;
        }
    }

    public readonly struct CombatEndedEvent
    {
        public readonly CombatResult result;
        public readonly int reward;

        public CombatEndedEvent(CombatResult result, int reward = 0)
        {
            this.result = result;
            this.reward = reward;
        }
    }

    public readonly struct CombatStartedEvent
    {
    }
}
