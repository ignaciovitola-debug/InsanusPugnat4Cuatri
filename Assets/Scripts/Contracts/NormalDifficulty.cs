using UnityEngine;

namespace GladiusAI
{
    // Dos rivales con los stats del arquetipo; paga una cantidad por cada rival.
    [CreateAssetMenu(fileName = "Normal", menuName = "Insanus Pugnat/Difficulty/Normal")]
    public class NormalDifficulty : DifficultyStrategy
    {
        [SerializeField] private int rewardPerEnemy = 30;

        public override int EnemyCount => 2;

        // Deja vida y dano del arquetipo, multiplicados por el nivel del gladiador.
        public override CombatStats Scale(CombatStats baseStats, float levelMultiplier)
            => new CombatStats(baseStats.maxHP * levelMultiplier, baseStats.minDamage * levelMultiplier, baseStats.maxDamage * levelMultiplier, baseStats.attackCooldown);

        public override int Reward() => rewardPerEnemy * EnemyCount;
    }
}
