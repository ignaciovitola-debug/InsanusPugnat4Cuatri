namespace GladiusAI
{
    /// Un gladiador reclutable en la Ludus: nombre + stats de combate (mismo formato que CombatStarter.EnemyWave).
    [System.Serializable]
    public struct GladiatorRosterEntry
    {
        public string label;
        public float maxHP;
        public float minDamage;
        public float maxDamage;
        public float attackCooldown;
        public bool unlocked;
        public int skin;

        public GladiatorRosterEntry(string label, float maxHP, float minDamage, float maxDamage, float attackCooldown, bool unlocked = true, int skin = 0)
        {
            this.skin = skin;
            this.label = label;
            this.maxHP = maxHP;
            this.minDamage = minDamage;
            this.maxDamage = maxDamage;
            this.attackCooldown = attackCooldown;
            this.unlocked = unlocked;
        }
    }
}
