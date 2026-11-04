namespace GladiusAI
{
    // Un combate elegido en la Ludus: en que zona (escena), contra cuantos rivales, con que fuerza y cuanto paga.
    // Las trampas de cada zona se agregarian aca a futuro.
    public class CombatContract
    {
        public string title;
        public string zoneScene;
        public string zoneName;
        public int enemyCount;
        public float statScale;
        public int reward;
    }

    // El contrato elegido viaja de la Ludus a la escena de combate por esta clase estatica
    // (igual que GladiatorRoster.SelectedIndex).
    public static class CombatSetup
    {
        public static CombatContract Current { get; set; }
    }
}
