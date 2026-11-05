namespace GladiusAI
{
    // Los numeros de combate de un gladiador: vida, dano y velocidad de ataque.
    public readonly struct CombatStats
    {
        public readonly float maxHP;
        public readonly float minDamage;
        public readonly float maxDamage;
        public readonly float attackCooldown;

        public CombatStats(float maxHP, float minDamage, float maxDamage, float attackCooldown)
        {
            this.maxHP = maxHP;
            this.minDamage = minDamage;
            this.maxDamage = maxDamage;
            this.attackCooldown = attackCooldown;
        }
    }
}
