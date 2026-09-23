namespace GladiusAI
{
    public enum CombatResult { Victory, Defeat, Surrender }

    public readonly struct GladiatorDiedEvent
    {
        public readonly string gladiatorName;

        public GladiatorDiedEvent(string gladiatorName)
        {
            this.gladiatorName = gladiatorName;
        }
    }

    public readonly struct CombatEndedEvent
    {
        public readonly CombatResult result;

        public CombatEndedEvent(CombatResult result)
        {
            this.result = result;
        }
    }

    public readonly struct CombatStartedEvent
    {
    }
}
