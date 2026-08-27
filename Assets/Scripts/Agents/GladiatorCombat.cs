using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Encapsula el cálculo de daño, el cooldown de ataque y el knockback.
    /// No sabe nada de movimiento ni del árbol de decisiones.
    /// </summary>
    public class GladiatorCombat
    {
        private readonly float minDamage;
        private readonly float maxDamage;
        private readonly float attackCooldown;
        private readonly float knockbackForce;
        private readonly float staggerDuration;

        private float attackTimer;

        public bool IsOnCooldown => attackTimer > 0f;

        public GladiatorCombat(float minDamage, float maxDamage, float attackCooldown,
            float knockbackForce, float staggerDuration)
        {
            this.minDamage = minDamage;
            this.maxDamage = maxDamage;
            this.attackCooldown = attackCooldown;
            this.knockbackForce = knockbackForce;
            this.staggerDuration = staggerDuration;
        }

        public void Tick(float deltaTime)
        {
            if (attackTimer > 0f)
                attackTimer -= deltaTime;
        }

        public void ResetCooldown() => attackTimer = 0f;
        public void RegisterAttack() => attackTimer = attackCooldown;
        public float RollDamage() => Mathf.Round(Random.Range(minDamage, maxDamage));

        public void ApplyKnockback(Rigidbody targetRb, Vector3 targetPos, Vector3 attackerPos, out float staggerOut)
        {
            Vector3 knockDir = (targetPos - attackerPos).normalized;
            knockDir.y = 0f;
            targetRb.AddForce(knockDir * knockbackForce, ForceMode.Impulse);
            staggerOut = staggerDuration;
        }
    }
}