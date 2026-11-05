using UnityEngine;

namespace GladiusAI
{
    // Un rival mas flojo y premio fijo.
    [CreateAssetMenu(fileName = "Easy", menuName = "Insanus Pugnat/Difficulty/Easy")]
    public class EasyDifficulty : DifficultyStrategy
    {
        [SerializeField] private float weakness = 0.8f;
        [SerializeField] private int flatReward = 25;

        public override int EnemyCount => 1;

        // Baja vida y dano, y el nivel del gladiador pesa solo la mitad.
        public override CombatStats Scale(CombatStats baseStats, float levelMultiplier)
        {
            float scale = weakness * (1f + (levelMultiplier - 1f) * 0.5f);
            return new CombatStats(baseStats.maxHP * scale, baseStats.minDamage * scale, baseStats.maxDamage * scale, baseStats.attackCooldown);
        }

        public override int Reward() => flatReward;
    }
}
