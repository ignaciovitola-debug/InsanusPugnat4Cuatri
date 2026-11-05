using UnityEngine;

namespace GladiusAI
{
    // Tres rivales mas fuertes y mas rapidos; paga por rival y suma un bonus.
    [CreateAssetMenu(fileName = "Hard", menuName = "Insanus Pugnat/Difficulty/Hard")]
    public class HardDifficulty : DifficultyStrategy
    {
        [SerializeField] private float strength = 1.25f;
        [SerializeField] private int rewardPerEnemy = 35;
        [SerializeField] private int bonus = 15;

        public override int EnemyCount => 3;

        // Sube vida y dano, y tambien ataca mas seguido.
        public override CombatStats Scale(CombatStats baseStats, float levelMultiplier)
        {
            float scale = strength * levelMultiplier;
            return new CombatStats(baseStats.maxHP * scale, baseStats.minDamage * scale, baseStats.maxDamage * scale, baseStats.attackCooldown / strength);
        }

        public override int Reward() => rewardPerEnemy * EnemyCount + bonus;
    }
}
