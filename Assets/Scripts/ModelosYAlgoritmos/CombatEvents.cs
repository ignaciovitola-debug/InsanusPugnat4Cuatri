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

    // Quienes pelean ahora (cambia con cada rival nuevo). Lo usa HealthBarsHud para mostrar las barras de vida.
    public readonly struct CombatantsChangedEvent
    {
        public readonly GladiatorNPC player;
        public readonly GladiatorNPC enemy;
        public readonly GladiatorNPC enemy2;
        public readonly string playerName;
        public readonly string enemyName;

        public CombatantsChangedEvent(GladiatorNPC player, GladiatorNPC enemy, GladiatorNPC enemy2, string playerName, string enemyName)
        {
            this.player = player;
            this.enemy = enemy;
            this.enemy2 = enemy2;
            this.playerName = playerName;
            this.enemyName = enemyName;
        }
    }
}
