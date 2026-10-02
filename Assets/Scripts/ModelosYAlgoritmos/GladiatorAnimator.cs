using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Traduce lo que hace el gladiador a su Animator (Idle, Walk, Attack, Hurt, Dead) y orienta el sprite.
    /// Igual que GladiatorMovement y GladiatorCombat, es una pieza que arma GladiatorNPC: el NPC decide,
    /// esta clase solo muestra. Si el prefab no tiene Animator, todos los métodos simplemente no hacen nada.
    /// </summary>
    public class GladiatorAnimator
    {
        private static readonly int MovingParam = Animator.StringToHash("Moving");
        private static readonly int AttackParam = Animator.StringToHash("Attack");
        private static readonly int HurtParam = Animator.StringToHash("Hurt");
        private static readonly int DeadParam = Animator.StringToHash("Dead");
        private static readonly int GuardingParam = Animator.StringToHash("Guarding");

        // Distancia horizontal mínima al rival para darse vuelta: evita que el sprite
        // "parpadee" de lado cuando los dos quedan casi alineados.
        private const float FacingDeadZone = 0.05f;

        private readonly Animator animator;
        private readonly SpriteRenderer sprite;
        private readonly float walkSpeedSqr;

        private bool isMoving;
        private bool isGuarding;

        public GladiatorAnimator(Animator animator, SpriteRenderer sprite, float walkSpeedThreshold)
        {
            this.animator = animator;
            this.sprite = sprite;
            walkSpeedSqr = walkSpeedThreshold * walkSpeedThreshold;
        }

        /// <summary>Idle o Walk según la velocidad horizontal real del cuerpo.</summary>
        public void UpdateLocomotion(Vector3 velocity)
        {
            if (animator == null) return;

            bool moving = velocity.x * velocity.x + velocity.z * velocity.z > walkSpeedSqr;
            if (moving == isMoving) return;

            isMoving = moving;
            animator.SetBool(MovingParam, moving);
        }

        /// <summary>Levanta el escudo (Block) mientras está en guardia y lo baja al terminar.</summary>
        public void SetGuarding(bool guarding)
        {
            if (animator == null || guarding == isGuarding) return;

            isGuarding = guarding;
            animator.SetBool(GuardingParam, guarding);
        }

        /// <summary>Los sprites miran a la derecha: se espejan cuando el rival queda a la izquierda.</summary>
        public void FaceTowards(Vector3 selfPosition, Vector3 targetPosition)
        {
            if (sprite == null) return;

            float dx = targetPosition.x - selfPosition.x;
            if (Mathf.Abs(dx) > FacingDeadZone)
                sprite.flipX = dx < 0f;
        }

        public void PlayAttack() => SetTrigger(AttackParam);

        public void PlayHurt() => SetTrigger(HurtParam);

        public void PlayDeath()
        {
            if (animator == null) return;

            SetLocomotionStopped();
            SetGuarding(false);
            animator.SetBool(DeadParam, true);
        }

        /// <summary>Vuelve a Idle desde cualquier estado (al reusar un enemigo del pool, por ejemplo).</summary>
        public void ResetToIdle()
        {
            if (animator == null) return;

            animator.Rebind();
            animator.SetBool(DeadParam, false);
            isMoving = false;
            isGuarding = false;
        }

        private void SetLocomotionStopped()
        {
            isMoving = false;
            animator.SetBool(MovingParam, false);
        }

        private void SetTrigger(int param)
        {
            if (animator != null)
                animator.SetTrigger(param);
        }
    }
}
