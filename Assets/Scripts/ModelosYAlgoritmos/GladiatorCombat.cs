using UnityEngine;

namespace GladiusAI
{
    public class GladiatorCombat
    {
        private readonly float minDamage;
        private readonly float maxDamage;
        private readonly float attackCooldown;
        private readonly float staggerDuration;

        private const float OrderCooldown = 2.5f;

        private float attackTimer;
        private float stunTimer;
        private float defendTimer;
        private float orderCooldownTimer;

        public bool IsOnCooldown => attackTimer > 0f;
        public bool IsStunned => stunTimer > 0f;
        public bool IsDefending => defendTimer > 0f;
        public bool CanReceiveOrder => orderCooldownTimer <= 0f;

        /// 1 recien dada la orden, 0 cuando ya se puede dar otra. Lo usan los botones para mostrar la espera.
        public float OrderCooldownRatio => orderCooldownTimer > 0f ? orderCooldownTimer / OrderCooldown : 0f;

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
            if (orderCooldownTimer > 0f)
                orderCooldownTimer -= deltaTime;
        }

        public void RegisterAttack() => attackTimer = attackCooldown;
        public float RollDamage() => Mathf.Round(Random.Range(minDamage, maxDamage));

        public void ApplyStagger() => stunTimer = staggerDuration;

        /// Consigna de "redoblar ataque": comparte cooldown con TryDefend para que no se pueda spamear ninguna orden.
        public bool TryRedoubleAttack()
        {
            if (orderCooldownTimer > 0f) return false;
            orderCooldownTimer = OrderCooldown;
            attackTimer = 0f;
            return true;
        }

        /// Consigna de "defenderse": mismo cooldown compartido que TryRedoubleAttack.
        public bool TryDefend(float duration)
        {
            if (orderCooldownTimer > 0f) return false;
            orderCooldownTimer = OrderCooldown;
            defendTimer = duration;
            return true;
        }
    }
}
