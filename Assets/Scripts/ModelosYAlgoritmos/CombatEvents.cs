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

    // Un gladiador recibió un golpe que le sacó vida (los bloqueados no cuentan). Lo usa BloodField.
    public readonly struct GladiatorHitEvent
    {
        public readonly GladiatorNPC victim;
        public readonly UnityEngine.Vector3 attackerPosition;
        public readonly bool killingBlow;

        public GladiatorHitEvent(GladiatorNPC victim, UnityEngine.Vector3 attackerPosition, bool killingBlow)
        {
            this.victim = victim;
            this.attackerPosition = attackerPosition;
            this.killingBlow = killingBlow;
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
