using UnityEngine;

namespace GladiusAI
{
    public class GladiatorCombat
    {
        private readonly float minDamage;
        private readonly float maxDamage;
        private readonly float attackCooldown;
        private readonly float staggerDuration;

        private float attackTimer;
        private float stunTimer;
        private float defendTimer;

        public bool IsOnCooldown => attackTimer > 0f;
        public bool IsStunned => stunTimer > 0f;
        public bool IsDefending => defendTimer > 0f;

        public GladiatorCombat(float minDamage, float maxDamage, float attackCooldown,
            float staggerDuration)
        {
            this.minDamage = minDamage;
            this.maxDamage = maxDamage;
            this.attackCooldown = attackCooldown;
            this.staggerDuration = staggerDuration;
        }

        public void Tick(float deltaTime)
        {
            if (attackTimer > 0f)
                attackTimer -= deltaTime;
            if (stunTimer > 0f)
                stunTimer -= deltaTime;
            if (defendTimer > 0f)
                defendTimer -= deltaTime;
        }

        public void ResetCooldown() => attackTimer = 0f;
        public void RegisterAttack() => attackTimer = attackCooldown;
        public float RollDamage() => Mathf.Round(Random.Range(minDamage, maxDamage));

        public void ApplyStagger() => stunTimer = staggerDuration;

        public void StartDefend(float duration) => defendTimer = duration;
    }
}
