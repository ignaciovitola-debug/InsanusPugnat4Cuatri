namespace GladiusAI
{
    /// <summary>Un gladiador reclutable en la Ludus: nombre + stats de combate (mismo formato que CombatStarter.EnemyWave).</summary>
    [System.Serializable]
    public struct GladiatorRosterEntry
    {
        public string label;
        public float maxHP;
        public float minDamage;
        public float maxDamage;
        public float attackCooldown;

        public GladiatorRosterEntry(string label, float maxHP, float minDamage, float maxDamage, float attackCooldown)
        {
            this.label = label;
            this.maxHP = maxHP;
            this.minDamage = minDamage;
            this.maxDamage = maxDamage;
            this.attackCooldown = attackCooldown;
        }
    }
}
