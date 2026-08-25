namespace GladiusAI
{
    [System.Serializable]
    public class GladiatorStats
    {
        public string gladiatorName;
        public float maxHP;
        public float minDamage;
        public float maxDamage;
        public float attackCooldown;

        public GladiatorStats(string name, float hp, float minDmg, float maxDmg, float cooldown = 1.2f)
        {
            gladiatorName = name;
            maxHP = hp;
            minDamage = minDmg;
            maxDamage = maxDmg;
            attackCooldown = cooldown;
        }
    }
}