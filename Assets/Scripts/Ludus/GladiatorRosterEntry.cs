namespace GladiusAI
{
    // Un gladiador reclutable en la Ludus: nombre + stats de combate (mismo formato que CombatStarter.EnemyWave).
    // Guarda aparte sus stats de base (las que tenía al reclutarlo, antes de entrenar) para poder volver
    // a ellas si cae en combate.
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
        public int level;

        // Stats de base. En partidas guardadas antes de que existieran vienen en 0 (ver HasBaseStats).
        public float baseMaxHP;
        public float baseMinDamage;
        public float baseMaxDamage;
        public float baseAttackCooldown;

        public GladiatorRosterEntry(string label, float maxHP, float minDamage, float maxDamage, float attackCooldown, bool unlocked = true, int skin = 0)
        {
            this.skin = skin;
            level = 0;
            this.label = label;
            this.maxHP = maxHP;
            this.minDamage = minDamage;
            this.maxDamage = maxDamage;
            this.attackCooldown = attackCooldown;
            this.unlocked = unlocked;
            baseMaxHP = maxHP;
            baseMinDamage = minDamage;
            baseMaxDamage = maxDamage;
            baseAttackCooldown = attackCooldown;
        }

        public bool HasBaseStats => baseMaxHP > 0f;

        // Las stats actuales pasan a ser las de base (al reclutarlo o al reparar una partida vieja).
        public void SaveCurrentAsBase()
        {
            baseMaxHP = maxHP;
            baseMinDamage = minDamage;
            baseMaxDamage = maxDamage;
            baseAttackCooldown = attackCooldown;
        }

        // Pierde todo lo entrenado: vuelve a sus stats de base y a nivel 0.
        public void RestoreBaseStats()
        {
            maxHP = baseMaxHP;
            minDamage = baseMinDamage;
            maxDamage = baseMaxDamage;
            attackCooldown = baseAttackCooldown;
            level = 0;
        }
    }
}
