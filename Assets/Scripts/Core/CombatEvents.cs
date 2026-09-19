namespace GladiusAI
{
    public enum CombatResult { Victory, Defeat, Surrender }

    /// <summary>Se publica cuando un gladiador (jugador o enemigo) muere en combate.</summary>
    public readonly struct GladiatorDiedEvent
    {
        public readonly string gladiatorName;

        public GladiatorDiedEvent(string gladiatorName)
        {
            this.gladiatorName = gladiatorName;
        }
    }

    /// <summary>Se publica una sola vez, cuando el combate completo (todas las oleadas) termina.</summary>
    public readonly struct CombatEndedEvent
    {
        public readonly CombatResult result;

        public CombatEndedEvent(CombatResult result)
        {
            this.result = result;
        }
    }

    /// <summary>Se publica justo cuando termina el countdown y el combate arranca de verdad — cualquier elemento que deba esperar (ej. ArenaBeast) se suscribe a esto en vez de moverse desde el arranque de la escena.</summary>
    public readonly struct CombatStartedEvent
    {
    }
}
