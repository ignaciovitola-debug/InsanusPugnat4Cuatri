namespace GladiusAI
{
    // Un combate elegido en la Ludus: zona, dificultad, rivales y premio.
    public class CombatContract
    {
        public DifficultyStrategy difficulty;
        public string zoneScene;
        public string zoneName;
        public int enemyCount;
        public int reward;
        public float levelMultiplier;
    }

    // Guarda el contrato elegido mientras se pasa de la Ludus a la arena.
    public static class CombatSetup
    {
        public static CombatContract Current { get; set; }
    }
}
